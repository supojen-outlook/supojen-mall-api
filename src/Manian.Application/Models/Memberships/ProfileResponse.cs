using Manian.Application.Models.Memberships.Base;
using Manian.Domain.Entities.Memberships;

namespace Manian.Application.Models.Memberships;

public class ProfileResponse : UserBase
{
    /// <summary>
    /// 獎勵點數
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// 擁有的角色
    /// </summary>
    public IEnumerable<Role> Roles { get; set; }
}
