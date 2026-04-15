using Manian.Domain.Entities.Products;

namespace Manian.Domain.Repositories.Products;

public interface IProductRepository : IRepository<Product>
{
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
    void AddSku(Sku sku);

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
    Task<Sku?> GetSkuAsync(int skuId);

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
    Task<IEnumerable<Sku>> GetSkusByProductIdAsync(int productId);

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
    Task<IEnumerable<Sku>> GetSkusByIdsAsync(IEnumerable<int> skuIds);

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
    void RemoveSku(Sku sku);
}
