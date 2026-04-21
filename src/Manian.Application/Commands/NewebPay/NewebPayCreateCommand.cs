using Manian.Application.Models.NewebPay;
using Manian.Application.Services;
using Manian.Domain.Repositories.Orders;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.NewebPay;

/// <summary>
/// 藍新金流付款建立命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝建立藍新金流付款請求所需的資訊
/// - 作為付款建立處理的輸入參數
/// 
/// 設計模式：
/// - 實作 IRequest<NewebPayRequestModel>，表示這是一個會回傳付款請求模型的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 NewebPayCreateHandler 配合使用，完成付款請求建立
/// 
/// 使用場景：
/// - 使用者選擇藍新金流付款
/// - 系統生成付款頁面
/// - API 端點接收付款建立請求
/// 
/// 資料來源：
/// - 前端傳送的訂單 ID 和網域
/// - 用於建立付款請求並導向藍新金流付款頁面
/// 
/// 安全性考量：
/// - OrderId 必須對應資料庫中存在的訂單
/// - Domain 用於生成回調 URL，確保正確性
/// - 付款金額從訂單資料庫讀取，防止篡改
/// </summary>
public class NewebPayCreateCommand : IRequest<NewebPayRequestModel>
{
    /// <summary>
    /// 訂單 ID
    /// 
    /// 用途：
    /// - 識別要建立付款的訂單
    /// - 用於查詢訂單資訊和金額
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new NewebPayCreateCommand 
    /// { 
    ///     OrderId = 1001,
    ///     Domain = "https://example.com"
    /// };
    /// var result = await _mediator.SendAsync(command);
    /// </code>
    /// 
    /// 注意事項：
    /// - 訂單必須存在且未付款
    /// - 付款金額從訂單資料庫讀取，不允許前端指定
    /// - 建議檢查訂單狀態是否允許付款
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 網域名稱
    /// 
    /// 用途：
    /// - 用於生成回調 URL (NotifyURL 和 ReturnURL)
    /// - 確保藍新金流能正確回調到系統
    /// 
    /// 格式要求：
    /// - 必須包含協議 (http:// 或 https://)
    /// - 不應包含路徑或查詢參數
    /// 
    /// 使用範例：
    /// <code>
    /// // 生產環境
    /// Domain = "https://example.com"
    /// 
    /// // 測試環境
    /// Domain = "https://test.example.com"
    /// 
    /// // 本地開發
    /// Domain = "https://localhost:5001"
    /// </code>
    /// 
    /// 注意事項：
    /// - 必須是可從外部訪問的網域
    /// - 本地開發需要使用 ngrok 或類似工具
    /// - 建議從設定檔讀取，不硬編碼
    /// 
    /// 生成的 URL：
    /// - NotifyURL: {Domain}/api/payment/notify
    /// - ReturnURL: {Domain}/api/payment/result
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// 聯絡信箱
    /// 
    /// 用途：
    /// - 接收付款通知的電子郵件地址
    /// - 藍新金流會將付款結果發送到此信箱
    /// 
    /// 驗證規則：
    /// - 必須符合 Email 格式規範
    /// - 建議從訂單讀取，而非前端指定
    /// 
    /// 使用範例：
    /// <code>
    /// Email = "user@example.com"
    /// </code>
    /// 
    /// 注意事項：
    /// - 建議從訂單資料庫讀取，不允許前端指定
    /// - 確保信箱格式正確，避免通知發送失敗
    /// - 建議在訂單建立時就記錄用戶信箱
    /// </summary>
    public string Email { get; set; }
}

/// <summary>
/// 藍新金流付款建立命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 NewebPayCreateCommand 命令
/// - 查詢訂單資訊
/// - 準備藍新金流付款參數
/// - 加密付款資訊
/// - 生成付款請求模型
/// 
/// 設計模式：
/// - 實作 IRequestHandler<NewebPayCreateCommand, NewebPayRequestModel> 介面
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
/// - NewebPayNotifyCommandHandler：處理付款回調
/// - PaymentUpdateCommandHandler：更新付款狀態
/// </summary>
public class NewebPayCreateHandler : IRequestHandler<NewebPayCreateCommand, NewebPayRequestModel>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 查詢訂單資料
    /// - 取得訂單編號和金額
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 查詢實體
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 藍新金流服務介面
    /// 
    /// 用途：
    /// - 加密付款參數
    /// - 生成檢查碼
    /// - 提供商店代號
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
    /// 使用者權限介面
    /// 
    /// 用途：
    /// - 取得當前登入使用者的 ID
    /// - 驗證訂單是否屬於當前使用者
    /// 
    /// 實作方式：
    /// - 見 Application/Services/IUserClaim.cs
    /// - 從 HTTP Context 中取得使用者資訊
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢訂單資料</param>
    /// <param name="newebPayService">藍新金流服務，用於加密和生成檢查碼</param>
    /// <param name="userClaim">使用者權限，用於驗證訂單所有權</param>
    public NewebPayCreateHandler(
        IOrderRepository orderRepository, 
        INewebPayService newebPayService,
        IUserClaim userClaim)
    {
        _orderRepository = orderRepository;
        _newebPayService = newebPayService;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理藍新金流付款建立命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 查詢訂單資訊
    /// 2. 驗證訂單存在性和所有權
    /// 3. 準備付款參數
    /// 4. 加密付款資訊
    /// 5. 生成檢查碼
    /// 6. 回傳付款請求模型
    /// 
    /// 錯誤處理：
    /// - 訂單不存在：拋出 Failure.BadRequest("找不到訂單")
    /// - 訂單非使用者所有：拋出 Failure.BadRequest("訂單非用戶的訂單")
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new NewebPayCreateCommand 
    /// { 
    ///     OrderId = 1001,
    ///     Domain = "https://example.com"
    /// };
    /// var result = await _mediator.SendAsync(command);
    /// 
    /// // 使用結果導向藍新金流付款頁面
    /// var form = new FormCollection
    /// {
    ///     { "MerchantID", result.MerchantID },
    ///     { "TradeInfo", result.TradeInfo },
    ///     { "TradeSha", result.TradeSha }
    /// };
    /// await form.PostAsync("https://ccore.newebpay.com/mpg/mpg_gateway");
    /// </code>
    /// 
    /// 注意事項：
    /// - 付款金額從訂單資料庫讀取，不允許前端指定
    /// - 建議檢查訂單狀態是否允許付款
    /// - 建議將完整請求記錄到日誌系統
    /// </summary>
    /// <param name="request">藍新金流付款建立命令物件，包含 OrderId、Domain 和 Email</param>
    /// <returns>藍新金流付款請求模型，包含 MerchantID、TradeInfo 和 TradeSha</returns>
    public async Task<NewebPayRequestModel> HandleAsync(NewebPayCreateCommand request)
    {
        // ========== 第一步：查詢訂單資訊 ==========
        // 使用 IOrderRepository.GetByIdAsync() 查詢訂單
        // 這個方法會從資料庫中取得完整的訂單實體
        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        
        // ========== 第二步：驗證訂單是否存在 ==========
        // 如果找不到訂單，拋出 400 錯誤
        // 這種情況可能發生在：
        // - 訂單 ID 不存在
        // - 訂單已被刪除（軟刪除）
        if (order == null)
            throw Failure.BadRequest("找不到訂單");

        // ========== 第三步：驗證訂單所有權 ==========
        // 檢查訂單是否屬於當前登入使用者
        // 這是為了防止使用者嘗試為他人的訂單付款
        if(order.UserId != _userClaim.Id)
            throw Failure.BadRequest("訂單非用戶的訂單");

        // ========== 第四步：準備付款參數 ==========
        // 建立藍新金流所需的付款參數字典
        // 這些參數會被加密後傳送給藍新金流
        var parameters = new Dictionary<string, string>
        {
            // 商店代號：從 INewebPayService 讀取
            { "MerchantID",  _newebPayService.MerchantID },

            // 強制開啟 ATM
            { "VACC", "1" },     
            
            // 強制開啟信用卡
            { "CREDIT", "1" },
            
            // 回應格式：固定為 JSON
            { "RespondType", "String" },
            
            // 時間戳記：目前時間的 Unix 時間戳（秒）
            { "TimeStamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
            
            // 版本：固定為 2.0
            { "Version", "2.0" },
            
            // 商店訂單編號：使用系統的訂單編號
            { "MerchantOrderNo", order.OrderNumber },
            
            // 付款金額：從訂單讀取，確保金額正確
            { "Amt", ((int)order.TotalAmount).ToString() },
            
            // 商品資訊：訂單編號和日期
            { "ItemDesc", $"{order.OrderNumber} - {DateTimeOffset.Now:yyyy/MM/dd}" },
            
            // 聯絡信箱：從命令中讀取
            { "Email", request.Email },
            
            // 登入型別：0 表示不登入
            { "LoginType", "0" },
            
            // 付款通知 URL：藍新 Server 對 Server 通知
            { "NotifyURL", $"{request.Domain}/api/payment/notify" },
            
            // 付款返回 URL：使用者付款後轉跳回來的地方
            { "ReturnURL", $"{request.Domain}/api/payment/return" },

            // 關鍵新增項目：非同步付款取號通知 (ATM/超商取號必備)
            // 當使用者在藍新頁面看到 ATM 帳號時，藍新會自動打這支 API
            { "CustomerURL", $"{request.Domain}/api/payment/customer" }
        };

        // ========== 第五步：生成查詢字串 ==========
        // 將參數字典轉換為查詢字串格式
        // 格式：key1=value1&key2=value2&key3=value3
        var queryString = string.Join("&", parameters.Select(x => $"{x.Key}={x.Value}"));
            
        // ========== 第六步：加密付款資訊 ==========
        // 使用 INewebPayService.EncryptAes() 加密查詢字串
        // 這會使用 AES-CBC 加密模式保護資料傳輸
        var tradeInfo = _newebPayService.EncryptAes(queryString);
        
        // ========== 第七步：生成檢查碼 ==========
        // 使用 INewebPayService.GetSha256() 生成檢查碼
        // 這會使用 SHA256 演算法確保資料完整性
        var tradeSha = _newebPayService.GetSha256(tradeInfo);

        // ========== 第八步：回傳付款請求模型 ==========
        // 建立並回傳 NewebPayRequestModel 物件
        // 包含藍新金流所需的 MerchantID、TradeInfo 和 TradeSha
        return new NewebPayRequestModel
        {
            MerchantID =  _newebPayService.MerchantID,
            TradeInfo = tradeInfo,
            TradeSha = tradeSha
        };
    }
}
