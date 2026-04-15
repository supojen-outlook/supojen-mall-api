using Manian.Application.Services;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships.Signs;

/// <summary>
/// 郵箱註冊命令類別 (CQRS 模式中的 Command)
/// 
/// 用途：封裝使用者透過 Email 註冊帳號的第一階段請求
/// 
/// 設計模式：
/// - 實作 IRequest 介面，表示這是一個不回傳資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SignupConfirmedCommand 配合使用，完成兩階段註冊流程
/// 
/// 註冊流程：
/// 1. 第一階段：使用者輸入 Email → 系統發送驗證碼 (SignupCommand)
/// 2. 第二階段：使用者輸入 Email、密碼、驗證碼 → 完成註冊 (SignupConfirmedCommand)
/// 
/// 為什麼要分兩階段？
/// - 驗證 Email 的真實性（防止使用假 Email）
/// - 防止惡意註冊（需要真實 Email 才能收到驗證碼）
/// - 減少垃圾帳號（增加註冊成本）
/// </summary>
public class SignupCommand : IRequest
{
    /// <summary>
    /// 使用者電子郵件地址
    /// 
    /// 用途：
    /// - 作為使用者的登入帳號
    /// - 接收系統通知和驗證碼
    /// - 作為使用者的唯一識別之一
    /// 
    /// 驗證規則：
    /// - 必須符合 Email 格式規範
    /// - 必須是唯一且未註冊過的 Email
    /// - 必須是可接收郵件的真實 Email
    /// 
    /// 使用範例：
    /// - "user@example.com"：標準 Email 格式
    /// - "user.name+tag@example.com"：帶標籤的 Email（視為同一帳號）
    /// </summary>
    public string Email { get; set; }
}

/// <summary>
/// 郵箱註冊命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SignupCommand 命令
/// - 驗證 Email 是否已註冊
/// - 產生並發送驗證碼到 Email
/// - 不會建立使用者帳號（由 SignupConfirmedCommand 負責）
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SignupCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// </summary>
public class SignupHandler : IRequestHandler<SignupCommand>
{
    /// <summary>
    /// 驗證碼服務介面
    /// 
    /// 用途：
    /// - 產生 6 位數字驗證碼
    /// - 將驗證碼儲存在記憶體快取中
    /// - 驗證使用者輸入的驗證碼是否正確
    /// 
    /// 實作方式：
    /// - 使用 IMemoryCache 儲存驗證碼（見 Infrastructure/Services/ValidationCodeService.cs）
    /// - 驗證碼有效期 5 分鐘
    /// - 驗證後自動失效
    /// </summary>
    private readonly IValidationCodeService _validationCodeService;

    /// <summary>
    /// 郵件服務介面
    /// 
    /// 用途：
    /// - 發送 HTML 格式的電子郵件
    /// - 支援非同步發送
    /// - 從設定檔讀取 SMTP 伺服器配置
    /// 
    /// 實作方式：
    /// - 使用 SmtpClient 發送郵件（見 Infrastructure/Services/EmailService.cs）
    /// - 支援 TLS/SSL 加密傳輸
    /// - 可自訂寄件者名稱和 Email
    /// </summary>
    private readonly IEmailService _emailService;

    /// <summary>
    /// 使用者倉儲介面
    /// 
    /// 用途：
    /// - 查詢 Email 是否已註冊
    /// - 存取使用者資料
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/UserRepository.cs）
    /// - 提供資料庫操作的抽象介面
    /// </summary>
    private readonly IUserRepository _userRepository;    

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="validationCodeService">驗證碼服務，用於產生和驗證驗證碼</param>
    /// <param name="emailService">郵件服務，用於發送驗證碼郵件</param>
    /// <param name="userRepository">使用者倉儲，用於查詢 Email 是否已註冊</param>
    public SignupHandler(
        IValidationCodeService validationCodeService, 
        IEmailService emailService, 
        IUserRepository userRepository)
    {
        _validationCodeService = validationCodeService;
        _emailService = emailService;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 處理郵箱註冊命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 檢查 Email 是否已註冊
    /// 2. 產生驗證碼
    /// 3. 建構 HTML 郵件內容
    /// 4. 發送驗證碼郵件
    /// 
    /// 錯誤處理：
    /// - Email 已註冊：拋出 Failure.BadRequest("該郵箱已被註冊")
    /// - 郵件發送失敗：由 EmailService 內部處理
    /// </summary>
    /// <param name="request">郵箱註冊命令物件，包含 Email 資訊</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(SignupCommand request)
    {
        // ========== 第一步：檢查 Email 是否已註冊 ==========
        // 使用 IUserRepository.GetByEmailAsync() 查詢
        // 如果找到使用者，表示 Email 已被註冊，拋出例外
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user != null) 
            throw Failure.BadRequest("該郵箱已被註冊");

        // ========== 第二步：產生驗證碼 ==========
        // 使用 IValidationCodeService.GenerateCode() 產生 6 位數字驗證碼
        // 驗證碼會被儲存在記憶體快取中，有效期 5 分鐘
        var code = _validationCodeService.GenerateCode(request.Email);

        // ========== 第三步：建構 HTML 郵件內容 ==========
        // 使用字串插值建構 HTML 郵件
        // 包含：公司 Logo、歡迎訊息、驗證碼、有效時間、免責聲明
        string html = $@"
        <!DOCTYPE html>
        <html lang=""zh-Hant"">
        <head>
        <meta charset=""UTF-8"">
        <title>小紅帽資訊 - 註冊驗證碼</title>
        <style>
            body {{
            font-family: Arial, ""Microsoft JhengHei"", sans-serif;
            background-color: #f9f9f9;
            margin: 0;
            padding: 0;
            }}
            .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            overflow: hidden;
            }}
            .header {{
            background-color: #ffffff;
            padding: 20px;
            text-align: center;
            }}
            .header img {{
            max-height: 60px;
            border-radius: 50%;
            }}
            .content {{
            padding: 30px;
            color: #333333;
            line-height: 1.6;
            }}
            .code-box {{
            margin: 20px 0;
            padding: 15px;
            text-align: center;
            font-size: 28px;
            font-weight: bold;
            letter-spacing: 4px;
            background-color: #f1f1f1;
            border-radius: 6px;
            color: #B33B15;
            }}
            .footer {{
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #888888;
            background-color: #f5f5f5;
            }}
        </style>
        </head>
        <body>
        <div class=""container"">
            <div class=""header"">
            <img src=""https://demo-po.sgp1.cdn.digitaloceanspaces.com/assets/logo_with_background.png"" alt=""小紅帽資訊 Logo"">
            </div>
            <div class=""content"">
            <h2>歡迎加入小紅帽資訊！</h2>
            <p>您正在使用 <strong>{request.Email}</strong> 註冊帳號。</p>
            <p>請使用以下驗證碼完成註冊流程：</p>
            <div class=""code-box"">{code}</div>
            <p>此驗證碼有效時間為 <strong>5 分鐘</strong>，請盡快完成驗證。</p>
            <p>如果您沒有申請註冊，請忽略此封信件。</p>
            </div>
            <div class=""footer"">
            &copy; 2026 小紅帽資訊. All rights reserved.
            </div>
        </div>
        </body>
        </html>";

        // ========== 第四步：發送驗證碼郵件 ==========
        // 使用 IEmailService.SendEmailAsync() 發送郵件
        // 非同步發送，不會阻塞執行緒
        await _emailService.SendEmailAsync(
            email: request.Email,
            subject: "小紅帽資訊 - 註冊驗證碼",
            htmlMessage: html
        );
    }
}
