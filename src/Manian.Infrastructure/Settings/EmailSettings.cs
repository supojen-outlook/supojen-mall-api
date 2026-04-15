namespace Manian.Infrastructure.Settings;

/// <summary>
/// 邮件设置类，用于存储和配置邮件发送相关的参数
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// 邮件服务器地址
    /// </summary>
    public required string Server { get; set; }

    /// <summary>
    /// 邮件服务器端口号
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// 发件人名称
    /// </summary>
    public required string SenderName { get; set; }

    /// <summary>
    /// 发件人邮箱地址
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 邮箱密码（或应用专用密码）
    /// </summary>
    public required string Password { get; set; }
}