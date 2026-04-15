using System.Text.Json;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Memberships;

/// <summary>
/// 积分交易实体类，实现IEntity接口和IDisposable接口
/// </summary>
public class PointTransaction : IEntity, IDisposable
{
    /// <summary>
    /// 交易 ID，唯一标识一笔积分交易
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户 ID，标识积分交易的所属用户
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// 點數變動量 (正數為增加，負數為減少)
    /// 表示积分数量的变化，正数表示增加积分，负数表示减少积分
    /// </summary>
    public int Delta { get; set; }
    
    /// <summary>
    /// 交易原因
    /// 描述积分变动的原因，如"购买商品"、"退款"等
    /// </summary>
    public string Reason { get; set; }
    
    /// <summary>
    /// 參考類型 (例如: 'order', 'refund', 'promotion')
    /// 标识积分交易关联的业务类型，如订单、退款或促销活动
    /// </summary>
    public string RefType { get; set; }
    
    /// <summary>
    /// 參考 ID (例如: 訂單ID)
    /// 关联的业务ID，如订单ID，用于追踪具体的业务来源
    /// </summary>
    public string RefId { get; set; }
    
    /// <summary>
    /// 交易時間
    /// 记录积分交易发生的具体时间
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }
    
    /// <summary>
    /// 額外資訊 (JSON格式)
    /// 存储与交易相关的额外信息，以JSON格式保存
    /// </summary>
    public JsonDocument? Metadata { get; set; }

    /// <summary>
    /// 释放资源的方法
    /// 释放Metadata对象占用的资源，防止内存泄漏
    /// </summary>
    public void Dispose()
    {
        Metadata?.Dispose();
    }
}

