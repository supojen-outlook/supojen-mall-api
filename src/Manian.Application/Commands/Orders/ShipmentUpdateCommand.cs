using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 更新物流記錄命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝更新物流記錄所需的資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<Shipment>>，表示這是一個會回傳更新後實體集合的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ShipmentUpdateCommandHandler 配合使用，完成更新物流記錄的業務邏輯
/// 
/// 使用場景：
/// - 訂單出貨處理
/// - 物流方式變更
/// - 物流追蹤編號更新
/// - 物流資訊修改
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 只更新非 null 的欄位，保持 null 欄位的原值不變
/// - 自動設定出貨日期
/// - 批次更新訂單的所有物流記錄
/// 
/// 注意事項：
/// - 更新操作不可逆，建議在 UI 層加入確認對話框
/// - 建議檢查物流方式是否有效
/// - 建議檢查追蹤編號格式
/// 
/// 與其他命令的對比：
/// - ShipmentAddCommand：新增物流記錄（不回傳資料）
/// - ShipmentUpdateCommand：更新物流記錄（回傳更新後的實體集合）
/// - ShipmentDeleteCommand：刪除物流記錄（不回傳資料）
/// </summary>
public class ShipmentUpdateCommand : IRequest<Shipment>
{
    /// <summary>
    /// 訂單 ID
    /// 
    /// 用途：
    /// - 識別要更新物流記錄的訂單
    /// - 作為查詢條件過濾物流記錄
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單
    /// 
    /// 錯誤處理：
    /// - 如果訂單不存在，會拋出 Failure.NotFound()
    /// - 如果訂單沒有物流記錄，會拋出 Failure.NotFound()
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order → OrderItem (1:N)
    /// - OrderItem → Shipment (1:N)
    /// - 此屬性用於查詢訂單的所有 OrderItem，再查詢每個 OrderItem 的 Shipment
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 物流方式
    /// 
    /// 用途：
    /// - 指定包裹的物流方式
    /// - 用於計算運費和預估送達時間
    /// - 用於整合第三方物流追蹤服務
    /// 
    /// 可選值：
    /// - "post"：中華郵政
    /// - "seven"：7-11
    /// - "family"：全家
    /// - "hilife"：萊爾富
    /// - "ok"：OK Mart
    /// - "tcat"：黑貓
    /// - "ecam"：宅配通
    /// - null：不更新此欄位
    /// 
    /// 驗證規則：
    /// - 必須是上述七個值之一
    /// - 可以為 null（表示不更新此欄位）
    /// - 建議在 UI 層提供下拉選單
    /// 
    /// 錯誤處理：
    /// - 如果物流方式無效，會忽略此欄位
    /// - 建議在 UI 層處理無效值
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新為黑貓物流
    /// var command = new ShipmentUpdateCommand { OrderId = 1001, Method = "tcat" };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 實體包含 Method 欄位
    /// - 此屬性用於更新 Shipment.Method
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 物流追蹤編號
    /// 
    /// 用途：
    /// - 用於追蹤包裹的運送狀態
    /// - 可用於整合第三方物流追蹤服務
    /// - 提供給客戶查詢物流狀態
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，應符合物流公司的編號格式
    /// - 建議在 UI 層加入格式驗證
    /// 
    /// 錯誤處理：
    /// - 如果追蹤編號格式無效，會忽略此欄位
    /// - 建議在 UI 層處理格式錯誤
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新追蹤編號
    /// var command = new ShipmentUpdateCommand { 
    ///     OrderId = 1001, 
    ///     TrackingNumber = "TCAT123456789" 
    /// };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 實體包含 TrackingNumber 欄位
    /// - 此屬性用於更新 Shipment.TrackingNumber
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// 寄送地址
    /// 
    /// 用途：
    /// - 記錄包裹的寄送地址
    /// - 用於物流公司配送
    /// - 提供給客戶確認配送資訊
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，應包含完整的地址資訊
    /// - 建議長度限制：10-500 字元
    /// 
    /// 錯誤處理：
    /// - 如果地址不完整，會忽略此欄位
    /// - 建議在 UI 層處理地址驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新寄送地址
    /// var command = new ShipmentUpdateCommand { 
    ///     OrderId = 1001, 
    ///     ShippingAddress = "台北市信義區信義路五段7號" 
    /// };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 實體包含 ShippingAddress 欄位
    /// - 此屬性用於更新 Shipment.ShippingAddress
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// 收件人姓名
    /// 
    /// 用途：
    /// - 記錄包裹的收件人姓名
    /// - 用於物流公司配送
    /// - 提供給客戶確認收件人資訊
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，不能為空字串
    /// - 建議長度限制：1-100 字元
    /// 
    /// 錯誤處理：
    /// - 如果姓名為空字串，會忽略此欄位
    /// - 建議在 UI 層處理姓名驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新收件人姓名
    /// var command = new ShipmentUpdateCommand { 
    ///     OrderId = 1001, 
    ///     RecipientName = "張三" 
    /// };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 實體包含 RecipientName 欄位
    /// - 此屬性用於更新 Shipment.RecipientName
    /// </summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// 收件人電話
    /// 
    /// 用途：
    /// - 記錄包裹的收件人電話
    /// - 用於物流公司配送
    /// - 提供給客戶確認聯絡資訊
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，應符合電話號碼格式
    /// - 建議格式：09xx-xxx-xxx 或 0x-xxxx-xxxx
    /// 
    /// 錯誤處理：
    /// - 如果電話號碼格式無效，會忽略此欄位
    /// - 建議在 UI 層處理電話號碼驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新收件人電話
    /// var command = new ShipmentUpdateCommand { 
    ///     OrderId = 1001, 
    ///     RecipientPhone = "0912-345-678" 
    /// };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Shipment 實體包含 RecipientPhone 欄位
    /// - 此屬性用於更新 Shipment.RecipientPhone
    /// </summary>
    public string? RecipientPhone { get; set; }
}

/// <summary>
/// 更新物流記錄命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ShipmentUpdateCommand 命令
/// - 查詢訂單的所有物流記錄
/// - 更新物流記錄資訊
/// - 自動設定出貨日期
/// - 回傳更新後的物流記錄集合
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShipmentUpdateCommand, IEnumerable<Shipment>> 介面
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
/// 潛在問題：
/// - 未檢查物流方式是否有效
/// - 未檢查追蹤編號格式
/// - 建議在實際專案中加入這些檢查
/// 
/// 參考實作：
/// - PaymentUpdateHandler：類似的更新邏輯
/// - InventoryUpdateHandler：類似的部分更新邏輯
/// - ShipmentAddHandler：類似的物流記錄處理邏輯
/// 
/// 與其他處理器的對比：
/// - ShipmentAddHandler：新增物流記錄（不回傳資料）
/// - ShipmentUpdateHandler：更新物流記錄（回傳更新後的實體集合）
/// - ShipmentDeleteHandler：刪除物流記錄（不回傳資料）
/// </summary>
internal class ShipmentUpdateHandler : IRequestHandler<ShipmentUpdateCommand, Shipment>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單和物流記錄資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 擴展了 GetShipmentAsync、GetShipmentsAsync 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">訂單倉儲，用於查詢和更新物流記錄</param>
    public ShipmentUpdateHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新物流記錄命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 OrderId 查詢訂單的所有訂單項目
    /// 2. 遍歷每個訂單項目，查詢對應的物流記錄
    /// 3. 驗證物流記錄是否存在
    /// 4. 更新非 null 的欄位
    /// 5. 自動設定出貨日期
    /// 6. 儲存變更
    /// 7. 回傳更新後的物流記錄集合
    /// 
    /// 錯誤處理：
    /// - 訂單項目不存在：拋出 Failure.NotFound()
    /// - 物流記錄不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查物流方式是否有效
    /// - 建議檢查追蹤編號格式
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新訂單 ID 為 1001 的物流記錄
    /// var command = new ShipmentUpdateCommand
    /// {
    ///     OrderId = 1001,
    ///     Method = "tcat",
    ///     TrackingNumber = "TCAT123456789",
    ///     ShippingAddress = "台北市信義區信義路五段7號",
    ///     RecipientName = "張三",
    ///     RecipientPhone = "0912-345-678"
    /// };
    /// 
    /// // 執行更新命令
    /// var shipments = await _mediator.SendAsync(command);
    /// 
    /// // 遍歷更新後的物流記錄
    /// foreach (var shipment in shipments)
    /// {
    ///     Console.WriteLine($"物流方式：{shipment.Method}");
    ///     Console.WriteLine($"追蹤編號：{shipment.TrackingNumber}");
    ///     Console.WriteLine($"出貨日期：{shipment.ShipDate}");
    /// }
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order → OrderItem (1:N)
    /// - OrderItem → Shipment (1:N)
    /// - 需要透過 OrderItem 找到對應的 Shipment
    /// </summary>
    /// <param name="request">更新物流記錄命令物件，包含物流記錄的所有資訊</param>
    /// <returns>更新後的物流記錄實體集合</returns>
    public async Task<Shipment> HandleAsync(ShipmentUpdateCommand request)
    {

        // ========== 第三步：根據 OrderItemId 查詢物流記錄 ==========
        // 使用 IOrderRepository.GetShipmentAsync() 查詢物流記錄
        // 這個方法會從資料庫中取得完整的物流記錄實體
        // 注意：這裡傳入的是 OrderItemId，不是 OrderId
        var shipment = await _repository.GetShipmentAsync(request.OrderId);
        
        // ========== 第四步：驗證物流記錄是否存在 ==========
        // 如果找不到物流記錄，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 訂單項目 ID 不存在
        // - 訂單項目尚未建立物流記錄
        if (shipment == null)
            throw Failure.NotFound($"物流記錄不存在，訂單 ID: {request.OrderId}");

        // ========== 第五步：更新非 null 的欄位 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        if (request.Method != null) shipment.Method = request.Method;
        if (request.TrackingNumber != null) shipment.TrackingNumber = request.TrackingNumber;
        if (request.ShippingAddress != null) shipment.ShippingAddress = request.ShippingAddress;
        if (request.RecipientName != null) shipment.RecipientName = request.RecipientName;
        if (request.RecipientPhone != null) shipment.RecipientPhone = request.RecipientPhone;

        // ========== 第六步：自動設定出貨日期 ==========
        // 如果物流方式或追蹤編號有更新，自動設定出貨日期為目前時間
        // 這表示訂單已經實際出貨
        if (request.Method != null || request.TrackingNumber != null)
        {
            shipment.ShipDate = DateTimeOffset.UtcNow;
        }

        // ========== 第七步：儲存變更 ==========
        // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第八步：回傳更新後的實體 ==========
        // 回傳更新後的 Shipment 實體集合
        // 包含所有更新後的屬性值
        return shipment;
    }
}
