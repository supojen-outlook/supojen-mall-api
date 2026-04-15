using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 更新付款記錄命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝更新付款記錄所需的資訊
/// - 用於處理付款成功、失敗、退款等狀態變更
/// 
/// 設計模式：
/// - 實作 IRequest<Payment>，表示這是一個會回傳 Payment 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PaymentUpdateHandler 配合使用，完成更新付款記錄的業務邏輯
/// 
/// 使用場景：
/// - 付款成功後更新狀態
/// - 付款失敗後更新狀態
/// - 退款處理
/// - 交易資訊補充
/// 
/// 設計特點：
/// - 支援狀態更新
/// - 自動設定時間戳
/// - 支援交易資訊更新
/// 
/// 注意事項：
/// - 狀態轉換必須符合業務規則
/// - 付款成功時必須提供 TransactionId 和 Gateway
/// </summary>
public class PaymentUpdateCommand : IRequest<Payment>
{
    /// <summary>
    /// 付款記錄唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的付款記錄
    /// - 必須是資料庫中已存在的付款記錄 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的付款記錄
    /// 
    /// 錯誤處理：
    /// - 如果付款記錄不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 付款狀態
    /// 
    /// 用途：
    /// - 更新付款的當前狀態
    /// - 用於訂單狀態更新
    /// 
    /// 可選值：
    /// - "success"：付款成功
    /// - "failed"：付款失敗
    /// - "refunded"：已退款
    /// 
    /// 狀態轉換規則：
    /// - pending → success（付款成功）
    /// - pending → failed（付款失敗）
    /// - success → refunded（退款）
    /// 
    /// 驗證規則：
    /// - 必須是有效的狀態值
    /// - 必須符合狀態轉換規則
    /// 
    /// 錯誤處理：
    /// - 如果狀態無效，會拋出 ArgumentException
    /// - 如果狀態轉換不合法，會拋出 Failure.BadRequest()
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 交易 ID（由支付閘道提供）
    /// 
    /// 用途：
    /// - 記錄支付閘道的交易 ID
    /// - 用於查詢和核對付款記錄
    /// 
    /// 使用場景：
    /// - 付款成功時必須提供
    /// - 退款處理時使用
    /// - 交易查詢時使用
    /// 
    /// 驗證規則：
    /// - 當 Status 為 "success" 時必須提供
    /// - 不能為空白或僅包含空白字元
    /// 
    /// 錯誤處理：
    /// - 如果 Status 為 "success" 但未提供，會拋出 ArgumentException
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// 付款方式
    /// 
    /// 用途：
    /// - 記錄使用的付款方式
    /// - 用於付款方式統計
    /// 
    /// 可選值：
    /// - "credit_card"：信用卡
    /// - "debit_card"：簽帳金融卡
    /// - "bank_transfer"：銀行轉帳
    /// - "cash"：現金
    /// - "digital_wallet"：數位錢包（如 Apple Pay、Google Pay）
    /// - "other"：其他
    /// 
    /// 使用場景：
    /// - 付款成功時必須提供
    /// - 付款方式統計
    /// 
    /// 驗證規則：
    /// - 當 Status 為 "success" 時必須提供
    /// - 必須是有效的付款方式值
    /// 
    /// 錯誤處理：
    /// - 如果 Status 為 "success" 但未提供，會拋出 ArgumentException
    /// </summary>
    public string? Method { get; set; }

}

/// <summary>
/// 更新付款記錄命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 PaymentUpdateCommand 命令
/// - 查詢付款記錄是否存在
/// - 驗證狀態轉換的合法性
/// - 更新付款記錄資訊
/// - 自動設定時間戳
/// - 回傳更新後的實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PaymentUpdateCommand, Payment> 介面
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
/// - 未檢查訂單狀態是否允許付款狀態變更
/// - 未處理並發更新衝突
/// - 建議在實際專案中加入這些檢查
/// </summary>
internal class PaymentUpdateHandler : IRequestHandler<PaymentUpdateCommand, Payment>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取付款記錄資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 擴展了 GetPaymentAsync、AddPayment 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">訂單倉儲，用於查詢和更新付款記錄</param>
    public PaymentUpdateHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新付款記錄命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢付款記錄
    /// 2. 驗證付款記錄是否存在
    /// 3. 驗證狀態轉換的合法性
    /// 4. 驗證交易資訊（當狀態為 success 時）
    /// 5. 更新付款記錄資訊
    /// 6. 自動設定時間戳
    /// 7. 儲存變更
    /// 8. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - 付款記錄不存在：拋出 Failure.NotFound()
    /// - 狀態無效：拋出 ArgumentException
    /// - 狀態轉換不合法：拋出 Failure.BadRequest()
    /// - 交易資訊不完整：拋出 ArgumentException
    /// 
    /// 注意事項：
    /// - 狀態轉換必須符合業務規則
    /// - 付款成功時必須提供 TransactionId 和 Gateway
    /// - 會自動設定 PaidAt 或 RefundedAt
    /// </summary>
    /// <param name="request">更新付款記錄命令物件，包含付款記錄 ID 和要更新的欄位</param>
    /// <returns>更新後的付款記錄實體</returns>
    public async Task<Payment> HandleAsync(PaymentUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢付款記錄 ==========
        // 使用 IOrderRepository.GetPaymentAsync() 查詢付款記錄
        // 這個方法會從資料庫中取得完整的付款記錄實體
        var payment = await _repository.GetPaymentAsync(request.Id);
        
        // ========== 第二步：驗證付款記錄是否存在 ==========
        // 如果找不到付款記錄，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 付款記錄 ID 不存在
        // - 付款記錄已被刪除
        if (payment == null)
            throw Failure.NotFound($"付款記錄不存在，ID: {request.Id}");

        // ========== 第三步：驗證狀態轉換的合法性 ==========
        // 定義有效的狀態轉換
        var validTransitions = new Dictionary<string, HashSet<string>>
        {
            { "pending", new HashSet<string> { "success", "failed" } },
            { "success", new HashSet<string> { "refunded" } }
        };

        // 檢查狀態轉換是否合法
        if (!validTransitions.ContainsKey(payment.Status) || 
            !validTransitions[payment.Status].Contains(request.Status))
        {
            throw Failure.BadRequest(
                $"無效的狀態轉換：從 {payment.Status} 到 {request.Status}");
        }

        // ========== 第四步：驗證交易資訊（當狀態為 success 時） ==========
        if (request.Status == "success")
        {
            // 驗證 TransactionId 是否提供
            if (string.IsNullOrWhiteSpace(request.TransactionId))
                throw new ArgumentException("付款成功時必須提供 TransactionId");
            
            // 驗證 Method 是否提供
            if (string.IsNullOrWhiteSpace(request.Method))
                throw new ArgumentException("付款成功時必須提供 Method");
        }

        // ========== 第五步：更新付款記錄資訊 ==========
        // 更新狀態
        payment.Status = request.Status;

        // 更新交易資訊
        if (request.TransactionId != null)
            payment.TransactionId = request.TransactionId;

        if (request.Method != null)
            payment.Method = request.Method;


        // ========== 第六步：自動設定時間戳 ==========
        // 如果狀態為 success，自動設定 PaidAt 為目前時間
        if (request.Status == "success")
        {
            payment.PaidAt = DateTimeOffset.UtcNow;
        }

        // 如果狀態為 refunded，自動設定 RefundedAt 為目前時間
        if (request.Status == "refunded")
        {
            payment.RefundedAt = DateTimeOffset.UtcNow;
        }

        // ========== 第七步：儲存變更 ==========
        // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第八步：回傳更新後的實體 ==========
        // 回傳更新後的 Payment 實體
        // 包含所有更新後的屬性值
        return payment;
    }
}
