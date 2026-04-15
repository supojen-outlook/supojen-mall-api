namespace Manian.Domain.ValueObjects.Order;

/// <summary>
/// 促銷規則快照
/// </summary>
public class PromotionRuleSnapshot
{
    /// <summary>
    /// 規則類型
    /// </summary>
    public string RuleType { get; set; }

    /// <summary>
    /// 規則名稱/標籤
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// 折扣金額
    /// </summary>
    public decimal DiscountAmount { get; set; }
}
