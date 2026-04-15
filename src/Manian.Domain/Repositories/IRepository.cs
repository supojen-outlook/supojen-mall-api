using System.Data;

namespace Manian.Domain.Repositories;

/// <summary>
/// 泛型倉儲介面 - 定義所有實體共用的資料存取操作
/// 
/// 設計哲學：
/// 1. 抽象化：隱藏底層資料存取的實作細節（EF Core、Dapper 等）
/// 2. 型別安全：使用泛型確保編譯時型別檢查
/// 3. 彈性查詢：透過 Func<IQueryable<T>, IQueryable<T>> 參數，讓呼叫端可以自訂查詢邏輯
/// 4. 工作單元：SaveChangeAsync 負責提交所有變更
/// 
/// 這個介面是 Clean Architecture 中 Domain 層的一部分
/// 定義了「資料存取應該要做什麼」，但不關心「怎麼做」
/// </summary>
/// <typeparam name="T">實體型別，例如 User、Order、Product 等</typeparam>
public interface IRepository<T>
{
    /// <summary>
    /// 開啟資料庫交易，並指定隔離層級
    /// 
    /// 什麼時候需要手動交易？
    /// - 需要跨多個 Repository 操作，且必須全部成功或全部失敗
    /// - 需要指定隔離層級（如可序列化）來解決特定並發問題
    /// 
    /// 一般情況：直接呼叫多個 AddAsync 再呼叫 SaveChangeAsync 即可
    /// EF Core 預設會在 SaveChanges 時自動使用交易
    /// </summary>
    /// <param name="level">交易隔離層級，如 IsolationLevel.Serializable</param>
    /// <returns>IDbTransaction 物件，可用於 Commit 或 Rollback</returns>
    IDbTransaction Begin(IsolationLevel level =  IsolationLevel.ReadCommitted);

    /// <summary>
    /// 計算符合條件的實體總數
    /// 
    /// 常見用途：
    /// - 分頁功能中計算總筆數，用於產生分頁資訊
    /// - 儀表板顯示統計數字
    /// </summary>
    /// <param name="func">
    /// 可選的查詢條件組合
    /// 例如：CountAsync(q => q.Where(x => x.IsActive))
    /// </param>
    /// <returns>符合條件的實體數量</returns>
    Task<int> CountAsync(Func<IQueryable<T>, IQueryable<T>>? func = null);

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
    int? EstimatedCount();
        
    /// <summary>
    /// 根據字串型主鍵查詢單筆實體
    /// 
    /// 適用場景：
    /// - 主鍵為 GUID、業務編號等字串型別
    /// </summary>
    /// <param name="id">主鍵值（字串格式）</param>
    /// <param name="func">可選的查詢條件，用於加入 Include、ThenInclude 等</param>
    /// <returns>查詢到的實體，若不存在則回傳 null</returns>
    Task<T?> GetByIdAsync(string id, Func<IQueryable<T>, IQueryable<T>>? func = null);
    
    /// <summary>
    /// 根據字串型主鍵查詢，並直接投影到 DTO
    /// 
    /// 優點：
    /// - 只查詢需要的欄位，減少資料傳輸量
    /// - 避免追蹤實體，提升效能
    /// - 不需要手動 mapping
    /// </summary>
    /// <typeparam name="V">目標 DTO 型別</typeparam>
    /// <param name="id">主鍵值（字串格式）</param>
    /// <param name="func">可選的查詢條件</param>
    /// <returns>投影後的 DTO，若不存在則回傳 null</returns>
    Task<V?> GetByIdAsync<V>(string id, Func<IQueryable<T>, IQueryable<T>>? func = null);
    
    /// <summary>
    /// 根據數值型主鍵查詢單筆實體
    /// 
    /// 適用場景：
    /// - 自增 ID、雪花 ID 等數值型主鍵
    /// </summary>
    /// <param name="id">主鍵值（數值格式）</param>
    /// <param name="func">可選的查詢條件</param>
    /// <returns>查詢到的實體，若不存在則回傳 null</returns>
    Task<T?> GetByIdAsync(long id, Func<IQueryable<T>, IQueryable<T>>? func = null);
    
    /// <summary>
    /// 根據數值型主鍵查詢，並直接投影到 DTO
    /// </summary>
    /// <typeparam name="V">目標 DTO 型別</typeparam>
    /// <param name="id">主鍵值（數值格式）</param>
    /// <param name="func">可選的查詢條件</param>
    /// <returns>投影後的 DTO，若不存在則回傳 null</returns>
    Task<V?> GetByIdAsync<V>(long id, Func<IQueryable<T>, IQueryable<T>>? func = null);
    
    /// <summary>
    /// 根據自訂條件查詢單筆實體
    /// 
    /// 適用場景：
    /// - 依唯一欄位查詢，如 Email、使用者名稱
    /// - 需要複雜查詢條件（多個欄位組合）
    /// 
    /// 注意：如果符合條件的資料超過一筆，只會回傳第一筆
    /// 建議搭配確保唯一性的條件使用
    /// </summary>
    /// <param name="func">查詢條件（必須傳入），例如 q => q.Where(x => x.Email == "test@test.com")</param>
    /// <returns>符合條件的第一筆實體，若不存在則回傳 null</returns>
    Task<T?> GetAsync(Func<IQueryable<T>, IQueryable<T>> func);
    
    /// <summary>
    /// 根據自訂條件查詢單筆實體，並投影到 DTO
    /// 
    /// func 參數可為 null，此時會回傳資料表的第一筆記錄
    /// 但通常不會這樣用，應該要加上 Where 條件
    /// </summary>
    /// <typeparam name="V">目標 DTO 型別</typeparam>
    /// <param name="func">可選的查詢條件</param>
    /// <returns>投影後的第一筆 DTO，若不存在則回傳 null</returns>
    Task<V?> GetAsync<V>(Func<IQueryable<T>, IQueryable<T>>? func = null);
    
    /// <summary>
    /// 查詢符合條件的所有實體
    /// 
    /// 適用場景：
    /// - 列表頁面
    /// - 匯出資料
    /// - 下拉選單選項
    /// 
    /// 注意：如果資料量很大，應該搭配分頁使用
    /// </summary>
    /// <param name="query">可選的查詢條件，可用於篩選、排序、Include 等</param>
    /// <returns>實體集合，若無資料則回傳空集合</returns>
    Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? query = null);

    /// <summary>
    /// 查詢符合條件的所有實體，並投影到 DTO
    /// 
    /// 與上一個方法的差異：
    /// - 回傳型別為 IEnumerable<V>
    /// - 預期使用方會在 query 中進行投影或由實作方自動投影
    /// </summary>
    /// <typeparam name="V">目標 DTO 型別</typeparam>
    /// <param name="query">可選的查詢條件</param>
    /// <returns>DTO 集合</returns>
    Task<IEnumerable<V>> GetAllAsync<V>(Func<IQueryable<T>, IQueryable<T>>? query = null);

    /// <summary>
    /// 查詢並投影的高度彈性版本
    /// 
    /// 這個方法讓呼叫端可以完全控制查詢和投影的過程
    /// query 參數的型別是 Func<IQueryable<T>, IQueryable<V>>
    /// 表示呼叫端可以先查詢、再投影、再排序、再分頁
    /// 
    /// 使用範例：
    /// await repo.GetAllAsync(q => q
    ///     .Where(x => x.IsActive)
    ///     .ProjectToType<UserDto>()
    ///     .OrderBy(x => x.CreatedAt)
    ///     .Skip(10).Take(20));
    /// </summary>
    /// <typeparam name="V">目標 DTO 型別</typeparam>
    /// <param name="query">定義從 IQueryable<T> 到 IQueryable<V> 的轉換邏輯</param>
    /// <returns>處理後的 DTO 集合</returns>
    Task<IEnumerable<V>> GetAllAsync<V>(Func<IQueryable<T>, IQueryable<V>> query);
    
    /// <summary>
    /// 新增實體到倉儲
    /// 
    /// 注意：此方法只會將實體加入追蹤，不會立即寫入資料庫
    /// 必須呼叫 SaveChangeAsync() 才會實際儲存
    /// 
    /// 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
    /// </summary>
    /// <param name="entity">要新增的實體物件</param>
    void Add(T entity);
    
    /// <summary>
    /// 刪除實體並標記為待刪除狀態
    /// 
    /// 與 AddAsync 相同，此方法只會將實體標記為 Deleted
    /// 不會立即寫入資料庫，必須呼叫 SaveChangeAsync() 才會實際執行 DELETE SQL
    /// 
    /// 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
    /// </summary>
    /// <param name="entity">要刪除的實體物件</param>
    void Delete(T entity);


    /// <summary>
    /// 儲存所有變更
    /// 
    /// 這是工作單元模式的提交點
    /// 會將所有被追蹤的實體變更（新增、修改、刪除）一次寫入資料庫
    /// 
    /// 在交易中，這個方法代表交易的提交
    /// </summary>
    /// <returns></returns>
    Task SaveChangeAsync();
}