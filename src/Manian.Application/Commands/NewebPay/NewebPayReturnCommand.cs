using System;
using System.Text.Json;
using Manian.Application.Models.NewebPay;
using Manian.Application.Services;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.NewebPay;

public class NewebPayReturnCommand : IRequest<Order?> 
{
    /// <summary>
    /// 藍新回傳狀態碼 (例如: SUCCESS)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 藍新商店編號
    /// </summary>
    public string? MerchantID { get; set; }

    /// <summary>
    /// 藍新回傳的加密資料 (AES)
    /// </summary>
    public string? TradeInfo { get; set; }

    /// <summary>
    /// 藍新回傳的檢查碼 (SHA256)
    /// </summary>
    public string? TradeSha { get; set; }

    /// <summary>
    /// 藍新 API 版本
    /// </summary>
    public string? Version { get; set; }
}


public class NewebPayReturnCommandHandler : IRequestHandler<NewebPayReturnCommand, Order?>
{
    private readonly IOrderRepository _orderRepository;
    private readonly INewebPayService _newebPayService;
    private readonly IUniqueIdentifier _uniqueIdentifier;

    public NewebPayReturnCommandHandler(
        IOrderRepository orderRepository, 
        INewebPayService newebPayService,
        IUniqueIdentifier uniqueIdentifier)
    {
        _orderRepository = orderRepository;
        _newebPayService = newebPayService;
        _uniqueIdentifier = uniqueIdentifier;
    }

    public async Task<Order?> HandleAsync(NewebPayReturnCommand request)
    {
        // ========== 第一步：驗證 TradeInfo 是否存在 (保持不變) ==========
        if (string.IsNullOrEmpty(request.TradeInfo)) 
            throw Failure.BadRequest("TradeInfo is required");

        // ========== 第二步：驗證 TradeSha 是否存在 (保持不變) ==========
        if (string.IsNullOrEmpty(request.TradeSha))
            throw Failure.BadRequest("TradeSha is required");

        // ========== 第三步：驗證 TradeSha 是否正確 (保持不變) ==========
        if(!_newebPayService.ValidateSha256(request.TradeInfo, request.TradeSha))
            throw Failure.BadRequest("TradeSha is invalid");

        // ========== 第四步：解密回傳資料 (保持不變) ==========
        string decryptedRaw = _newebPayService.DecryptAes(request.TradeInfo);
        
        // ========== 關鍵修正：清理 JSON 字串 (保持不變) ==========
        int lastBraceIndex = decryptedRaw.LastIndexOf('}');
        if (lastBraceIndex == -1)
        {
             throw Failure.BadRequest("解密後的資料格式錯誤，找不到 JSON 結尾");
        }
        string decryptedJson = decryptedRaw.Substring(0, lastBraceIndex + 1);
        
        // ========== 第五步：反序列化成物件 (保持不變) ==========
        var callbackData = JsonSerializer.Deserialize<NewebPayConfirmedModel>(decryptedJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if(callbackData == null) throw Failure.BadRequest("解密後的資料格式錯誤，無法反序列化");

        // ============================================================
        // 核心邏輯開始：處理訂單與付款記錄
        // ============================================================
        var r = callbackData.Result;
        
        // 1. 查詢訂單
        var order = await _orderRepository.GetAsync(
            q => q.Where(o => o.OrderNumber == r.MerchantOrderNo)
        );
        if (order == null) return order;

        // 2. 如果訂單還不是 paid (代表這是取號成功，或是 Notify 還沒到)
        if (order.Status == "pending")
        {
            var payment = new Payment()
            {
                Id = _uniqueIdentifier.NextInt(),
                OrderId = order.Id,
                Amount = r.Amt,
                TransactionId = r.TradeNo,
                Status = "pending",
                Method = r.PaymentType switch
                {
                    "TAIWANPAY" => "taiwan_pay",
                    "VACC"      => "atm_virtual",
                    "CREDIT"    => "credit_card_one_time",
                    _           => "cash"
                },
                CreatedAt = DateTimeOffset.UtcNow,
                // 這裡可以把藍新的 BankCode 或 CodeNo 存入 Payment 的備註或擴展欄位
                // 例如：payment.ExternalCode = r.CodeNo; 
            };

            _orderRepository.AddPayment(payment);
            await _orderRepository.SaveChangeAsync();
        }


        return order;
    }
}