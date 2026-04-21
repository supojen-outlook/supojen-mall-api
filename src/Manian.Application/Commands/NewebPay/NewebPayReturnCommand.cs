using System.Text.Json;
using Manian.Application.Models.NewebPay;
using Manian.Application.Services;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.NewebPay;

public class NewebPayReturnCommand : IRequest
{
    public string MerchatID { get; set; }

    public string TradeInfo { get; set; }

    public string TradeSha { get; set; }

    public string Version { get; set; }
}


public class NewebPayReturnHandler : IRequestHandler<NewebPayReturnCommand>
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


    public NewebPayReturnHandler(INewebPayService newebPayService, IOrderRepository orderRepository, IUniqueIdentifier uniqueIdentifier)
    {
        _newebPayService = newebPayService;
        _orderRepository = orderRepository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    public async Task HandleAsync(NewebPayReturnCommand request)
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