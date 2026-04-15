namespace Manian.Application.Services;

/// <summary>
/// 電子郵件服務介面
/// 
/// 定義了系統中發送電子郵件所需的基本功能。
/// 此介面實現了依賴注入模式，允許不同的郵件服務實作來替換實現細節。
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 非同步發送電子郵件
    /// 
    /// 使用指定的收件人、主題和 HTML 內容發送電子郵件。
    /// 此方法為非同步實作，不會阻塞呼叫線程，適合在高流量應用中使用。
    /// </summary>
    /// <param name="email">收件人的電子郵件地址。必須是有效的電子郵件格式。</param>
    /// <param name="subject">郵件主題，將顯示在收件人的郵件列表中。</param>
    /// <param name="htmlMessage">郵件的 HTML 格式內容。可以使用 HTML 標籤來格式化郵件內容。</param>
    /// <returns>
    /// 表示非同步操作的工作 (Task)。
    /// 工作完成時表示郵件已成功排隊發送，但不保證郵件一定會成功到達收件人信箱。
    /// </returns>
    /// <exception cref="System.ArgumentNullException">當 email、subject 或 htmlMessage 為 null 時拋出。</exception>
    /// <exception cref="System.ArgumentException">當 email 不是有效的電子郵件格式時拋出。</exception>
    /// <exception cref="System.OperationCanceledException">當操作被取消時拋出。</exception>
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}