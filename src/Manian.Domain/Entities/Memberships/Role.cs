using System;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Memberships;

public class Role : IEntity
{
    /// <summary>
    /// Unique Identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 角色編碼
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 角色名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }
}
