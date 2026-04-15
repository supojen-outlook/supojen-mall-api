using System;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Memberships;

public class PointAccount : IEntity
{
    /// <summary>
    /// 用戶 ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 點數餘額
    /// </summary>
    public int Balance { get; set; }
    
    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Navigation Property - 交易紀錄
    /// </summary>
    public ICollection<PointTransaction> Transactions { get; set; }
}