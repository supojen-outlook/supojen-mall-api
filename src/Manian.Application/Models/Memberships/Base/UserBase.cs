using System;

namespace Manian.Application.Models.Memberships.Base;

public class UserBase
{
    /// <summary>
    /// Unique Identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 狀態: active, suspended, deleted
    /// </summary>
    public string Status { get; set; }

        /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// 用戶姓名
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// 用戶生日
    /// </summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>
    /// 用戶性別
    /// </summary>
    public string Gender { get; set; }

    /// <summary>
    /// 用戶頭像
    /// </summary>
    public string Avatar { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 電子郵件認證
    /// </summary>
    public bool EmailVerified { get; set; }
    
    /// <summary>
    /// 會員等級 - bronze/sliver/gold/vip
    /// </summary>
    public string MembershipLevel { get; set; }
    
    /// <summary>
    /// 備註
    /// </summary>
    public string? Note { get; set; }
}
