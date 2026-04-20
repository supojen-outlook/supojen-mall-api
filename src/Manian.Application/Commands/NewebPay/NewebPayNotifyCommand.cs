using System.Text.Json;
using Manian.Application.Models.NewebPay;
using Manian.Application.Services;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.NewebPay;

/// <summary>
/// 藍新金流付款回調命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝藍新金流平台回傳的付款結果資訊
/// - 作為付款回調處理的輸入參數
/// 
/// 設計模式：
/// - 實作 IRequest，表示這是一個不回傳資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 NewebPayNotifyCommandHandler 配合使用，完成付款回調處理
/// 
/// 使用場景：
/// - 藍新金流 Server 對 Server 通知 (NotifyURL)
/// - 處理付款成功/失敗回調
/// - 更新訂單付款狀態
/// 
/// 資料來源：
/// - 藍新金流平台 POST 到 NotifyURL 的表單資料
/// - 包含 Status (是否送出成功) 與 TradeInfo (加密的付款內容)
/// 
/// 安全性考量：
/// - TradeInfo 經過 AES 加密，需解密後才能使用
/// - 必須驗證 Status 是否為 "SUCCESS"
/// - 必須驗證金額是否正確，防止金額被篡改
/// </summary>
public class NewebPayNotifyCommand : IRequest
{
    /// <summary>
    /// 回應狀態
    /// 
    /// 用途：
    /// - 指示藍新金流請求是否成功送出
    /// - 用於初步驗證請求的有效性
    /// 
    /// 可能值：
    /// - "SUCCESS"：請求成功送出
    /// - 其他值：請求失敗或錯誤
    /// 
    /// 使用範例：
    /// <code>
    /// if (request.Status == "SUCCESS")
    /// {
    ///     // 處理成功情況
    /// }
    /// else
    /// {
    ///     // 處理失敗情況
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 區分大小寫
    /// - 只表示請求是否成功送出，不代表付款是否成功
    /// - 付款成功與否需檢查解密後的 Status 欄位
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 加密的交易資訊
    /// 
    /// 用途：
    /// - 包含付款結果的詳細資訊（經過 AES 加密）
    /// - 需要解密後才能使用
    /// 
    /// 加密方式：
    /// - 使用 AES-CBC 加密模式
    /// - 使用 PKCS7 填充方式
    /// - 金鑰和 IV 從 NewebPaySettings 讀取
    /// 
    /// 解密後內容：
    /// - MerchantOrderNo：訂單編號
    /// - TradeNo：藍新交易序號
    /// - Amt：實際付款金額
    /// - PayTime：付款時間
    /// - PaymentType：付款方式
    /// - Status：付款狀態
    /// 
    /// 使用範例：
    /// <code>
    /// // 解密交易資訊
    /// string decryptedJson = _newebPayService.DecryptAes(request.TradeInfo);
    /// 
    /// // 反序列化成物件
    /// var callbackData = JsonSerializer.Deserialize<NewebPayResponse>(decryptedJson);
    /// </code>
    /// 
    /// 注意事項：
    /// - 必須使用 INewebPayService.DecryptAes() 解密
    /// - 解密後的 JSON 需反序列化成 NewebPayResponse 物件
    /// - 建議將完整回應記錄到日誌系統
    /// </summary>
    public string TradeInfo { get; set; }
}

/// <summary>
/// 藍新金流付款回調命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 NewebPayNotifyCommand 命令
/// - 解密並驗證付款回調資訊
/// - 更新訂單和付款記錄狀態
/// 
/// 設計模式：
/// - 實作 IRequestHandler<NewebPayNotifyCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IOrderRepository 和 INewebPayService
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - NewebPayCreateHandler：類似的藍新金流邏輯
/// - PaymentUpdateHandler：類似的付款更新邏輯
/// </summary>
public class NewebPayNotifyCommandHandler : IRequestHandler<NewebPayNotifyCommand>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 查詢訂單資料
    /// - 查詢付款記錄
    /// - 更新訂單和付款狀態
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetAsync、SaveChangeAsync 等
    /// - 擴展了 GetPaymentAsync 方法專門查詢訂單的付款記錄
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 藍新金流服務介面
    /// 
    /// 用途：
    /// - 解密回傳的交易資訊
    /// - 提供藍新金流平台相關操作
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/NewebPayService.cs
    /// - 使用 AES 加密演算法保護資料傳輸
    /// - 使用 SHA256 演算法生成檢查碼
    /// 
    /// 介面定義：
    /// - 見 Application/Services/INewebPayService.cs
    /// </summary>
    private readonly INewebPayService _newebPayService;

    /// <summary>
    /// 唯一識別代碼生成器
    /// 
    /// 用途：
    /// - 生成唯一識別碼
    /// - 用於生成訂單編號
    /// 
    /// 實作方式：
    /// - 見 Shared/UniqueIdentifier/UniqueIdentifier.cs
    /// - 使用 Guid 生成唯一識別碼
    /// 
    /// 介面定義：
    /// - 見 Shared/UniqueIdentifier/IUniqueIdentifier.cs
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢和更新訂單資料</param>
    /// <param name="newebPayService">藍新金流服務，用於解密交易資訊</param>
    public NewebPayNotifyCommandHandler(
        IOrderRepository orderRepository, 
        INewebPayService newebPayService,
        IUniqueIdentifier uniqueIdentifier)
    {
        _orderRepository = orderRepository;
        _newebPayService = newebPayService;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理藍新金流付款回調命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證 TradeInfo 是否存在
    /// 2. 解密回傳資料
    /// 3. 反序列化成物件
    /// 4. 驗證回應狀態
    /// 5. 提取核心參數
    /// 6. 查詢訂單
    /// 7. 更新訂單狀態
    /// 8. 建立付款記錄
    /// 9. 儲存變更
    /// 
    /// 錯誤處理：
    /// - TradeInfo 為空：拋出 Failure.BadRequest("TradeInfo is required")
    /// - 訂單不存在：拋出 Failure.NotFound("找不到訂單")
    /// - 回應狀態失敗：拋出 Failure.BadRequest("更新付款資訊失敗")
    /// 
    /// 注意事項：
    /// - 建議驗證金額是否正確（TODO）
    /// - 建議將完整回應記錄到日誌系統（TODO）
    /// - 建議使用事務確保資料一致性
    /// </summary>
    /// <param name="request">藍新金流付款回調命令物件，包含 Status 和 TradeInfo</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(NewebPayNotifyCommand request)
    {
        // ========== 第一步：驗證 TradeInfo 是否存在 ==========
        // TradeInfo 是加密的交易資訊，必須存在才能進行後續處理
        if (string.IsNullOrEmpty(request.TradeInfo)) 
            throw Failure.BadRequest("TradeInfo is required");

        // ========== 第二步：解密回傳資料 ==========
        // 使用 INewebPayService.DecryptAes() 解密 TradeInfo
        // 解密後的資料是 JSON 格式
        string decryptedJson = _newebPayService.DecryptAes(request.TradeInfo);
        
        // ========== 第三步：反序列化成物件 ==========
        // 將解密後的 JSON 反序列化成 NewebPayResponse 物件
        // 使用 PropertyNameCaseInsensitive = true 忽略大小寫
        var callbackData = JsonSerializer.Deserialize<NewebPayConfirmedModel>(decryptedJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // ========== 第四步：驗證回應狀態 ==========
        // 檢查回應狀態是否為 "SUCCESS"
        // 只有狀態為 "SUCCESS" 才繼續處理
        if (callbackData?.Status == "SUCCESS")
        {
            // ========== 第五步：提取核心參數 ==========
            // 從回應結果中提取五個核心參數
            var r = callbackData.Result;

            // 訂單編號（系統的訂單編號）
            string myOrderId = r.MerchantOrderNo;
            
            // 藍新交易序號（藍新平台的交易編號）
            string systemTradeNo = r.TradeNo;
            
            // 實際付款金額
            int payAmt = r.Amt;
            
            // 付款時間
            string payTime = r.PayTime;
            
            // 付款方式（CREDIT、WEBATM、VACC、TAIWANPAY 等）
            string payMethod = r.PaymentType;

            // ========== 第六步：處理資料庫更新 ==========
            // TODO: 驗證 Amt 是否正確 -> 更新訂單狀態 -> 存入 JSONB Log
            // 目前只輸出到控制台，實際應該：
            // 1. 驗證付款金額是否與訂單金額一致
            // 2. 更新訂單狀態為 "paid"
            // 3. 將完整回應記錄到日誌系統（可考慮存入資料庫的 JSONB 欄位）
            Console.WriteLine($"訂單 {myOrderId} 透過 {payMethod} 付款成功！金額：{payAmt}");

            // ========== 第七步：查詢訂單 ==========
            // 根據訂單編號查詢訂單
            var order = await _orderRepository.GetAsync(
                q => q.Where(order => order.OrderNumber == myOrderId)
            );

            // 驗證訂單是否存在
            if (order == null) 
                throw Failure.NotFound("找不到訂單");

            // ========== 第八步：更新訂單狀態 ==========
            // 將訂單狀態更新為 "paid"
            order.Status = "paid";

            // ========== 第九步：建立付款記錄 ==========
            // 建立新的付款記錄實體
            var payment = new Payment()
            {
                Id = _uniqueIdentifier.NextInt(),          // 產生全域唯一的付款記錄 ID
                OrderId = order.Id,                        // 關聯到訂單
                Amount = order.TotalAmount,                // 設定付款金額
                Method = payMethod switch
                {
                    "TAIWANPAY" => "taiwan_pay",
                    "VACC"      => "atm_virtual",
                    _           => "credit_card_one_time"
                },                                         // 設定付款方式
                Status = "paid",                           // 設定付款狀態為已付款
                CreatedAt = DateTimeOffset.UtcNow,         // 設定建立時間為目前 UTC 時間
                PaidAt = DateTimeOffset.Parse(payTime),    // 設定付款時間（將字串轉換為 DateTimeOffset）
                TransactionId = systemTradeNo              // 設定藍新交易序號
            };

            // ========== 第十步：新增付款記錄 ==========
            // 使用 IOrderRepository.AddPayment() 新增付款記錄
            _orderRepository.AddPayment(payment);

            // ========== 第十一步：儲存變更 ==========
            // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
            await _orderRepository.SaveChangeAsync();
        }
        else
        {
            // ========== 第十二步：處理失敗情況 ==========
            // 如果回應狀態不是 "SUCCESS"，拋出例外
            throw Failure.BadRequest("更新付款資訊失敗");
        }
    }
}
