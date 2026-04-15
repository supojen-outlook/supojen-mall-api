namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 優惠券快照
/// </summary>
public class CouponSnapshot
{
    /// <summary>
    /// 優惠券名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 折扣金額
    /// </summary>
    public decimal DiscountAmount { get; set; }
}
