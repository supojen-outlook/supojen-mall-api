using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Orders;

/// <summary>
/// 訂單倉儲實作類別
/// 
/// 職責：
/// - 實作 IOrderRepository 介面
/// - 處理 Order 實體的所有資料庫操作
/// - 管理 Order 與 OrderItem、Payment、Shipment、Return 的關聯關係
/// - 繼承泛型 Repository<Order> 獲得通用 CRUD 功能
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 繼承模式：複用泛型 Repository 的通用功能
/// - 依賴注入：透過建構函式注入 MainDbContext
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 Domain 層的 IOrderRepository 介面
/// - 使用 MainDbContext 與資料庫互動
/// 
/// 生命週期：
/// - 註冊為 Scoped（見 Infrastructure/DI.cs）
/// - 每個 HTTP 請求一個實例
/// 
/// 參考實作：
/// - ProductRepository：類似的擴展方法設計（AddSku、GetSkusAsync）
/// - LocationRepository：類似的擴展方法設計（AddInventory、GetInventoriesAsync）
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    /// <summary>
    /// 查詢指定訂單項目的所有揀貨項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單項目 ID 的所有揀貨項目
    /// - 不載入關聯實體（由 EF Core 延遲載入或按需載入）
    /// 
    /// 實作細節：
    /// - 使用 Where 方法過濾指定訂單項目的揀貨項目
    /// - 不使用 Include 載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 揀貨清單生成
    /// - 揀貨狀態查詢
    /// - 揀貨進度追蹤
    /// 
    /// 注意事項：
    /// - 如果訂單項目沒有揀貨項目，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含關聯的 OrderItem 和 Location 實體
    /// </summary>
    /// <param name="orderItemId">訂單項目 ID</param>
    /// <returns>該訂單項目的所有揀貨項目集合</returns>
    public async Task<IEnumerable<PickItem>> GetPickItemsByOrderItemAsync(int orderItemId)
    {
        // ========== 第一步：取得 PickItem 的 DbSet ==========
        // context.Set<PickItem>() 取得 PickItem 實體的 DbSet
        var pickItemSet = context.Set<PickItem>();

        // ========== 第二步：查詢指定訂單項目的所有揀貨項目 ==========
        // pickItemSet.Where(pi => pi.OrderItemId == orderItemId) 過濾出指定訂單項目的揀貨項目
        // OrderBy(pi => pi.CreatedAt) 按建立時間排序
        // ToListAsync() 執行查詢並轉換為 List
        var pickItems = await pickItemSet
            .Where(pi => pi.OrderItemId == orderItemId)
            .OrderBy(pi => pi.CreatedAt)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的揀貨項目集合
        // 如果沒有找到，會返回空集合
        return pickItems;
    }

    /// <summary>
    /// 查詢指定訂單的所有揀貨項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單 ID 的所有揀貨項目
    /// - 不載入關聯實體（由 EF Core 延遲載入或按需載入）
    /// 
    /// 實作細節：
    /// - 使用 Where 方法過濾指定訂單的揀貨項目
    /// - 不使用 Include 載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 訂單揀貨清單生成
    /// - 訂單揀貨狀態查詢
    /// - 訂單揀貨進度追蹤
    /// 
    /// 注意事項：
    /// - 如果訂單沒有揀貨項目，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含關聯的 OrderItem 和 Location 實體
    /// </summary>
    /// <param name="orderId">訂單 ID</param>
    /// <returns>該訂單的所有揀貨項目集合</returns>
    public async Task<IEnumerable<PickItem>> GetPickItemsByOrderAsync(int orderId)
    {
        // ========== 第一步：取得 PickItem 的 DbSet ==========
        // context.Set<PickItem>() 取得 PickItem 實體的 DbSet
        var pickItemSet = context.Set<PickItem>();

        // ========== 第二步：查詢指定訂單的所有揀貨項目 ==========
        // pickItemSet.Where(pi => pi.OrderId == orderId) 過濾出指定訂單的揀貨項目
        // OrderBy(pi => pi.CreatedAt) 按建立時間排序
        // ToListAsync() 執行查詢並轉換為 List
        var pickItems = await pickItemSet
            .Where(pi => pi.OrderId == orderId)
            .OrderBy(pi => pi.CreatedAt)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的揀貨項目集合
        // 如果沒有找到，會返回空集合
        return pickItems;
    }

    /// <summary>
    /// 新增揀貨項目
    /// 
    /// 職責：
    /// - 將揀貨項目實體加入資料庫追蹤
    /// - 設定揀貨項目與訂單項目、儲位的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保揀貨項目的 OrderId 和 OrderItemId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 創建訂單時新增揀貨項目
    /// - 為現有訂單項目新增新的揀貨項目
    /// - 拆單揀貨時新增多個揀貨項目
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證揀貨項目的 OrderId 和 OrderItemId 是否正確
    /// - 不支援更新揀貨項目（應使用 Repository 的 Update 方法）
    /// </summary>
    /// <param name="pickItem">要新增的揀貨項目實體</param>
    public void AddPickItem(PickItem pickItem)
    {
        // ========== 第一步：取得 PickItem 的 DbSet ==========
        // context.Set<PickItem>() 取得 PickItem 實體的 DbSet
        var pickItemSet = context.Set<PickItem>();

        // ========== 第二步：將揀貨項目加入追蹤 ==========
        // pickItemSet.Add(pickItem) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        pickItemSet.Add(pickItem);
    }

    /// <summary>
    /// 建構函式
    /// 
    /// 職責：
    /// - 初始化倉儲實例
    /// - 注入資料庫上下文
    /// - 傳遞給父類別 Repository<Order>
    /// 
    /// 參數說明：
    /// - context：MainDbContext 實例，用於資料庫操作
    /// 
    /// 設計考量：
    /// - 不指定主鍵屬性名稱，使用父類別預設值
    /// - 與 ProductRepository、LocationRepository 保持一致
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
    public OrderRepository(MainDbContext context) : base(context) {}

    // =========================================================================
    // 訂單項目相關方法 (Order Item Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單的所有訂單項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單 ID 的所有訂單項目
    /// - 包含關聯的 Product 和 Sku 實體
    /// - 按建立時間排序
    /// 
    /// 設計考量：
    /// - 使用 Where 方法過濾指定訂單的項目
    /// - 使用 Include 預先載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 訂單詳情頁顯示所有訂單項目
    /// - 訂單總金額計算
    /// - 訂單項目狀態更新
    /// 
    /// 注意事項：
    /// - 如果訂單沒有項目，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含退貨記錄（需使用 GetReturnAsync）
    /// </summary>
    /// <param name="orderId">訂單 ID</param>
    /// <returns>該訂單的所有訂單項目集合</returns>
    public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
    {
        // ========== 第一步：取得 OrderItem 的 DbSet ==========
        // context.Set<OrderItem>() 取得 OrderItem 實體的 DbSet
        var orderItemSet = context.Set<OrderItem>();

        // ========== 第二步：查詢指定訂單的所有訂單項目 ==========
        // orderItemSet.Where(oi => oi.OrderId == orderId) 過濾出指定訂單的項目
        // OrderBy(oi => oi.CreatedAt) 按建立時間排序
        // ToListAsync() 執行查詢並轉換為 List
        var orderItems = await orderItemSet
            .Where(oi => oi.OrderId == orderId)
            .OrderBy(oi => oi.CreatedAt)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的訂單項目集合
        // 如果沒有找到，會返回空集合
        return orderItems;
    }

    /// <summary>
    /// 查詢指定 ID 的訂單項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的訂單項目
    /// - 不載入關聯實體（由 EF Core 延遲載入或按需載入）
    /// 
    /// 實作細節：
    /// - 使用 FindAsync 方法提高查詢效率
    /// - 不使用 Include 載入關聯實體
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 退貨申請處理
    /// - 訂單項目詳情顯示
    /// - 庫存管理
    /// 
    /// 注意事項：
    /// - 如果訂單項目不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 如需關聯實體，應使用專用查詢方法或按需載入
    /// </summary>
    /// <param name="orderItemId">訂單項目 ID</param>
    /// <returns>查詢到的訂單項目實體，若不存在則回傳 null</returns>
    public async Task<OrderItem?> GetOrderItemAsync(int orderItemId)
    {
        // ========== 第一步：取得 OrderItem 的 DbSet ==========
        // context.Set<OrderItem>() 取得 OrderItem 實體的 DbSet
        var orderItemSet = context.Set<OrderItem>();

        // ========== 第二步：根據 ID 查詢訂單項目 ==========
        // 使用 FindAsync 方法根據主鍵查詢
        // 不使用 Include 載入關聯實體
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var orderItem = await orderItemSet.FindAsync(orderItemId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的訂單項目實體
        // 如果找不到，回傳 null
        return orderItem;
    }


    /// <summary>
    /// 新增訂單項目到訂單
    /// 
    /// 職責：
    /// - 將訂單項目實體加入資料庫追蹤
    /// - 設定訂單項目與訂單的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保訂單項目的 OrderId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 創建訂單時新增訂單項目
    /// - 為現有訂單新增新的訂單項目
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證訂單項目的 OrderId 是否正確
    /// - 不支援更新訂單項目（應使用 Repository 的 Update 方法）
    /// </summary>
    /// <param name="orderItem">要新增的訂單項目實體</param>
    public void AddOrderItem(OrderItem orderItem)
    {
        // ========== 第一步：取得 OrderItem 的 DbSet ==========
        // context.Set<OrderItem>() 取得 OrderItem 實體的 DbSet
        var orderItemSet = context.Set<OrderItem>();

        // ========== 第二步：將訂單項目加入追蹤 ==========
        // orderItemSet.Add(orderItem) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        orderItemSet.Add(orderItem);
    }

    // =========================================================================
    // 付款相關方法 (Payment Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單的付款記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單 ID 的付款記錄
    /// - 包含關聯的 Order 實體
    /// 
    /// 設計考量：
    /// - 使用 FindAsync 方法提高查詢效率
    /// - 包含 Order 實體以減少後續查詢
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 訂單付款狀態查詢
    /// - 付款方式確認
    /// - 退款處理
    /// 
    /// 注意事項：
    /// - 如果訂單沒有付款記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 不包含付款交易記錄（如需，應新增專用方法）
    /// </summary>
    /// <param name="orderId">訂單 ID</param>
    /// <returns>查詢到的付款記錄，若不存在則回傳 null</returns>
    public async Task<Payment?> GetPaymentAsync(int orderId)
    {
        // ========== 第一步：取得 Payment 的 DbSet ==========
        // context.Set<Payment>() 取得 Payment 實體的 DbSet
        var paymentSet = context.Set<Payment>();

        // ========== 第二步：根據訂單 ID 查詢付款記錄 ==========
        // paymentSet.FirstOrDefaultAsync(p => p.OrderId == orderId) 查詢指定訂單的付款記錄
        // 使用 FirstOrDefaultAsync 而非 FindAsync，因為我們是根據外鍵查詢，而非主鍵
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var payment = await paymentSet
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的付款記錄
        // 如果找不到，回傳 null
        return payment;
    }

    /// <summary>
    /// 新增付款記錄到訂單
    /// 
    /// 職責：
    /// - 將付款記錄實體加入資料庫追蹤
    /// - 設定付款記錄與訂單的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保付款記錄的 OrderId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 訂單付款處理
    /// - 退款記錄新增
    /// - 付款方式變更
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證付款記錄的 OrderId 是否正確
    /// - 不支援更新付款記錄（應使用 Repository 的 Update 方法）
    /// </summary>
    /// <param name="payment">要新增的付款記錄實體</param>
    public void AddPayment(Payment payment)
    {
        // ========== 第一步：取得 Payment 的 DbSet ==========
        // context.Set<Payment>() 取得 Payment 實體的 DbSet
        var paymentSet = context.Set<Payment>();

        // ========== 第二步：將付款記錄加入追蹤 ==========
        // paymentSet.Add(payment) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        paymentSet.Add(payment);
    }

        /// <summary>
        /// 刪除付款記錄
        /// 
        /// 職責：
        /// - 從資料庫刪除指定付款記錄
        /// 
        /// 設計考量：
        /// - 使用 EF Core 的變更追蹤機制
        /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
        /// 
        /// 使用場景：
        /// - 付款取消
        /// - 退款處理
        /// 
        /// 注意事項：
        /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
        /// - 建議在刪除前驗證付款記錄的存在
        /// </summary>
        public void DeletePayment(Payment payment)
        {
            // ========== 第一步：取得 Payment 的 DbSet ==========
            // context.Set<Payment>() 取得 Payment 實體的 DbSet
            var paymentSet = context.Set<Payment>();

            // ========== 第二步：將付款記錄標記為待刪除 ==========
            // if(payment != null) 檢查實體是否存在
            // 這是一種防禦性編程，避免對 null 執行操作
            // 
            // paymentSet.Remove(payment) 將實體標記為 Deleted
            // EF Core 會追蹤這個實體的狀態為 Deleted
            // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 DELETE
            // 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
            if(payment != null) paymentSet.Remove(payment);  
        }

    /// <summary>
    /// 查詢指定訂單項目的物流記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單項目 ID 的物流記錄
    /// - 包含關聯的 OrderItem 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳單一實體或 null
    /// - 使用 FirstOrDefaultAsync 而非 FindAsync，因為我們是根據外鍵查詢
    /// 
    /// 使用場景：
    /// - 訂單項目物流狀態查詢
    /// - 物流追蹤編號顯示
    /// - 出貨處理
    /// 
    /// 注意事項：
    /// - 如果訂單項目沒有物流記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 不包含物流狀態更新記錄（如需，應新增專用方法）
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 與 OrderItem 是一對多關係
    /// - 一個訂單項目可以分批出貨，產生多筆物流記錄
    /// - 此方法只回傳第一筆物流記錄（如需全部，應新增專用方法）
    /// </summary>
    /// <param name="orderId">訂單 ID</param>
    /// <returns>查詢到的物流記錄，若不存在則回傳 null</returns>
    public async Task<Shipment?> GetShipmentAsync(int orderId)
    {
        // ========== 第一步：取得 Shipment 的 DbSet ==========
        // context.Set<Shipment>() 取得 Shipment 實體的 DbSet
        // 這是 EF Core 提供的泛型方法，用於存取特定實體類型的資料集
        var shipmentSet = context.Set<Shipment>();

        // ========== 第二步：根據訂單項目 ID 查詢物流記錄 ==========
        // shipmentSet.FirstOrDefaultAsync(s => s.OrderItemId == orderItemId) 查詢指定訂單項目的物流記錄
        // 使用 FirstOrDefaultAsync 而非 FindAsync，因為我們是根據外鍵查詢，而非主鍵
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        // 
        // 設計考量：
        // - 使用 FirstOrDefaultAsync 而非 SingleOrDefaultAsync，因為一個訂單項目可能有多筆物流記錄
        // - 只回傳第一筆物流記錄（如需全部，應新增專用方法）
        // - 如果訂單項目沒有物流記錄，會返回 null
        var shipment = await shipmentSet
            .FirstOrDefaultAsync(s => s.OrderId == orderId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的物流記錄
        // 如果找不到，回傳 null
        return shipment;
    }


    /// <summary>
    /// 新增物流記錄到訂單
    /// 
    /// 職責：
    /// - 將物流記錄實體加入資料庫追蹤
    /// - 設定物流記錄與訂單的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保物流記錄的 OrderId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 訂單出貨處理
    /// - 物流方式變更
    /// - 物流追蹤編號更新
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證物流記錄的 OrderId 是否正確
    /// - 不支援更新物流記錄（應使用 Repository 的 Update 方法）
    /// </summary>
    /// <param name="shipment">要新增的物流記錄實體</param>
    public void AddShipment(Shipment shipment)
    {
        // ========== 第一步：取得 Shipment 的 DbSet ==========
        // context.Set<Shipment>() 取得 Shipment 實體的 DbSet
        var shipmentSet = context.Set<Shipment>();

        // ========== 第二步：將物流記錄加入追蹤 ==========
        // shipmentSet.Add(shipment) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        shipmentSet.Add(shipment);
    }

    // =========================================================================
    // 退貨相關方法 (Return Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單項目的退貨記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單項目 ID 的退貨記錄
    /// - 包含關聯的 OrderItem 實體
    /// 
    /// 設計考量：
    /// - 使用 FindAsync 方法提高查詢效率
    /// - 包含 OrderItem 實體以減少後續查詢
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 退貨申請查詢
    /// - 退貨狀態確認
    /// - 退款處理
    /// 
    /// 注意事項：
    /// - 如果訂單項目沒有退貨記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 不包含退貨流程記錄（如需，應新增專用方法）
    /// </summary>
    /// <param name="orderItemId">訂單項目 ID</param>
    /// <returns>查詢到的退貨記錄，若不存在則回傳 null</returns>
    public async Task<Return?> GetReturnAsync(int orderItemId)
    {
        // ========== 第一步：取得 Return 的 DbSet ==========
        // context.Set<Return>() 取得 Return 實體的 DbSet
        var returnSet = context.Set<Return>();

        // ========== 第二步：根據訂單項目 ID 查詢退貨記錄 ==========
        // returnSet.FirstOrDefaultAsync(r => r.OrderItemId == orderItemId) 查詢指定訂單項目的退貨記錄
        // 使用 FirstOrDefaultAsync 而非 FindAsync，因為我們是根據外鍵查詢，而非主鍵
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var returnItem = await returnSet
            .FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的退貨記錄
        // 如果找不到，回傳 null
        return returnItem;
    }

    /// <summary>
    /// 新增退貨記錄到訂單項目
    /// 
    /// 職責：
    /// - 將退貨記錄實體加入資料庫追蹤
    /// - 設定退貨記錄與訂單項目的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保退貨記錄的 OrderItemId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 退貨申請處理
    /// - 退貨狀態更新
    /// - 退款處理
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證退貨記錄的 OrderItemId 是否正確
    /// - 不支援更新退貨記錄（應使用 Repository 的 Update 方法）
    /// </summary>
    /// <param name="returnItem">要新增的退貨記錄實體</param>
    public void AddReturn(Return returnItem)
    {
        // ========== 第一步：取得 Return 的 DbSet ==========
        // context.Set<Return>() 取得 Return 實體的 DbSet
        var returnSet = context.Set<Return>();

        // ========== 第二步：將退貨記錄加入追蹤 ==========
        // returnSet.Add(returnItem) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        returnSet.Add(returnItem);
    }
}
