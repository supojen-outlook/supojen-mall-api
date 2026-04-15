using Manian.Domain.Entities.Warehouses;

namespace Manian.Domain.Repositories.Warehouses;

/// <summary>
/// 儲位倉儲介面
/// 
/// 職責：
/// - 定義儲位相關的資料存取操作
/// - 繼承泛型 IRepository<Location> 獲得通用 CRUD 功能
/// - 擴展庫存相關的特定查詢和操作方法
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 介面隔離原則（ISP）：只定義特定方法，不暴露不必要功能
/// - 依賴反轉原則（DIP）：依賴抽象而非實作
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 由 Infrastructure 層的 LocationRepository 實作
/// - 被 Application 層的 Query/Command 使用
/// 
/// 使用場景：
/// - 儲位管理（新增、查詢、更新、刪除）
/// - 庫存查詢（取得指定儲位的所有庫存）
/// - 庫存操作（新增、刪除庫存記錄）
/// 
/// 關聯實體：
/// - Location：儲位實體（主實體）
/// - Inventory：庫存實體（關聯實體）
/// 
/// 設計特點：
/// - 提供庫存相關的特定方法（GetInventoriesAsync、GetInventoryAsync）
/// - 支援庫存記錄的新增和刪除（AddInventory、DeleteInventory）
/// - 不包含庫存更新邏輯（應使用 InventoryTransaction）
/// 
/// 參考實作：
/// - IProductRepository：類似的擴展方法設計（AddSku、GetSkusAsync）
/// - ICategoryRepository：類似的擴展方法設計（GetAttributeKeysAsync）
/// </summary>
public interface ILocationRepository : IRepository<Location>
{
    /// <summary>
    /// 查詢指定儲位的所有庫存記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定儲位 ID 的所有庫存
    /// - 包含關聯的 Sku 和 Location 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 不包含分頁邏輯（如需要，應新增專用查詢方法）
    /// 
    /// 使用場景：
    /// - 儲位庫存總覽
    /// - 庫存報表生成
    /// - 儲位移動操作前查詢
    /// 
    /// 注意事項：
    /// - 如果儲位沒有庫存，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含庫存交易記錄（InventoryTransaction）
    /// </summary>
    /// <param name="locationId">儲位 ID</param>
    /// <param name="func">查詢條件</param>
    /// <returns>該儲位的所有庫存記錄集合</returns>
    Task<IEnumerable<Inventory>> GetInventoriesByLocationIdAsync(int locationId, Func<IQueryable<Inventory>, IQueryable<Inventory>>? func = null);

    /// <summary>
    /// 查詢指定 ID 的庫存記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的庫存記錄
    /// - 包含關聯的 Sku 和 Location 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 使用 FindAsync 方法提高查詢效率
    /// - 包含關聯實體以減少後續查詢
    /// 
    /// 使用場景：
    /// - 庫存詳情顯示
    /// - 庫存更新操作
    /// - 庫存刪除操作
    /// 
    /// 注意事項：
    /// - 如果庫存記錄不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 不包含庫存交易記錄（InventoryTransaction）
    /// </summary>
    /// <param name="inventoryId">庫存記錄 ID</param>
    /// <returns>查詢到的庫存實體，若不存在則回傳 null</returns>
    Task<Inventory?> GetInventoryAsync(int inventoryId);

    /// <summary>
    /// 查詢指定 SKU 在所有儲位的庫存記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 SKU ID 在所有儲位的庫存記錄
    /// - 包含關聯的 Sku 和 Location 實體
    /// - 回傳該 SKU 在所有儲位的庫存記錄集合
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 一個 SKU 可能存在於多個儲位
    /// - 不包含庫存交易記錄（InventoryTransaction）
    /// 
    /// 使用場景：
    /// - SKU 庫存總覽（查看 SKU 在所有儲位的庫存）
    /// - 訂單處理時選擇最佳儲位出貨
    /// - 庫存調度（從一個儲位移動到另一個儲位）
    /// - 庫存報表生成
    /// 
    /// 注意事項：
    /// - 如果 SKU 沒有庫存記錄，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含庫存交易記錄（InventoryTransaction）
    /// 
    /// 與 GetInventoryAsync 的差異：
    /// - GetInventoryAsync：查詢指定 SKU 的庫存記錄（假設只有一筆）
    /// - GetInventoriesBySkuIdsync：查詢指定 SKU 在所有儲位的庫存記錄（可能有多筆）
    /// 
    /// 設計限制：
    /// - 只根據 SKU ID 查詢，不支援多條件查詢
    /// - 不支援分頁（假設一個 SKU 的庫存記錄數量有限）
    /// - 不支援排序（如需要，應新增專用查詢方法）
    /// </summary>
    /// <param name="skuId">SKU ID</param>
    /// <param name="func">查詢條件</param>
    /// <returns>該 SKU 在所有儲位的庫存記錄集合</returns>
    Task<IEnumerable<Inventory>> GetInventoriesBySkuIdsync(int skuId, Func<IQueryable<Inventory>, IQueryable<Inventory>>? func = null);

    /// <summary>
    /// 新增庫存記錄
    /// 
    /// 職責：
    /// - 將庫存記錄加入資料庫追蹤
    /// - 設定庫存與 Sku、Location 的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保庫存的 SkuId 和 LocationId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 初始化庫存記錄
    /// - 為新 SKU 建立庫存
    /// - 為新儲位建立庫存
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證 SkuId 和 LocationId 是否正確
    /// - 不應使用此方法更新庫存數量（應使用 InventoryTransaction）
    /// 
    /// 資料約束：
    /// - (sku_id + location_id) 必須唯一（複合唯一索引）
    /// - QuantityOnHand 不能為負
    /// - QuantityReserved 不能為負
    /// </summary>
    /// <param name="inventory">要新增的庫存記錄實體</param>
    void AddInventory(Inventory inventory);

    /// <summary>
    /// 刪除庫存記錄
    /// 
    /// 職責：
    /// - 將庫存記錄標記為待刪除
    /// - 確保資料庫約束不會被違反
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 使用 EF Core 的變更追蹤機制
    /// - 檢查實體是否存在於資料庫中
    /// 
    /// 使用場景：
    /// - 清理無用的庫存記錄
    /// - SKU 或儲位刪除前的清理
    /// - 測試資料清理
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 如果庫存有交易記錄，刪除會失敗
    /// - 建議在刪除前檢查是否有交易記錄
    /// - 建議在刪除前檢查庫存數量是否為零
    /// 
    /// 資料約束：
    /// - 如果有關聯的 InventoryTransaction，刪除會失敗
    /// - 由 InventoryConfiguration.cs 設定 OnDelete(DeleteBehavior.Restrict)
    /// </summary>
    /// <param name="inventory">要刪除的庫存記錄實體</param>
    void DeleteInventory(Inventory inventory);
}
