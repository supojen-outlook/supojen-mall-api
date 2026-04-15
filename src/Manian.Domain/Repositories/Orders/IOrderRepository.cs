using System;
using Manian.Domain.Entities.Orders;

namespace Manian.Domain.Repositories.Orders;

/// <summary>
/// 訂單倉儲介面
/// 
/// 職責：
/// - 定義訂單相關的資料存取操作
/// - 繼承泛型 IRepository<Order> 獲得通用 CRUD 功能
/// - 擴展訂單項目、付款、物流、退貨等特定查詢和操作方法
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 介面隔離原則（ISP）：只定義特定方法，不暴露不必要功能
/// - 依賴反轉原則（DIP）：依賴抽象而非實作
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 由 Infrastructure 層的 OrderRepository 實作
/// - 被 Application 層的 Query/Command 使用
/// 
/// 使用場景：
/// - 訂單管理（新增、查詢、更新、刪除）
/// - 訂單項目管理（新增、查詢）
/// - 付款處理（新增、查詢）
/// - 物流處理（新增、查詢）
/// - 退貨處理（新增、查詢）
/// 
/// 關聯實體：
/// - Order：訂單實體（主實體）
/// - OrderItem：訂單項目實體（關聯實體）
/// - Payment：付款實體（關聯實體）
/// - Shipment：物流實體（關聯實體）
/// - Return：退貨實體（關聯實體）
/// 
/// 設計特點：
/// - 提供訂單項目、付款、物流、退貨相關的特定方法
/// - 支援訂單項目的新增和查詢（AddOrderItem、GetOrderItemsAsync）
/// - 支援付款的新增和查詢（AddPayment、GetPaymentAsync）
/// - 支援物流的新增和查詢（AddShipment、GetShipmentAsync）
/// - 支援退貨的新增和查詢（AddReturn、GetReturnAsync）
/// 
/// 參考實作：
/// - IProductRepository：類似的擴展方法設計（AddSku、GetSkusAsync）
/// - IAttributeKeyRepository：類似的擴展方法設計（AddValue、GetValuesAsync）
/// - ILocationRepository：類似的擴展方法設計（AddInventory、GetInventoriesAsync）
/// </summary>
public interface IOrderRepository : IRepository<Order>
{

    // =========================================================================
    // 挑貨項目相關方法 (Pick Item Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單項目的所有挑貨項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單項目 ID 的所有挑貨項目
    /// - 包含關聯的 Sku 實體（由實作決定）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 不包含分頁邏輯（如需要，應新增專用查詢方法）
    /// 
    /// 使用場景：
    /// - 訂單項目詳情頁顯示所有挑貨項目
    /// - 訂單項目庫存管理
    /// - 訂單項目狀態更新
    /// 
    /// 注意事項：
    /// - 如果訂單項目沒有挑貨項目，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含退貨記錄（需使用 GetReturnAsync）
    /// </summary>
    Task<IEnumerable<PickItem>> GetPickItemsByOrderItemAsync(int orderItemId);

    /// <summary>
    /// 查詢指定訂單的所有挑貨項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單 ID 的所有挑貨項目
    /// - 包含關聯的 Sku 實體（由實作決定）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 不包含分頁邏輯（如需要，應新增專用查詢方法）
    /// 
    /// 使用場景：
    /// - 訂單詳情頁顯示所有挑貨項目
    /// - 訂單項目庫存管理
    /// - 訂單項目狀態更新
    /// 
    /// 注意事項：
    /// - 如果訂單沒有挑貨項目，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含退貨記錄（需使用 GetReturnAsync）
    /// </summary>
    Task<IEnumerable<PickItem>> GetPickItemsByOrderAsync(int orderId);

    /// <summary>
    /// 新增挑貨項目到訂單項目
    /// 
    /// 職責：
    /// - 將挑貨項目實體加入資料庫追蹤
    /// - 設定挑貨項目與訂單項目的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保挑貨項目的 OrderItemId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 創建訂單項目時新增挑貨項目
    /// - 為現有訂單項目新增新的挑貨項目
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證挑貨項目的 OrderItemId 是否正確
    /// - 不支援更新挑貨項目（應使用 Repository 的 Update 方法）
    /// </summary>
    void AddPickItem(PickItem pickItem);

    // =========================================================================
    // 訂單項目相關方法 (Order Item Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單的所有訂單項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單 ID 的所有訂單項目
    /// - 包含關聯的 Product 和 Sku 實體（由實作決定）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 不包含分頁邏輯（如需要，應新增專用查詢方法）
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
    Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);

    /// <summary>
    /// 查詢指定 ID 的訂單項目
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的訂單項目
    /// - 包含關聯的 Order、Product 和 Sku 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 包含關聯實體以減少後續查詢
    /// - 使用 FindAsync 方法提高查詢效率
    /// 
    /// 使用場景：
    /// - 退貨申請處理
    /// - 訂單項目詳情顯示
    /// - 庫存管理
    /// 
    /// 注意事項：
    /// - 如果訂單項目不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// </summary>
    /// <param name="orderItemId">訂單項目 ID</param>
    /// <returns>查詢到的訂單項目實體，若不存在則回傳 null</returns>
    Task<OrderItem?> GetOrderItemAsync(int orderItemId);

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
    void AddOrderItem(OrderItem orderItem);

    // =========================================================================
    // 付款相關方法 (Payment Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單的付款記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單 ID 的付款記錄
    /// - 包含關聯的 Order 實體（由實作決定）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳單一實體而非集合
    /// - 一個訂單通常只有一筆付款記錄
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
    Task<Payment?> GetPaymentAsync(int orderId);

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
    void AddPayment(Payment payment);

    // =========================================================================
    // 物流相關方法 (Shipment Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單項目的物流記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單項目 ID 的物流記錄
    /// - 包含關聯的 OrderItem 實體（由實作決定）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳單一實體或 null
    /// - 一個訂單項目可能有多筆物流記錄（分批出貨）
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
    Task<Shipment?> GetShipmentAsync(int orderId);


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
    void AddShipment(Shipment shipment);

    // =========================================================================
    // 退貨相關方法 (Return Related Methods)
    // =========================================================================

    /// <summary>
    /// 查詢指定訂單項目的退貨記錄
    /// 
    /// 職責：
    /// - 從資料庫查詢指定訂單項目 ID 的退貨記錄
    /// - 包含關聯的 OrderItem 實體（由實作決定）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳單一實體而非集合
    /// - 一個訂單項目通常只有一筆退貨記錄
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
    Task<Return?> GetReturnAsync(int orderItemId);

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
    void AddReturn(Return returnItem);
}
