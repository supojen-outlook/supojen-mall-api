using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Products;

/// <summary>
/// 商品倉儲實作類別
/// 
/// 職責：
/// - 實作 IProductRepository 介面
/// - 處理 Product 實體的所有資料庫操作
/// - 管理 Product 與 Sku 的關聯關係
/// - 繼承泛型 Repository<Product> 獲得通用 CRUD 功能
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 繼承模式：複用泛型 Repository 的通用功能
/// - 依賴注入：透過建構函式注入 MainDbContext
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 Domain 層的 IProductRepository 介面
/// - 使用 MainDbContext 與資料庫互動
/// 
/// 生命週期：
/// - 註冊為 Scoped（見 Infrastructure/DI.cs）
/// - 每個 HTTP 請求一個實例
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    /// <summary>
    /// 建構函式
    /// 
    /// 職責：
    /// - 初始化倉儲實例
    /// - 注入資料庫上下文
    /// - 傳遞給父類別 Repository<Product>
    /// 
    /// 參數說明：
    /// - context：MainDbContext 實例，用於資料庫操作
    /// 
    /// 設計考量：
    /// - 不指定主鍵屬性名稱，使用父類別預設值
    /// - 與 CategoryRepository 不同，CategoryRepository 明確指定 "Id"
    /// 
    /// 父類別建構函式簽名：
    /// Repository(DbContext context, string? idPropertyName = null)
    /// </summary>
    /// <param name="context">
    /// MainDbContext 實例
    /// - 負責與資料庫的連線和操作
    /// - 由 DI 容器自動注入
    /// - 生命週期為 Scoped
    /// </param>
    public ProductRepository(MainDbContext context) : base(context) {}

    /// <summary>
    /// 新增 SKU 到商品
    /// 
    /// 職責：
    /// - 將 SKU 實體加入資料庫追蹤
    /// - 設定 SKU 與 Product 的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保 SKU 的 ProductId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 創建商品時新增預設 SKU
    /// - 為現有商品新增新的 SKU 規格
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證 SKU 的 ProductId 是否正確
    /// </summary>
    /// <param name="sku">要新增的 SKU 實體</param>
    public void AddSku(Sku sku)
    {
        // ========== 第一步：取得 Sku 的 DbSet ==========
        // context.Set<Sku>() 取得 Sku 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var skuSet = context.Set<Sku>();

        // ========== 第二步：將 SKU 加入追蹤 ==========
        // skuSet.Add(sku) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        skuSet.Add(sku);
    }

    /// <summary>
    /// 根據 SKU ID 查詢單一 SKU
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的 SKU
    /// - 包含關聯的 Product 實體
    /// 
    /// 設計考量：
    /// - 使用 FindAsync 方法提高查詢效率
    /// - 包含 Product 實體以減少後續查詢
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 庫存查詢
    /// - 訂單處理時獲取 SKU 資訊
    /// - SKU 詳情頁顯示
    /// 
    /// 注意事項：
    /// - 如果 SKU 不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// </summary>
    /// <param name="skuId">SKU ID</param>
    /// <returns>查詢到的 SKU 實體，若不存在則回傳 null</returns>
    public async Task<Sku?> GetSkuAsync(int skuId)
    {
        // ========== 第一步：取得 Sku 的 DbSet ==========
        // context.Set<Sku>() 取得 Sku 實體的 DbSet
        var skuSet = context.Set<Sku>();

        // ========== 第二步：根據 ID 查詢 SKU 實體 ==========
        // skuSet.FindAsync(skuId) 根據主鍵查詢實體
        // 這個方法會先在記憶體中追蹤的實體中查找
        // 如果找不到，會發送 SQL 查詢到資料庫
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var sku = await skuSet.FindAsync(skuId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的 SKU 實體
        // 如果找不到，回傳 null
        return sku;
    }

    /// <summary>
    /// 查詢指定 SKU ID 集合的所有 SKU
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 SKU ID 集合的所有 SKU
    /// - 包含關聯的 Product 實體
    /// 
    /// 設計考量：
    /// - 使用 Where 方法過濾指定 SKU ID 集合的 SKU
    /// - 包含 Product 實體以減少後續查詢
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 庫存查詢
    /// - 訂單處理時獲取多個 SKU 資訊
    /// - SKU 詳情頁顯示
    /// 
    /// 注意事項：
    /// - 如果 SKU 不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// </summary>
    /// <param name="skuIds">SKU ID 集合</param>
    /// <returns>符合條件的 SKU 集合</returns>
    public async Task<IEnumerable<Sku>> GetSkusByIdsAsync(IEnumerable<int> skuIds)
    {
        // ========== 第一步：取得 Sku 的 DbSet ==========
        // context.Set<Sku>() 取得 Sku 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var skuSet = context.Set<Sku>().AsQueryable();

        // ========== 第二步：查詢指定 SKU ID 集合的所有 SKU ==========
        // skuSet.Where(s => skuIds.Contains(s.Id)) 過濾出指定 SKU ID 集合的 SKU
        // skuIds.Contains(s.Id) 檢查 SKU ID 是否在傳入的集合中
        // 這會產生 SQL 的 IN 子句：WHERE id IN (@id1, @id2, ...)
        // ToListAsync() 執行查詢並轉換為 List
        return await skuSet.Where(s => skuIds.Contains(s.Id)).ToListAsync();
    }


    /// <summary>
    /// 查詢指定商品的所有 SKU
    /// 
    /// 職責：
    /// - 從資料庫查詢指定商品 ID 的所有 SKU
    /// - 包含關聯的 Product 實體
    /// - 按建立時間排序
    /// 
    /// 設計考量：
    /// - 使用 Where 方法過濾指定商品的 SKU
    /// - 包含 Product 實體以減少後續查詢
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 商品詳情頁顯示所有規格
    /// - 庫存管理
    /// - 價格比較
    /// 
    /// 注意事項：
    /// - 如果商品沒有 SKU，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// </summary>
    /// <param name="productId">商品 ID</param>
    /// <returns>該商品的所有 SKU 集合</returns>
    public async Task<IEnumerable<Sku>> GetSkusByProductIdAsync(int productId)
    {
        // ========== 第一步：取得 Sku 的 DbSet ==========
        // context.Set<Sku>() 取得 Sku 實體的 DbSet
        var skuSet = context.Set<Sku>();

        // ========== 第二步：查詢指定商品的所有 SKU ==========
        // skuSet.Where(s => s.ProductId == productId) 過濾出指定商品的 SKU
        // Include(s => s.Product) 預先載入關聯的 Product 實體
        // OrderBy(s => s.CreatedAt) 按建立時間排序
        // ToListAsync() 執行查詢並轉換為 List
        var skus = await skuSet
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的 SKU 集合
        // 如果沒有找到，會返回空集合
        return skus;
    }

    /// <summary>
    /// 從商品中移除 SKU
    /// 
    /// 職責：
    /// - 將 SKU 實體標記為待刪除
    /// - 確保資料庫約束不會被違反
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 使用 EF Core 的變更追蹤機制
    /// - 檢查實體是否存在於資料庫中
    /// 
    /// 使用場景：
    /// - 刪除商品的特定規格
    /// - 商品規格調整
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 如果 SKU 有關聯的庫存記錄，刪除會失敗
    /// - 建議在刪除前檢查是否有關聯資料
    /// </summary>
    /// <param name="sku">要移除的 SKU 實體</param>
    public void RemoveSku(Sku sku)
    {
        // ========== 第一步：取得 Sku 的 DbSet ==========
        // context.Set<Sku>() 取得 Sku 實體的 DbSet
        var skuSet = context.Set<Sku>();

        // ========== 第二步：刪除 SKU 實體 ==========
        // if(sku != null) 檢查實體是否存在
        // 這是一種防禦性編程，避免對 null 執行操作
        // 
        // skuSet.Remove(sku) 將實體標記為 Deleted
        // EF Core 會追蹤這個實體的狀態為 Deleted
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 DELETE
        // 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
        if(sku != null) skuSet.Remove(sku);
    }
}

