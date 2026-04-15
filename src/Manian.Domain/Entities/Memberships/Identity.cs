using System;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Memberships;

public class Identity : IEntity
{
    /// <summary>
    /// Unique Identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User Id
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 哪個 Auth 廠商: google, line, micorsoft, facebook
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Auth Unique Identifier
    /// </summary>
    public string ProviderUid { get; set; }
}
