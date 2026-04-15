using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Manian.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Warehouses;

/// <summary>
/// 儲位倉儲實作類別
/// 
/// 職責：
/// - 實作 ILocationRepository 介面
/// - 處理 Location 實體的所有資料庫操作
/// - 管理 Location 與 Inventory 的關聯關係
/// - 繼承泛型 Repository<Location> 獲得通用 CRUD 功能
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 繼承模式：複用泛型 Repository 的通用功能
/// - 依賴注入：透過建構函式注入 MainDbContext
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 Domain 層的 ILocationRepository 介面
/// - 使用 MainDbContext 與資料庫互動
/// 
/// 生命週期：
/// - 註冊為 Scoped（見 Infrastructure/DI.cs）
/// - 每個 HTTP 請求一個實例
/// 
/// 參考實作：
/// - ProductRepository：類似的擴展方法設計（AddSku、GetSkusAsync）
/// - OrderRepository：類似的擴展方法設計（GetOrderItemsAsync）
/// </summary>
public class LocationRepository : Repository<Location>, ILocationRepository
{
    /// <summary>
    /// 建構函式
    /// 
    /// 職責：
    /// - 初始化倉儲實例
    /// - 注入資料庫上下文
    /// - 傳遞給父類別 Repository<Location>
    /// 
    /// 參數說明：
    /// - context：MainDbContext 實例，用於資料庫操作
    /// 
    /// 設計考量：
    /// - 不指定主鍵屬性名稱，使用父類別預設值
    /// - 與 ProductRepository、OrderRepository 保持一致
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
    public LocationRepository(MainDbContext context) : base(context) {}

    // =========================================================================
    // 庫存相關方法 (Inventory Related Methods)
    // =========================================================================

    /// <summary>
    /// 新增庫存記錄
    /// 
    /// 職責：
    /// - 將庫存記錄加入資料庫追蹤
    /// - 設定庫存與 Sku、Location 的關聯
    /// 
    /// 實作細節：
    /// - 使用 DbSet.Add 方法加入實體
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
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
    public void AddInventory(Inventory inventory)
    {
        // ========== 第一步：取得 Inventory 的 DbSet ==========
        // context.Set<Inventory>() 取得 Inventory 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var inventorySet = context.Set<Inventory>();

        // ========== 第二步：將庫存記錄加入追蹤 ==========
        // inventorySet.Add(inventory) 將實體加入 EF Core 的變更追蹤
        // 實體狀態會被設定為 Added
        // 在呼叫 SaveChangeAsync 時會執行 INSERT 語句
        inventorySet.Add(inventory);
    }

    /// <summary>
    /// 刪除庫存記錄
    /// 
    /// 職責：
    /// - 將庫存記錄標記為待刪除
    /// - 確保資料庫約束不會被違反
    /// 
    /// 實作細節：
    /// - 使用 DbSet.Remove 方法標記刪除
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 使用 EF Core 的變更追蹤機制
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
    public void DeleteInventory(Inventory inventory)
    {
        // ========== 第一步：取得 Inventory 的 DbSet ==========
        // context.Set<Inventory>() 取得 Inventory 實體的 DbSet
        var inventorySet = context.Set<Inventory>();

        // ========== 第二步：將庫存記錄標記為待刪除 ==========
        // inventorySet.Remove(inventory) 將實體標記為 Deleted
        // 實體狀態會被設定為 Deleted
        // 在呼叫 SaveChangeAsync 時會執行 DELETE 語句
        // 如果有關聯的 InventoryTransaction，會拋出 DbUpdateException
        inventorySet.Remove(inventory);
    }

    /// <summary>
    /// 查詢指定儲位的所有庫存記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定儲位 ID 的所有庫存
    /// - 不載入關聯實體（由 EF Core 延遲載入或按需載入）
    /// 
    /// 實作細節：
    /// - 使用 Where 方法過濾指定儲位的庫存
    /// - 不使用 Include 載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
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
    /// - 不包含關聯的 Sku 和 Location 實體
    /// </summary>
    /// <param name="locationId">儲位 ID</param>
    /// <param name="func">查詢條件</param>
    /// <returns>該儲位的所有庫存記錄集合</returns>
    public async Task<IEnumerable<Inventory>> GetInventoriesByLocationIdAsync(int locationId, Func<IQueryable<Inventory>, IQueryable<Inventory>>? func = null)
    {
        // ========== 第一步：取得 Inventory 的 DbSet ==========
        // context.Set<Inventory>() 取得 Inventory 實體的 DbSet
        var inventorySet = context.Set<Inventory>().AsQueryable();


        // ========== 第二步：過濾查詢條件 ==========
        if( func != null) inventorySet = func(inventorySet);

        // ========== 第三步：查詢指定儲位的所有庫存記錄 ==========
        // inventorySet.Where(i => i.LocationId == locationId) 過濾出指定儲位的庫存
        // 不使用 Include 載入關聯實體
        // ToListAsync() 執行查詢並轉換為 List
        var inventories = await inventorySet
            .Where(i => i.LocationId == locationId)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的庫存記錄集合
        // 如果沒有找到，會返回空集合
        return inventories;
    }

    /// <summary>
    /// 查詢指定 ID 的庫存記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的庫存記錄
    /// - 不載入關聯實體（由 EF Core 延遲載入或按需載入）
    /// 
    /// 實作細節：
    /// - 使用 FindAsync 方法根據主鍵查詢
    /// - 不使用 Include 載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 庫存詳情顯示
    /// - 庫存更新操作
    /// - 庫存刪除操作
    /// 
    /// 注意事項：
    /// - 如果庫存記錄不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 如需關聯實體，應使用專用查詢方法或按需載入
    /// </summary>
    /// <param name="inventoryId">庫存記錄 ID</param>
    /// <returns>查詢到的庫存實體，若不存在則回傳 null</returns>
    public async Task<Inventory?> GetInventoryAsync(int inventoryId)
    {
        // ========== 第一步：取得 Inventory 的 DbSet ==========
        // context.Set<Inventory>() 取得 Inventory 實體的 DbSet
        var inventorySet = context.Set<Inventory>();

        // ========== 第二步：根據 ID 查詢庫存記錄 ==========
        // inventorySet.FindAsync(inventoryId) 根據主鍵查詢實體
        // 不使用 Include 載入關聯實體
        // 返回值可能是 null（如果找不到對應的實體）
        var inventory = await inventorySet.FindAsync(inventoryId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的庫存實體
        // 如果找不到，回傳 null
        return inventory;
    }

    /// <summary>
    /// 查詢指定 SKU 在所有儲位的庫存記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 SKU ID 在所有儲位的庫存記錄
    /// - 不載入關聯實體（由 EF Core 延遲載入或按需載入）
    /// 
    /// 實作細節：
    /// - 使用 Where 方法過濾指定 SKU 的庫存
    /// - 不使用 Include 載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
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
    /// - 不包含關聯的 Sku 和 Location 實體
    /// </summary>
    /// <param name="skuId">SKU ID</param>
    /// <param name="func">查詢過濾條件</param>
    /// <returns>該 SKU 在所有儲位的庫存記錄集合</returns>
    public async Task<IEnumerable<Inventory>> GetInventoriesBySkuIdsync(int skuId, Func<IQueryable<Inventory>, IQueryable<Inventory>>? func = null)
    {
        // ========== 第一步：取得 Inventory 的 DbSet ==========
        // context.Set<Inventory>() 取得 Inventory 實體的 DbSet
        var inventorySet = context.Set<Inventory>().AsQueryable();

        // ========== 第二步：查詢指定 SKU 在所有儲位的庫存記錄 ==========
        if(func != null)
        {
            inventorySet = func(inventorySet);
        }

        // ========== 第三步：查詢指定 SKU 在所有儲位的庫存記錄 ==========
        // inventorySet.Where(i => i.SkuId == skuId) 過濾出指定 SKU 的庫存
        // 不使用 Include 載入關聯實體
        // ToListAsync() 執行查詢並轉換為 List
        var inventories = await inventorySet
            .Where(i => i.SkuId == skuId)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的庫存記錄集合
        // 如果沒有找到，會返回空集合
        return inventories;
    }

}
