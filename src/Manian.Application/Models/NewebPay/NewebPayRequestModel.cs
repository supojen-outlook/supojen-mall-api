using System;

namespace Manian.Application.Models.NewebPay;

public class NewebPayRequestModel
{
    public string MerchantID { get; set; }

    public string TradeInfo { get; set; }

    public string TradeSha { get; set; }
}
