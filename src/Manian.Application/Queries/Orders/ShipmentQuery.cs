using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢單一物流記錄的請求物件
/// 
/// 用途：
/// - 根據訂單項目 ID 查詢該項目的物流記錄
/// - 用於訂單詳情頁顯示物流資訊
/// - 用於物流狀態追蹤
/// 
/// 設計模式：
/// - 實作 IRequest<Shipment?>，表示這是一個查詢請求
/// - 回傳 Shipment 實體或 null（如果不存在）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ShipmentQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 訂單詳情頁顯示物流資訊
/// - 物流狀態追蹤頁面
/// - 出貨處理頁面
/// - 訂單管理後台
/// 
/// 資料關聯：
/// - 根據 sql/04-order/README.md 的五表關係圖
/// - Shipment 與 OrderItem 是一對多關係
/// - 一個訂單項目可以有多筆物流記錄（分批出貨）
/// - 此方法根據 OrderItemId 查詢物流記錄
/// 
/// 設計特點：
/// - 簡單直接的查詢，只根據 OrderItemId 過濾
/// - 不支援分頁（假設一個訂單項目的物流記錄數量有限）
/// - 不支援排序（由 Repository 預設按 CreatedAt 排序）
/// 
/// 參考實作：
/// - PaymentQuery：查詢訂單的付款記錄（需要 OrderId）
/// - OrderQuery：查詢單一訂單詳情（需要 OrderId）
/// </summary>
public class ShipmentQuery : IRequest<Shipment?>
{
    /// <summary>
    /// 訂單項目 ID
    /// 
    /// 用途：
    /// - 識別要查詢物流記錄的訂單項目
    /// - 作為查詢條件過濾物流記錄
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單項目
    /// 
    /// 錯誤處理：
    /// - 如果訂單項目不存在，會返回 null
    /// - 如果訂單項目沒有物流記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單項目 ID 為 1001 的物流記錄
    /// var query = new ShipmentQuery { OrderItemId = 1001 };
    /// var shipment = await _mediator.SendAsync(query);
    /// if (shipment != null)
    /// {
    ///     // 顯示物流資訊
    ///     Console.WriteLine($"物流方式：{shipment.Method}");
    ///     Console.WriteLine($"追蹤編號：{shipment.TrackingNumber}");
    ///     Console.WriteLine($"出貨日期：{shipment.ShipDate}");
    /// }
    /// else
    /// {
    ///     // 處理沒有物流記錄的情況
    ///     Console.WriteLine("該訂單項目沒有物流記錄");
    /// }
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 與 OrderItem 是一對多關係
    /// - 一個訂單項目可以有多筆物流記錄（分批出貨）
    /// - 此屬性用於關聯 OrderItem 和 Shipment
    /// </summary>
    public int OrderId { get; init; }
}

/// <summary>
/// 物流記錄查詢處理器
/// 
/// 職責：
/// - 接收 ShipmentQuery 請求
/// - 呼叫 Repository 查詢該訂單項目的物流記錄
/// - 回傳物流記錄實體或 null
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShipmentQuery, Shipment?> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IOrderRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 簡單直接的查詢邏輯
/// - 不包含複雜的篩選、排序、分頁
/// - 依賴 Repository 的實作細節
/// 
/// 參考實作：
/// - PaymentQueryHandler：查詢訂單的付款記錄
/// - OrderQueryHandler：查詢單一訂單詳情
/// </summary>
public class ShipmentQueryHandler : IRequestHandler<ShipmentQuery, Shipment?>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單和物流記錄資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 擴展了 GetShipmentAsync 方法專門查詢訂單項目的物流記錄
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 與 OrderItem 是一對多關係
    /// - 一個訂單項目可以有多筆物流記錄（分批出貨）
    /// - GetShipmentAsync 方法根據 OrderItemId 查詢物流記錄
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢物流記錄資料</param>
    public ShipmentQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// 處理物流記錄查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 ShipmentQuery 請求
    /// 2. 呼叫 Repository 的 GetShipmentAsync 方法
    /// 3. 回傳該訂單項目的物流記錄實體或 null
    /// 
    /// 查詢特性：
    /// - 根據 OrderItemId 過濾物流記錄
    /// - 包含關聯的 OrderItem 實體（由 Repository 實作決定）
    /// - 不支援分頁（假設一個訂單項目的物流記錄數量有限）
    /// 
    /// 錯誤處理：
    /// - 如果訂單項目不存在，會返回 null
    /// - 如果訂單項目沒有物流記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單項目 ID 為 1001 的物流記錄
    /// var query = new ShipmentQuery { OrderItemId = 1001 };
    /// var shipment = await _mediator.SendAsync(query);
    /// if (shipment != null)
    /// {
    ///     // 顯示物流資訊
    ///     Console.WriteLine($"物流方式：{shipment.Method}");
    ///     Console.WriteLine($"追蹤編號：{shipment.TrackingNumber}");
    ///     Console.WriteLine($"出貨日期：{shipment.ShipDate}");
    ///     Console.WriteLine($"收件人：{shipment.RecipientName}");
    ///     Console.WriteLine($"收件人電話：{shipment.RecipientPhone}");
    ///     Console.WriteLine($"寄送地址：{shipment.ShippingAddress}");
    /// }
    /// else
    /// {
    ///     // 處理沒有物流記錄的情況
    ///     Console.WriteLine("該訂單項目沒有物流記錄");
    /// }
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 與 OrderItem 是一對多關係
    /// - 一個訂單項目可以有多筆物流記錄（分批出貨）
    /// - GetShipmentAsync 方法根據 OrderItemId 查詢物流記錄
    /// </summary>
    /// <param name="request">物流記錄查詢請求物件，包含 OrderItemId</param>
    /// <returns>該訂單項目的物流記錄實體，若不存在則回傳 null</returns>
    public async Task<Shipment?> HandleAsync(ShipmentQuery request)
    {
        // 呼叫 Repository 的 GetShipmentAsync 方法查詢該訂單項目的物流記錄
        // 這個方法會：
        // 1. 從資料庫查詢指定訂單項目 ID 的物流記錄
        // 2. 包含關聯的 OrderItem 實體（由 Repository 實作決定）
        // 3. 回傳物流記錄實體或 null（如果找不到）
        return await _orderRepository.GetShipmentAsync(
            request.OrderId
        );
    }
}
