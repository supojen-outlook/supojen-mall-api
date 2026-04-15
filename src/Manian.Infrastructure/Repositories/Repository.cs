using System.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Po.Api.Response;
using Manian.Domain.Repositories;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories;

/// <summary>
/// 泛型仓储基类，提供所有实体共用的 CRUD 操作
/// 采用泛型设计，避免为每个实体重复编写基础的数据库访问代码
/// </summary>
/// <typeparam name="T">实体类型，必须是引用类型（class）</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    /// <summary>
    /// Entity Framework Core 的数据库上下文
    /// protected 访问级别让继承类可以直接操作上下文，实现更复杂的查询
    /// </summary>
    protected readonly MainDbContext context;
    
    /// <summary>
    /// 当前实体类型的 DbSet
    /// 等同于 context.Set<T>()，缓存起来避免重复调用
    /// </summary>
    protected readonly DbSet<T> dbSet;
    
    /// <summary>
    /// 实体主键的属性名称，默认为 "Id"
    /// 用于 GetById 等需要按主键查询的方法，让仓储可以支持不同名称的主键
    /// </summary>
    private readonly string _idPropertyName;

    /// <summary>
    /// 构造函数，初始化仓储实例
    /// </summary>
    /// <param name="context">数据库上下文，由 DI 容器注入</param>
    /// <param name="idPropertyName">主键属性名，若实体主键不叫 "Id" 需传入实际名称</param>
    protected Repository(MainDbContext context, string idPropertyName = "Id")
    {
        // 將傳入的 DbContext 實例存到 protected 欄位，讓繼承類別也可以存取
        this.context = context;
        
        // 從 DbContext 中取得目前實體類型 T 對應的 DbSet
        // 例如 T 是 User，則 dbSet 就是 context.Users
        // 這樣我們就可以用 dbSet 進行查詢，而不需要在每個方法都寫 context.Set<T>()
        dbSet = context.Set<T>();
        
        // 記錄實體的主鍵欄位名稱，預設為 "Id"
        // 如果某個實體的主鍵叫 "UserID" 或 "Guid"，可以在繼承時傳入正確的名稱
        _idPropertyName = idPropertyName;
    }

    /// <summary>
    /// 开启数据库事务，并指定隔离级别
    /// 注意：只有在需要严格控制事务隔离级别时才使用此方法
    /// 一般场景直接使用 SaveChangeAsync() 即可，EF Core 会自动使用隐式事务
    /// </summary>
    /// <param name="level">事务隔离级别，如 IsolationLevel.Serializable</param>
    /// <returns>返回 IDbTransaction 对象，可用于手动提交或回滚事务</returns>
    public IDbTransaction Begin(IsolationLevel level)
    {
        // 1. 呼叫 DbContext 的 Database.BeginTransaction 方法開啟一個資料庫交易
        //    BeginTransaction 會回傳一個 Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction
        // 2. 這個交易物件實作了 IDbTransaction 介面，所以我們可以用 GetDbTransaction() 轉型
        // 3. 回傳 IDbTransaction 讓呼叫端可以手動 Commit() 或 Rollback()
        return context.Database.BeginTransaction(level).GetDbTransaction();
    }

    /// <summary>
    /// 计算符合条件的实体总数
    /// 常用于分页查询前的总记录数统计
    /// </summary>
    /// <param name="func">
    /// 可选的条件组合函数，可在内部调用 Where、Include 等操作
    /// 例如: repo.CountAsync(q => q.Where(x => x.IsActive).Include(x => x.Orders))
    /// </param>
    /// <returns>符合条件的实体数量</returns>
    public Task<int> CountAsync(Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        //    IQueryable<T> 是延遲執行的，此時還沒有真的去資料庫查
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func 參數（自訂查詢邏輯）
        //    func 是一個委派，接收 IQueryable<T> 並回傳 IQueryable<T>
        //    通常用來加入 Where 條件、Include 關聯資料、OrderBy 排序等
        if (func != null) 
            query = func(query); // 執行 func，把當前的 query 傳進去，並用回傳的新 query 取代舊的
        
        // 3. 執行 CountAsync() 產生 SQL: SELECT COUNT(*) FROM ...
        //    CountAsync() 是 EF Core 的擴充方法，會立即連資料庫執行並回傳整數
        return query.CountAsync();
    }

    /// <summary>
    /// 取得實體對應資料表的估計資料筆數
    /// 用於儀表板、監控等不需要精確數值的場景，效能遠優於精確計數
    /// </summary>
    /// <remarks>
    /// 此方法查詢 PostgreSQL 系統目錄 pg_class 取得估計筆數，不掃描實際資料表。
    /// 誤差通常在 1-5% 以內，取決於統計資訊更新頻率。
    /// 若資料表從未分析或發生錯誤則回傳 null。
    /// </remarks>
    /// <returns>估計筆數，無法取得時回傳 null</returns>
    /// <example>
    /// var totalUsers = await _repository.EstimatedCount() ?? 0;
    /// </example>
    public int? EstimatedCount()
    {
        return context.GetEstimatedRowCount<T>();
    }

    /// <summary>
    /// 根据字符串类型的主键查询单条实体
    /// </summary>
    /// <param name="id">实体主键值，如 Guid 字符串、业务编码等</param>
    /// <param name="func">可选的条件组合，用于添加 Include、ThenInclude 等</param>
    /// <returns>查询到的实体，若不存在则返回 null</returns>
    public async Task<T?> GetByIdAsync(string id, Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func，就執行 func 來擴充查詢（例如加入 .Include(x => x.Orders)）
        if (func != null) 
            query = func(query);
        
        // 3. 使用 EF.Property 動態指定主鍵欄位名稱
        //    EF.Property<string>(e, _idPropertyName) 的意思是：從 e 物件中取出名為 _idPropertyName 的屬性，並視為 string 型別
        //    這樣做的好處是：不需要知道實體的主鍵屬性叫什麼名字，在建構函式傳入即可
        //    例如 _idPropertyName = "UserGuid"，就會產生 WHERE "UserGuid" = @p0
        // 4. FirstOrDefaultAsync 會取回第一筆符合條件的資料，如果沒有資料就回傳 null
        return await query.FirstOrDefaultAsync(e => EF.Property<string>(e, _idPropertyName) == id);
    }

    /// <summary>
    /// 根据字符串主键查询，并直接映射到指定的 ViewModel/DTO 类型
    /// 避免查询完整实体后再手动映射，减少内存占用和代码量
    /// </summary>
    /// <typeparam name="V">目标映射类型，通常是 Response DTO</typeparam>
    /// <param name="id">实体主键值</param>
    /// <param name="func">可选的条件组合</param>
    /// <returns>映射后的 DTO 对象，若不存在则返回 null</returns>
    public async Task<V?> GetByIdAsync<V>(string id, Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func，就先執行 func（加入 Include、Where 等條件）
        if (func != null) 
            query = func(query);
        
        // 3. 加入主鍵篩選條件
        //    注意：這裡用的是 Where，不是 FirstOrDefault
        //    Where 會回傳 IQueryable<T>，但還沒執行；FirstOrDefault 才會執行
        query = query.Where(e => EF.Property<string>(e, _idPropertyName) == id);
        
        // 4. 使用 Mapster 的 ProjectToType<V>() 進行高效率映射
        //    ProjectToType 會分析 V 型別需要哪些屬性，然後產生 SELECT 語句只查那些欄位
        //    例如 V 是 UserDto 且只有 Id, Name，則 SQL 會是 SELECT "Id", "Name" FROM ...
        //    這比先查整個 User 實體再手動 mapping 更有效率
        // 5. FirstOrDefaultAsync 執行查詢並回傳第一個結果（如果有的話）
        return await query.ProjectToType<V>().FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// 根据长整型主键查询单条实体
    /// </summary>
    /// <param name="id">实体主键值，如自增 ID、雪花算法 ID 等</param>
    /// <param name="func">可选的条件组合</param>
    /// <returns>查询到的实体，若不存在则返回 null</returns>
    public async Task<T?> GetByIdAsync(long id, Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func，就執行 func 來擴充查詢
        if (func != null) 
            query = func(query);
        
        // 3. 使用 EF.Property 動態指定主鍵欄位名稱，這次指定為 long 型別
        //    因為主鍵是 long，所以用 EF.Property<long>
        // 4. FirstOrDefaultAsync 執行查詢並回傳結果（或 null）
        return await query.FirstOrDefaultAsync(e => EF.Property<long>(e, _idPropertyName) == id);
    }
    
    /// <summary>
    /// 根据长整型主键查询，并直接映射到指定的 ViewModel/DTO 类型
    /// </summary>
    /// <typeparam name="V">目标映射类型</typeparam>
    /// <param name="id">实体主键值</param>
    /// <param name="func">可选的条件组合</param>
    /// <returns>映射后的 DTO 对象，若不存在则返回 null</returns>
    public async Task<V?> GetByIdAsync<V>(long id, Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func，就先執行 func
        if (func != null) 
            query = func(query);
        
        // 3. 加入主鍵篩選條件，這次是用 long 型別比較
        query = query.Where(e => EF.Property<long>(e, _idPropertyName) == id);
        
        // 4. 用 Mapster 映射到 V 型別，並執行查詢
        //    ProjectToType 會在建構 LINQ Expression 時就決定要查哪些欄位
        return await query.ProjectToType<V>().FirstOrDefaultAsync();
    }

    /// <summary>
    /// 根据自定义条件查询单条实体
    /// 适用于无法通过主键查询的场景，如按用户名、邮箱查询
    /// </summary>
    /// <param name="func">必须传入的查询条件，通常包含 Where 子句</param>
    /// <returns>符合条件的第一个实体，若不存在则返回 null</returns>
    /// <example>
    /// await repo.GetAsync(q => q.Where(x => x.Email == "test@example.com"));
    /// </example>
    public Task<T?> GetAsync(Func<IQueryable<T>, IQueryable<T>> func)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 執行呼叫端傳入的 func，這個 func「必須」包含查詢條件
        //    例如 func = q => q.Where(x => x.IsActive == true)
        //    執行完這行後，query 已經包含了 Where 條件
        query = func(query);
        
        // 3. 執行 FirstOrDefaultAsync 取得第一筆符合條件的資料
        //    注意：如果 func 中沒有加入 Where 條件，這裡會取回資料表的第一筆
        return query.FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// 根据自定义条件查询单条实体，并直接映射到 DTO
    /// </summary>
    /// <typeparam name="V">目标映射类型</typeparam>
    /// <param name="func">可选的条件组合，若为 null 则查询第一条记录</param>
    /// <returns>映射后的 DTO 对象，若不存在则返回 null</returns>
    public async Task<V?> GetAsync<V>(Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func，就套用查詢條件
        if (func != null) 
            query = func(query);
        
        // 3. 用 Mapster 映射到 V 型別
        //    ProjectToType 會在這時分析 V 需要哪些欄位
        // 4. FirstOrDefaultAsync 執行查詢並回傳第一個結果（映射成 V 型別）
        return await query.ProjectToType<V>().FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// 查询符合条件的实体集合
    /// </summary>
    /// <param name="func">可选的条件组合，可用于筛选、排序、Include 等</param>
    /// <returns>符合条件的实体集合，若无数据则返回空集合</returns>
    public async Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢
        IQueryable<T> query = dbSet;
        
        // 2. 如果呼叫端有傳入 func，就套用查詢條件
        if (func != null)
        {
            query = func(query);
        }
        
        // 3. 執行 ToListAsync() 將查詢結果載入記憶體，成為 List<T>
        //    ToListAsync 會立即連線資料庫執行 SQL 並回傳結果
        return await query.ToListAsync();
    }

    /// <summary>
    /// 查询符合条件的实体集合，并直接映射到 DTO 集合
    /// 默认启用 AsNoTracking 以提高查询性能，因为 DTO 通常不需要更新跟踪
    /// </summary>
    /// <typeparam name="V">目标映射类型</typeparam>
    /// <param name="func">可选的条件组合</param>
    /// <returns>映射后的 DTO 集合</returns>
    public async Task<IEnumerable<V>> GetAllAsync<V>(Func<IQueryable<T>, IQueryable<T>>? func = null)
    {
        // 1. 從 dbSet 建立基礎查詢，並加上 AsNoTracking()
        //    AsNoTracking() 告訴 EF Core 不要追蹤這些實體的變化
        //    因為我們要查出來映射成 DTO，不會再存回去，不需要追蹤
        //    這樣可以減少記憶體使用量，查詢速度也更快
        IQueryable<T> query = dbSet.AsNoTracking();
        
        // 2. 如果呼叫端有傳入 func，就套用查詢條件（Where, Include, OrderBy 等）
        if (func != null) 
            query = func(query);
        
        // 3. 用 Mapster 映射到 V 型別
        //    此時 query 還是 IQueryable<T>，ProjectToType 會把它轉成 IQueryable<V>
        var mappedQuery = query.ProjectToType<V>();
        
        // 4. 執行 ToListAsync 取得 List<V>
        return await mappedQuery.ToListAsync();
    }

    /// <summary>
    /// 查询符合条件的实体集合，并允许在映射后进行自定义处理
    /// 此方法接受一个直接返回 IQueryable<V> 的函数，提供最大的灵活性
    /// 例如：在映射后执行 OrderBy、GroupBy 等操作
    /// </summary>
    /// <typeparam name="V">目标映射类型</typeparam>
    /// <param name="func">
    /// 接收 IQueryable<T> 并返回 IQueryable<V> 的函数
    /// 可在内部先 Where/Include，再映射，再排序/分组
    /// </param>
    /// <returns>处理后的 DTO 集合</returns>
    /// <example>
    /// await repo.GetAllAsync(q => q
    ///     .Where(x => x.IsActive)
    ///     .ProjectToType<UserDto>()
    ///     .OrderBy(x => x.CreatedAt));
    /// </example>
    public async Task<IEnumerable<V>> GetAllAsync<V>(Func<IQueryable<T>, IQueryable<V>> func)
    {
        // 1. 從 dbSet 建立基礎查詢，加上 AsNoTracking() 提升效能
        IQueryable<T> query = dbSet.AsNoTracking();
        
        // 2. 執行呼叫端傳入的 func
        //    注意：func 的回傳型別是 IQueryable<V>，代表它內部已經做了映射
        //    例如 func = q => q.Where(x => x.IsActive).ProjectToType<UserDto>().OrderBy(x => x.Name)
        //    這行程式執行完後，mappedQuery 已經是 IQueryable<UserDto>，且包含了 Where, OrderBy
        var mappedQuery = func(query);
        
        // 3. 執行 ToListAsync 取得 List<V>
        return await mappedQuery.ToListAsync();
    }

    /// <summary>
    /// 新增实体到数据库上下文
    /// 注意：此方法只将实体添加到跟踪，并未保存到数据库
    /// 需要配合 SaveChangeAsync() 或 SaveChanges() 才能真正持久化
    /// 这种设计允许在一个工作单元中同时处理多个新增操作
    /// </summary>
    /// <param name="entity">要新增的实体对象</param>
    public virtual void Add(T entity)
    {
        // 將 entity 加入到 DbContext 的追蹤中，狀態設為 Added
        // 此時 EF Core 會開始追蹤這個 entity，但還不會產生 INSERT SQL
        // AddAsync 非同步版本主要用在 entity 有自動產生的值（如 Guid）時，可以立即取得產生的值
        // 如果沒有特殊需求，也可以用同步的 Add()
        context.Add(entity);
    }

    /// <summary>
    /// 刪除實體並標記為待刪除狀態
    /// 
    /// 與 AddAsync 相同，此方法只會將實體標記為 Deleted
    /// 不會立即寫入資料庫，必須呼叫 SaveChangeAsync() 才會實際執行 DELETE SQL
    /// 
    /// 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
    /// </summary>
    /// <param name="entity">要刪除的實體物件</param>
    public void Delete(T entity)
    {
        // 將 entity 的狀態設為 Deleted，EF Core 會開始追蹤這個刪除動作
        context.Remove(entity);
    }

    /// <summary>
    /// 保存所有被跟踪实体的变更到数据库
    /// 这是工作单元模式的提交点，会执行所有 Insert/Update/Delete 操作
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Failure">当发生数据库约束违反时，会转换为友好的业务异常</exception>
    public virtual async Task SaveChangeAsync()
    {
        try
        {
            // 嘗試將所有追蹤中的實體變更寫入資料庫
            // 這會產生 INSERT/UPDATE/DELETE SQL，並在交易中執行
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // DbUpdateException 是 EF Core 在更新資料庫失敗時拋出的例外
            // 檢查內部例外是否為 PostgreSQL 特定的 Npgsql 例外
            if (ex.InnerException is PostgresException pgEx)
            {
                // 根據 PostgreSQL 錯誤碼（SqlState）決定要拋出什麼樣的業務例外
                switch (pgEx.SqlState)
                {
                    case PostgresErrorCodes.UniqueViolation: // 23505 - 唯一约束违反
                        // 嘗試從約束名稱中提取欄位名稱，讓錯誤訊息更人性化
                        // 例如約束名稱為 "IX_Users_Email"，我們把它轉成 "Users Email"
                        var columnName = pgEx.ConstraintName?.Replace("IX_", "").Replace("_", " ");
                        // 拋出 BadRequest，前端會收到 400 狀態碼和這個訊息
                        throw Failure.BadRequest($"{columnName} 已存在，請使用其他值。");

                    case PostgresErrorCodes.NotNullViolation: // 23502 - 非空约束违反
                        // pgEx.ColumnName 會告訴我們是哪個欄位不能為 null
                        throw Failure.BadRequest($"欄位 {pgEx.ColumnName} 不可為空值。");

                    case PostgresErrorCodes.ForeignKeyViolation: // 23503 - 外键约束违反
                        throw Failure.BadRequest($"外鍵約束失敗，請確認關聯資料存在。");

                    case PostgresErrorCodes.CheckViolation: // 23514 - 检查约束违反
                        throw Failure.BadRequest($"輸入值不符合資料庫規則。");

                    default:
                        // 其他未特別處理的 PostgreSQL 錯誤
                        throw Failure.BadRequest($"資料庫錯誤：{pgEx.MessageText}");
                }
            }

            // 非 PostgreSQL 的資料庫錯誤（例如 SQL Server 或其他）
            throw Failure.BadRequest("保存數據時發生錯誤：" + ex.InnerException?.Message ?? ex.Message);
        }
    }
}