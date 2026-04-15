using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Manian.Infrastructure.Persistence;
using Manian.Infrastructure.Persistence.ManyToMany;
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Products;

/// <summary>
/// 屬性鍵倉儲實作類別
/// 
/// 職責：
/// - 實作 IAttributeKeyRepository 介面
/// - 處理 AttributeKey 實體的所有資料庫操作
/// - 提供查詢屬性鍵對應屬性值的功能
/// - 繼承泛型 Repository<AttributeKey> 獲得通用 CRUD 功能
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 繼承模式：複用泛型 Repository 的通用功能
/// - 依賴注入：透過建構函式注入 MainDbContext
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 Domain 層的 IAttributeKeyRepository 介面
/// - 使用 MainDbContext 與資料庫互動
/// 
/// 生命週期：
/// - 註冊為 Scoped（見 Infrastructure/DI.cs）
/// - 每個 HTTP 請求一個實例
/// </summary>
public class AttributeKeyRepository : Repository<AttributeKey>, IAttributeKeyRepository
{
    /// <summary>
    /// 唯一識別碼服務
    /// 
    /// 職責：
    /// - 提供唯一的整數識別碼
    /// - 用於生成實體的主鍵值
    /// 
    /// 生命週期：
    /// - 由 DI 容器注入
    /// - 通常註冊為 Singleton 或 Scoped
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="context">資料庫上下文</param>
    /// <param name="uniqueIdentifier">唯一識別碼服務</param>
    public AttributeKeyRepository(MainDbContext context, IUniqueIdentifier uniqueIdentifier) : base(context)
    {
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 為指定屬性鍵新增單一屬性值
    /// </summary>
    /// <param name="attributeKeyId">屬性鍵 ID</param>
    /// <param name="value">屬性值內容</param>
    /// <param name="sortOrder">排序順序，預設為 0</param>
    /// <param name="description">屬性值描述，可為 null</param>
    /// <returns>新增的屬性值實體</returns>
    public AttributeValue AddValue(int attributeKeyId, string value, int sortOrder = 0, string? description = null)
    {
        // ========== 第一步：取得 AttributeValue 的 DbSet ==========
        // context.Set<AttributeValue>() 取得 AttributeValue 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var set = context.Set<AttributeValue>();
        
        // ========== 第二步：建立並新增屬性值實體 ==========
        // 使用 DbSet.Add 方法新增新的 AttributeValue 實體
        // EF Core 會追蹤這個實體的狀態為 Added
        var attributeValue = new AttributeValue
        {
            // 使用唯一識別碼服務生成主鍵
            // _uniqueIdentifier.NextInt() 產生全域唯一的整數 ID
            // 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
            Id = _uniqueIdentifier.NextInt(),
            
            // 關聯到指定的屬性鍵
            // AttributeId 是外鍵，指向 AttributeKey 實體
            AttributeId = attributeKeyId,
            
            // 屬性值內容
            // 這是實際顯示給使用者看的值，如"紅色"、"XL"
            Value = value,
            
            // 屬性值描述
            // 提供額外的說明資訊，可為 null
            Description = description,
            
            // 排序順序
            // 用於控制屬性值在列表中的顯示順序
            // 數字越小越前面，預設為 0
            SortOrder = sortOrder,
            
            // 記錄建立時間（UTC）
            // 使用協調世界時 (UTC) 記錄建立時間
            // 避免時區問題，便於跨時區系統使用
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // ========== 第三步：將實體加入追蹤 ==========
        // set.Add(attributeValue) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        set.Add(attributeValue);
    
        // ========== 第四步：返回新增的屬性值實體 ==========
        // 返回新增的屬性值實體，包含所有屬性值
        // 這樣可以讓呼叫者直接使用這個實體，而不需要再進行查詢
        return attributeValue;
    }

    /// <summary>
    /// 刪除指定的屬性值並標記為待刪除狀態
    /// 
    /// 不會立即寫入資料庫，必須呼叫 SaveChangeAsync() 才會實際執行 DELETE SQL
    /// </summary>
    /// <param name="attributeValue">要刪除的屬性值實體</param>
    public void Delete(AttributeValue attributeValue)
    {
        // ========== 第一步：取得 AttributeValue 的 DbSet ==========
        // context.Set<AttributeValue>() 取得 AttributeValue 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var set = context.Set<AttributeValue>();

        // ========== 第二步：刪除屬性值實體 ==========
        // if(attributeValue != null) 檢查實體是否存在
        // 這是一種防禦性編程，避免對 null 執行操作
        // 
        // set.Remove(attributeValue) 將實體標記為 Deleted
        // EF Core 會追蹤這個實體的狀態為 Deleted
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 DELETE
        // 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
        if(attributeValue != null) set.Remove(attributeValue);
    }


    /// <summary>
    /// 根據 ID 查詢單一屬性值
    /// </summary>
    /// <param name="attributeValueId">屬性值 ID</param>
    /// <returns>查詢到的屬性值實體，若不存在則回傳 null</returns>
    public async Task<AttributeValue?> GetValueAsync(int attributeValueId)
    {
        // ========== 第一步：取得 AttributeValue 的 DbSet ==========
        // context.Set<AttributeValue>() 取得 AttributeValue 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var set = context.Set<AttributeValue>();

        // ========== 第二步：根據 ID 查詢屬性值實體 ==========
        // set.FindAsync(attributeValueId) 根據主鍵查詢實體
        // 這個方法會先在記憶體中追蹤的實體中查找
        // 如果找不到，會發送 SQL 查詢到資料庫
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var attributeValue = await set.FindAsync(attributeValueId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的屬性值實體
        // 如果找不到，回傳 null
        return attributeValue;
    }

    /// <summary>
    /// 根據類別 ID 與銷售狀態，非同步取得該類別關聯的屬性鍵集合。
    /// </summary>
    /// <param name="categoryId">要查詢的類別 ID</param>
    /// <param name="forSales">
    /// 銷售狀態篩選條件 (可選)。
    /// true: 僅回傳用於銷售的屬性。
    /// false: 僅回傳非用於銷售的屬性。
    /// null: 回傳所有屬性 (不進行篩選)。
    /// </param>
    /// <returns>符合條件的屬性鍵集合 (AttributeKey)</returns>
    public async Task<IEnumerable<AttributeKey>> GetCategoryAttributesAsync(int categoryId, bool? forSales = null)
    {
        // 1. 構建基礎查詢
        // 從資料庫的 CategoryAttribute 資料表（聯結表）開始查詢
        var query = context.Set<CategoryAttribute>()
            // 2. 預先載入
            // 使用 Include 通知 EF Core 在查詢 CategoryAttribute 時，一併將關聯的 AttributeKey 資料載入
            // 這是為了避免稍後存取 ca.AttributeKey 時觸發延遲載入，造成 N+1 查詢問題
            .Include(ca => ca.AttributeKey)
            // 3. 基礎過濾條件
            // 篩選出屬於指定 categoryId 的關聯記錄
            // 這個條件無論如何都會執行
            .Where(ca => ca.CategoryId == categoryId);

        // 4. 條件式動態過濾
        // 檢查呼叫端是否有傳入 forSales 參數 (即不為 null)
        if (forSales.HasValue)
        {
            // 如果有指定值，將過濾條件加入查詢表達式
            // 注意：這裡的條件會被轉譯成 SQL 的 WHERE 子句，在資料庫端執行過濾
            query = query.Where(ca => 
                ca.AttributeKey != null &&           // 確保關聯的屬性鍵存在
                ca.AttributeKey.ForSales == forSales.Value // 且屬性鍵的 ForSales 欄位符合傳入值
            );
        }

        // 5. 執行查詢
        // ToListAsync 會觸發資料庫連線，執行上述組裝好的 SQL 指令
        // 並將結果非同步地載入到記憶體中的 List 裡
        var categoryAttributes = await query.ToListAsync();

        // 6. 資料轉換與回傳
        // 將查詢結果 (CategoryAttribute 實體集合) 投影轉換為我們需要的型別 (AttributeKey 實體集合)
        return categoryAttributes
            // 從 CategoryAttribute 中取出關聯的 AttributeKey 物件
            .Select(ca => ca.AttributeKey)
            // 再次確保過濾掉任何為 null 的項目 (雙重保險，確保回傳資料的完整性)
            .Where(ak => ak != null);
    }


    /// <summary>
    /// 根據屬性鍵 ID 查詢所有關聯的屬性值
    /// </summary>
    /// <param name="attributeKeyId">屬性鍵 ID</param>
    /// <returns>符合條件的屬性值集合</returns>
    public async Task<IEnumerable<AttributeValue>> GetValuesAsync(int attributeKeyId)
    {
        // 建立查詢物件
        // context.Set<AttributeValue>() 取得 AttributeValue 的 DbSet
        // AsQueryable() 將 DbSet 轉換為 IQueryable，支援 LINQ 查詢
        var query = context.Set<AttributeValue>().AsQueryable();
        
        // 添加過濾條件
        // Where(x => x.AttributeId == attributeKeyId) 篩選出指定屬性鍵的屬性值
        // 這會產生 SQL 的 WHERE 子句：WHERE attribute_id = @attributeKeyId
        query = query.Where(x => x.AttributeId == attributeKeyId);

        // 執行查詢並返回結果
        // ToListAsync() 非同步執行查詢並將結果轉換為 List
        // 返回類型為 Task<List<AttributeValue>>，會自動轉換為 Task<IEnumerable<AttributeValue>>
        return await query.ToListAsync();
    }
}
