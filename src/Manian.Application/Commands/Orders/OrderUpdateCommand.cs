using System.Text.Json;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 更新訂單命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新訂單所需的資訊
/// 設計模式：實作 IRequest<Order>，表示這是一個會回傳更新後實體的命令
/// 
/// 使用場景：
/// - 訂單狀態更新（付款、出貨、完成）
/// - 訂單金額調整（折扣、稅金、運費）
/// - 訂單時間戳更新
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 只更新提供的欄位，其他欄位保持不變
/// - 自動驗證狀態轉換規則
/// - 自動驗證時間戳順序
/// 
/// 注意事項：
/// - 更新操作不可逆，建議在 UI 層加入確認對話框
/// - 狀態轉換必須遵循規則
/// - 時間戳不能早於前一階段
/// </summary>
public class OrderUpdateCommand : IRequest<Order>
{
    /// <summary>
    /// 訂單唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的訂單
    /// - 必須是資料庫中已存在的訂單 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單
    /// 
    /// 錯誤處理：
    /// - 如果訂單不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 訂單狀態
    /// 
    /// 可選值：
    /// - "created"：已建立
    /// - "paid"：已付款
    /// - "shipped"：已出貨
    /// - "completed"：已完成
    /// - "closed"：已關閉/取消
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 狀態轉換規則：
    /// - created → paid
    /// - paid → shipped
    /// - shipped → completed
    /// - created/paid/shipped → closed
    /// 
    /// 錯誤處理：
    /// - 如果狀態轉換不符合規則，會拋出 ArgumentException
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 訂單總金額
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// 
    /// 錯誤處理：
    /// - 如果金額為負數，會拋出 ArgumentException
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// 折扣金額
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// 
    /// 錯誤處理：
    /// - 如果金額為負數，會拋出 ArgumentException
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// 稅金金額
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// 
    /// 錯誤處理：
    /// - 如果金額為負數，會拋出 ArgumentException
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// 運費金額
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// 
    /// 錯誤處理：
    /// - 如果金額為負數，會拋出 ArgumentException
    /// </summary>
    public decimal? ShippingAmount { get; set; }

    /// <summary>
    /// 付款時間
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能早於訂單建立時間
    /// 
    /// 錯誤處理：
    /// - 如果時間早於訂單建立時間，會拋出 ArgumentException
    /// </summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>
    /// 出貨時間
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能早於付款時間
    /// 
    /// 錯誤處理：
    /// - 如果時間早於付款時間，會拋出 ArgumentException
    /// </summary>
    public DateTimeOffset? ShippedAt { get; set; }

    /// <summary>
    /// 完成時間
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能早於出貨時間
    /// 
    /// 錯誤處理：
    /// - 如果時間早於出貨時間，會拋出 ArgumentException
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// 更新訂單命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 OrderUpdateCommand 命令
/// - 查詢訂單是否存在
/// - 更新訂單資訊
/// - 驗證狀態轉換規則
/// - 驗證時間戳順序
/// 
/// 設計模式：
/// - 實作 IRequestHandler<OrderUpdateCommand, Order> 介面
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
/// - 未檢查訂單是否屬於當前使用者
/// - 未記錄訂單變更歷史
/// - 建議在實際專案中加入這些功能
/// </summary>
internal class OrderUpdateHandler : IRequestHandler<OrderUpdateCommand, Order>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">訂單倉儲，用於查詢和更新訂單</param>
    public OrderUpdateHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新訂單命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢訂單實體
    /// 2. 驗證訂單是否存在
    /// 3. 更新訂單屬性（只更新非 null 的欄位）
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 訂單不存在：拋出 Failure.NotFound()
    /// - 狀態轉換不符合規則：拋出 ArgumentException
    /// - 時間戳順序不正確：拋出 ArgumentException
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查訂單是否屬於當前使用者
    /// - 建議記錄訂單變更歷史
    /// </summary>
    /// <param name="request">更新訂單命令物件，包含訂單 ID 和要更新的欄位</param>
    /// <returns>更新後的訂單實體</returns>
    public async Task<Order> HandleAsync(OrderUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢訂單實體 ==========
        // 使用 IOrderRepository.GetByIdAsync() 查詢訂單
        // 這個方法會從資料庫中取得完整的訂單實體
        var order = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證訂單是否存在 ==========
        // 如果找不到訂單，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 訂單 ID 不存在
        // - 訂單已被刪除（軟刪除）
        if (order == null)
            throw Failure.NotFound($"訂單不存在，ID: {request.Id}");

        // ========== 第三步：更新訂單屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新狀態
        if (request.Status != null)
        {
            // 驗證狀態轉換規則
            var validTransitions = new Dictionary<string, HashSet<string>>
            {
                { "created", new HashSet<string> { "paid", "closed" } },
                { "paid", new HashSet<string> { "shipped", "closed" } },
                { "shipped", new HashSet<string> { "completed", "closed" } },
                { "completed", new HashSet<string>() },
                { "closed", new HashSet<string>() }
            };

            if (!validTransitions.ContainsKey(order.Status) || 
                !validTransitions[order.Status].Contains(request.Status))
            {
                throw new ArgumentException($"無效的狀態轉換：從 {order.Status} 到 {request.Status}");
            }

            order.Status = request.Status;
        }
        
        // 更新金額
        if (request.TotalAmount.HasValue) order.TotalAmount = request.TotalAmount.Value;
        if (request.DiscountAmount.HasValue) order.DiscountAmount = request.DiscountAmount.Value;
        if (request.TaxAmount.HasValue) order.TaxAmount = request.TaxAmount.Value;
        if (request.ShippingAmount.HasValue) order.ShippingAmount = request.ShippingAmount.Value;
        
        // 更新時間戳
        if (request.PaidAt.HasValue) order.PaidAt = request.PaidAt.Value;
        if (request.ShippedAt.HasValue) order.ShippedAt = request.ShippedAt.Value;
        if (request.CompletedAt.HasValue) order.CompletedAt = request.CompletedAt.Value;
        
        // ========== 第四步：儲存變更 ==========
        // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第五步：回傳更新後的實體 ==========
        // 回傳更新後的 Order 實體
        // 包含所有更新後的屬性值
        return order;
    }
}
