namespace Manian.Application.Models.NewebPay;

/// <summary>
/// 藍新金流回應根物件
/// 
/// 職責：
/// - 封裝藍新金流 API 的回應資料
/// - 提供付款狀態和結果資訊
/// - 用於處理付款回調和查詢結果
/// 
/// 使用場景：
/// - 處理付款回調 (NotifyURL)
/// - 處理付款查詢結果
/// - 處理退款回調
/// 
/// 資料來源：
/// - 藍新金流 API 回應
/// - 經過 AES 解密後的 JSON 資料
/// 
/// 實作位置：
/// - src/Manian.Application/Models/NewebPay/NewebPayResult.cs
/// - 由 NewebPayNotifyCommandHandler 使用
/// 
/// 安全考量：
/// - Status 欄位用於驗證請求是否成功
/// - Result 包含敏感資訊，需妥善處理
/// - 建議將完整回應記錄到日誌系統
/// </summary>
public class NewebPayConfirmedModel
{
    /// <summary>
    /// 回應狀態
    /// 
    /// 用途：
    /// - 指示 API 請求是否成功
    /// - 用於錯誤處理和流程控制
    /// 
    /// 可能值：
    /// - "SUCCESS"：請求成功
    /// - "FAILURE"：請求失敗
    /// - 其他錯誤代碼
    /// 
    /// 使用範例：
    /// <code>
    /// if (response.Status == "SUCCESS")
    /// {
    ///     // 處理成功情況
    ///     var result = response.Result;
    /// }
    /// else
    /// {
    ///     // 處理失敗情況
    ///     throw new Exception($"付款失敗：{response.Message}");
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 區分大小寫
    /// - 必須先檢查此欄位再處理 Result
    /// - 失敗時 Result 可能為 null
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 回應訊息
    /// 
    /// 用途：
    /// - 提供請求結果的詳細說明
    /// - 用於錯誤診斷和日誌記錄
    /// 
    /// 可能內容：
    /// - 成功訊息
    /// - 錯誤描述
    /// - 警告訊息
    /// 
    /// 使用範例：
    /// <code>
    /// if (response.Status != "SUCCESS")
    /// {
    ///     // 記錄錯誤訊息
    ///     _logger.LogError($"付款失敗：{response.Message}");
    ///     // 回傳錯誤給使用者
    ///     throw new Exception(response.Message);
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 可能包含敏感資訊，不應直接顯示給使用者
    /// - 建議記錄到日誌系統
    /// - 失敗時此欄位特別重要
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 付款結果內容
    /// 
    /// 用途：
    /// - 包含付款的詳細資訊
    /// - 用於更新訂單和付款記錄
    /// 
    /// 內容說明：
    /// - 包含訂單編號、金額、時間等核心資訊
    /// - 包含付款方式和交易序號
    /// - 當 Status 為 "SUCCESS" 時才有意義
    /// 
    /// 使用範例：
    /// <code>
    /// if (response.Status == "SUCCESS")
    /// {
    ///     var result = response.Result;
    ///     // 更新訂單狀態
    ///     order.Status = "paid";
    ///     // 更新付款記錄
    ///     payment.TransactionId = result.TradeNo;
    ///     payment.PaidAt = DateTimeOffset.Parse(result.PayTime);
    ///     payment.Amount = result.Amt;
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 當 Status 為 "FAILURE" 時可能為 null
    /// - 包含敏感資訊，需妥善處理
    /// - 建議將完整內容記錄到日誌系統
    /// </summary>
    public NewebPayContent Result { get; set; }
}

/// <summary>
/// 藍新金流付款內容物件
/// 
/// 職責：
/// - 封裝付款結果的詳細資訊
/// - 提供訂單和付款的核心資料
/// - 用於更新系統中的訂單和付款記錄
/// 
/// 使用場景：
/// - 更新訂單狀態
/// - 更新付款記錄
/// - 記錄交易歷史
/// - 對帳處理
/// 
/// 資料來源：
/// - 藍新金流 API 回應的 Result 部分
/// - 經過 AES 解密後的 JSON 資料
/// 
/// 實作位置：
/// - src/Manian.Application/Models/NewebPay/NewebPayResult.cs
/// - 由 NewebPayNotifyCommandHandler 使用
/// 
/// 安全考量：
/// - 包含敏感的付款資訊
/// - 需要驗證金額是否正確
/// - 建議將完整內容記錄到日誌系統
/// </summary>
public class NewebPayContent
{
    /// <summary>
    /// 商店代號
    /// 
    /// 用途：
    /// - 識別付款的商店
    /// - 驗證回應是否來自正確的商店
    /// 
    /// 驗證規則：
    /// - 必須與系統中配置的 MerchantID 一致
    /// - 用於防止回應被篡改
    /// 
    /// 使用範例：
    /// <code>
    /// // 驗證商店代號
    /// if (result.MerchantID != _newebPayService.MerchantID)
    /// {
    ///     _logger.LogError($"商店代號不符：{result.MerchantID}");
    ///     throw new Exception("商店代號驗證失敗");
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 應與 appsettings.json 中的設定一致
    /// - 不同環境（開發、測試、生產）有不同的值
    /// - 建議記錄到日誌系統
    /// </summary>
    public string MerchantID { get; set; }

    /// <summary>
    /// 商店訂單編號
    /// 
    /// 用途：
    /// - 識別付款的訂單
    /// - 用於查詢和更新訂單
    /// 
    /// 資料來源：
    /// - 建立付款請求時傳送的 MerchantOrderNo
    /// - 通常使用系統的訂單編號
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單
    /// var order = await _orderRepository.GetAsync(
    ///     q => q.Where(order => order.OrderNumber == result.MerchantOrderNo)
    /// );
    /// 
    /// if (order == null)
    /// {
    ///     _logger.LogError($"找不到訂單：{result.MerchantOrderNo}");
    ///     throw new Exception("找不到訂單");
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 必須對應資料庫中存在的訂單
    /// - 用於關聯藍新金流交易和系統訂單
    /// - 建議記錄到日誌系統
    /// </summary>
    public string MerchantOrderNo { get; set; }

    /// <summary>
    /// 實際付款金額
    /// 
    /// 用途：
    /// - 記錄實際付款的金額
    /// - 驗證付款金額是否正確
    /// - 用於更新付款記錄
    /// 
    /// 驗證規則：
    /// - 必須與訂單金額一致
    /// - 用於防止金額被篡改
    /// 
    /// 使用範例：
    /// <code>
    /// // 驗證付款金額
    /// if (result.Amt != order.TotalAmount)
    /// {
    ///     _logger.LogError($"金額不符：訂單金額={order.TotalAmount}，付款金額={result.Amt}");
    ///     throw new Exception("付款金額驗證失敗");
    /// }
    /// 
    /// // 更新付款記錄
    /// payment.Amount = result.Amt;
    /// </code>
    /// 
    /// 注意事項：
    /// - 單位為元（非分）
    /// - 必須與訂單金額一致
    /// - 建議記錄到日誌系統
    /// </summary>
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public int Amt { get; set; }

    /// <summary>
    /// 藍新金流交易序號
    /// 
    /// 用途：
    /// - 識別藍新金流平台的交易
    /// - 用於查詢和對帳
    /// - 用於退款處理
    /// 
    /// 資料來源：
    /// - 由藍新金流平台生成
    /// - 每筆交易唯一
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新付款記錄
    /// payment.TransactionId = result.TradeNo;
    /// 
    /// // 查詢交易
    /// var transaction = await _newebPayService.QueryTransactionAsync(result.TradeNo);
    /// 
    /// // 退款處理
    /// var refundResult = await _newebPayService.RefundAsync(result.TradeNo, refundAmount);
    /// </code>
    /// 
    /// 注意事項：
    /// - 由藍新金流平台生成，系統無法控制
    /// - 用於關聯藍新金流交易和系統付款記錄
    /// - 建議記錄到日誌系統
    /// </summary>
    public string TradeNo { get; set; }

    /// <summary>
    /// 付款時間
    /// 
    /// 用途：
    /// - 記錄付款完成的時間
    /// - 用於更新付款記錄
    /// - 用於對帳和統計
    /// 
    /// 時間格式：
    /// - 格式：yyyy/MM/dd HH:mm:ss
    /// - 時區：台灣時間 (UTC+8)
    /// 
    /// 使用範例：
    /// <code>
    /// // 更新付款記錄
    /// payment.PaidAt = DateTimeOffset.Parse(result.PayTime);
    /// 
    /// // 記錄付款時間
    /// _logger.LogInformation($"訂單 {result.MerchantOrderNo} 於 {result.PayTime} 付款成功");
    /// </code>
    /// 
    /// 注意事項：
    /// - 格式為字串，需轉換為 DateTimeOffset
    /// - 時區為台灣時間 (UTC+8)
    /// - 建議記錄到日誌系統
    /// </summary>
    public string? PayTime { get; set; }

    /// <summary>
    /// 付款方式
    /// 
    /// 用途：
    /// - 記錄使用的付款方式
    /// - 用於更新付款記錄
    /// - 用於統計和分析
    /// 
    /// 可能值：
    /// - "CREDIT"：信用卡
    /// - "WEBATM"：網路 ATM
    /// - "VACC"：虛擬帳號
    /// - "TAIWANPAY"：台灣 Pay
    /// - 其他付款方式
    /// 
    /// 使用範例：
    /// <code>
    /// // 映射付款方式
    /// payment.Method = result.PaymentType switch
    /// {
    ///     "TAIWANPAY" => "taiwan_pay",
    ///     "VACC" => "atm_virtual",
    ///     _ => "credit_card_one_time"
    /// };
    /// 
    /// // 記錄付款方式
    /// _logger.LogInformation($"訂單 {result.MerchantOrderNo} 使用 {result.PaymentType} 付款");
    /// </code>
    /// 
    /// 注意事項：
    /// - 區分大小寫
    /// - 需要映射到系統的付款方式
    /// - 建議記錄到日誌系統
    /// </summary>
    public string PaymentType { get; set; }

    /// <summary>
    /// 回應類型
    /// 
    /// 用途：
    /// - 指示回應的資料格式
    /// - 用於解析回應資料
    /// 
    /// 可能值：
    /// - "JSON"：JSON 格式
    /// - "String"：字串格式
    /// 
    /// 使用範例：
    /// <code>
    /// // 檢查回應類型
    /// if (result.RespondType != "JSON")
    /// {
    ///     _logger.LogError($"不支援的回應類型：{result.RespondType}");
    ///     throw new Exception("不支援的回應類型");
    /// }
    /// </code>
    /// 
    /// 注意事項：
    /// - 通常為 "JSON"
    /// - 用於驗證回應格式
    /// - 建議記錄到日誌系統
    /// </summary>
    public string RespondType { get; set; }

    public string? BankCode { get; set; }
    public string? CodeNo { get; set; }
    public string? ExpireDate { get; set; } // 到期日
}
