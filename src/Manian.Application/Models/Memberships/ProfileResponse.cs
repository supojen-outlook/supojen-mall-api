using System;
using Manian.Application.Models.Memberships.Base;

namespace Manian.Application.Models.Memberships;

public class ProfileResponse : UserBase
{
    /// <summary>
    /// 獎勵點數
    /// </summary>
    public int Points { get; set; }
}
