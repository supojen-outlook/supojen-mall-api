using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using Manian.Application.Services;
using Manian.Infrastructure.Settings;

namespace Manian.Infrastructure.Services;

/// <summary>
/// 郵件服務類別 - 負責處理應用程式的電子郵件發送功能
/// 
/// 設計職責：
/// 1. 封裝 SMTP 通訊協定的複雜性，提供簡潔的發送介面
/// 2. 從設定檔讀取郵件伺服器配置，避免硬編碼
/// 3. 支援 HTML 郵件內容，適用於驗證信、通知信等場景
/// </summary>
public class EmailService : IEmailService
{
    /// <summary>
    /// 郵件設定資訊（從 appsettings.json 注入）
    /// 
    /// 包含以下關鍵設定：
    /// - Server: SMTP 伺服器位址（如 smtp.gmail.com）
    /// - Port: 連接埠（587 為 TLS，465 為 SSL）
    /// - Email: 寄件者電子郵件帳號
    /// - Password: 郵件帳號密碼或應用程式專用密碼
    /// - SenderName: 寄件者顯示名稱（如「XX系統通知」）
    /// </summary>
    private readonly EmailSettings _settings;
    
    /// <summary>
    /// SMTP 用戶端 - .NET 內建的郵件發送元件
    /// 
    /// 這個物件負責：
    /// - 管理與郵件伺服器的連線
    /// - 處理郵件的實際傳輸工作
    /// - 管理連線池和資源回收
    /// </summary>
    private readonly SmtpClient _smtpClient;

    /// <summary>
    /// 建構函式 - 初始化郵件服務
    /// </summary>
    /// <param name="settings">由 DI 容器注入的郵件設定（包裝在 IOptions 中）</param>
    /// <remarks>
    /// IOptions<T> 是 ASP.NET Core 的設定模式，可以：
    /// 1. 自動從 appsettings.json 綁定到 EmailSettings 類別
    /// 2. 支援熱更新（如果用 IOptionsSnapshot）
    /// 3. 將設定與使用邏輯分離
    /// </remarks>
    public EmailService(IOptions<EmailSettings> settings)
    {
        // 從 IOptions 中取出實際的 EmailSettings 物件
        _settings = settings.Value;
        
        // 建立並設定 SMTP 用戶端
        // SmtpClient 實作 IDisposable，但此處由 DI 容器管理生命週期
        _smtpClient = new SmtpClient(_settings.Server)
        {
            // 設定連接埠：
            // 25 - 標準 SMTP（通常未加密）
            // 465 - SMTP over SSL（加密）
            // 587 - SMTP with STARTTLS（加密，最常見）
            Port = _settings.Port,
            
            // 啟用 SSL 加密傳輸
            // 防止郵件內容在網路傳輸過程中被竊聽
            EnableSsl = true,
            
            // 指定傳送方式為網路傳送
            // 其他選項如 SpecifiedPickupDirectory 可將郵件存到磁碟（用於測試）
            DeliveryMethod = SmtpDeliveryMethod.Network,
            
            // 不使用預設憑證
            // 若設為 true，會嘗試使用目前 Windows 使用者的憑證
            UseDefaultCredentials = false,
            
            // 設定認證憑證（帳號密碼）
            // 注意：如果使用 Gmail，通常需要「應用程式密碼」而非 Google 帳號密碼
            Credentials = new System.Net.NetworkCredential(
                _settings.Email,    // 寄件者 Email 帳號
                _settings.Password) // 密碼或應用程式專用密碼
        };
    }

    /// <summary>
    /// 建立郵件訊息物件（私有輔助方法）
    /// 
    /// 封裝 MailMessage 的建立細節，提供統一的郵件格式
    /// </summary>
    /// <param name="from">寄件者 Email 地址</param>
    /// <param name="displayname">寄件者顯示名稱（如「系統管理員」）</param>
    /// <param name="to">收件者 Email 地址</param>
    /// <param name="subject">郵件主旨</param>
    /// <param name="body">郵件內容（可以是 HTML）</param>
    /// <returns>設定完成的 MailMessage 物件</returns>
    private MailMessage MailMessageServer(
        string from, 
        string displayname, 
        string to, 
        string subject, 
        string body)
    {
        // 建立新的郵件訊息物件
        MailMessage mail = new MailMessage();
        
        // 設定寄件者（包含顯示名稱和編碼）
        // 使用 UTF-8 編碼確保中文顯示正常
        mail.From = new MailAddress(from, displayname, Encoding.UTF8);
        
        // 加入收件者（可多次呼叫 Add 加入多個收件者）
        mail.To.Add(to);
        
        // 設定郵件主旨（主題）
        mail.Subject = subject;
        
        // 設定郵件本文內容
        mail.Body = body;
        
        // 設定本文編碼為 UTF-8（支援多國語言）
        mail.BodyEncoding = Encoding.UTF8;
        
        // 設定本文為 HTML 格式
        // true 代表郵件內容可以包含 HTML 標籤（如 <h1>、<a> 等）
        mail.IsBodyHtml = true;
        
        // 加入自訂標頭（提供額外資訊）
        // 這裡標記郵件來自 App Mail，方便追蹤
        mail.Headers.Add("Mail", "App Mail");
        
        // 設定郵件優先權為高
        // 這會影響郵件用戶端的顯示（如 Gmail 會加上驚嘆號）
        mail.Priority = MailPriority.High;
        
        // 設定傳送通知選項
        // OnFailure 表示如果傳送失敗，需要回傳通知
        mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
        
        return mail;
    }

    /// <summary>
    /// 非同步發送電子郵件
    /// 
    /// 這是對外公開的主要方法，實作 IEmailService 介面
    /// </summary>
    /// <param name="email">收件者 Email 地址</param>
    /// <param name="subject">郵件主旨</param>
    /// <param name="htmlMessage">HTML 格式的郵件內容</param>
    /// <returns>代表非同步工作的 Task</returns>
    /// <remarks>
    /// 使用範例：
    /// await _emailService.SendEmailAsync(
    ///     "user@example.com", 
    ///     "帳號驗證通知", 
    ///     "<h1>請點擊以下連結驗證您的帳號</h1>..."
    /// );
    /// </remarks>
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // 1. 建立郵件訊息
        //    使用設定的寄件者 Email、寄件者名稱、收件者、主旨和內容
        var msg = MailMessageServer(
            _settings.Email,      // 寄件者（從設定取得）
            _settings.SenderName, // 寄件者名稱（從設定取得）
            email,                 // 收件者（參數傳入）
            subject,               // 主旨（參數傳入）
            htmlMessage);          // HTML 內容（參數傳入）
        
        // 2. 非同步發送郵件
        //    SendMailAsync 會回傳 Task，讓呼叫端可以 await
        //    如果發送失敗，例外會透過 Task 傳回
        return _smtpClient.SendMailAsync(msg);
        
        // 注意：這裡沒有 using 區塊來釋放 MailMessage
        // 因為 SmtpClient.SendMailAsync 會自動處理 MailMessage 的釋放
    }
}