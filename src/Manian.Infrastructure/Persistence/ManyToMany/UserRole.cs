using System;

namespace Manian.Infrastructure.Persistence.ManyToMany;

public class UserRole
{
    /// <summary>
    /// 用戶 ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 优惠券ID，用于标识具体的优惠券
    /// </summary>
    /// <summary>
    /// 指定時間
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
