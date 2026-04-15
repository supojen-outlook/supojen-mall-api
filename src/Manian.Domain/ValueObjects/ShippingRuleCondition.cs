namespace Manian.Domain.ValueObjects;

/// <summary>
/// 運費規則條件值物件
/// 
/// 用途：
/// - 定義運費規則的適用條件
/// - 支援按數量和按金額兩種條件類型
/// - 提供類型安全的條件設定
/// 
/// 設計考量：
/// - 使用值物件模式，確保不可變性
/// - 與 Specification 值物件保持一致的設計風格
/// - 支援 JSON 序列化/反序列化
/// </summary>
public abstract record ShippingRuleCondition
{
    /// <summary>
    /// 規則類型
    /// 
    /// 可選值：
    /// - "quantity"：按數量計算
    /// - "amount"：按金額計算
    /// </summary>
    public string RuleType { get; init; }

    /// <summary>
    /// 依據規則類型，提供相應的條件設定
    /// "quantity"：最小數量（可選）
    /// "amount"：最小金額（可選）
    /// </summary>
    public int? MinAmount { get; init; }

    /// <summary>
    /// 依據規則類型，提供相應的條件設定
    /// "quantity"：最大數量（可選）
    /// "amount"：最大金額（可選）
    /// </summary>
    public int? MaxAmount { get; init; }

    /// <summary>
    /// 驗證條件是否有效
    /// </summary>
    public abstract bool IsValid();

    /// <summary>
    /// 檢查指定數量或金額是否符合條件
    /// </summary>
    public abstract bool Matches(int amount);
}

/// <summary>
/// 按數量計算的運費條件
/// </summary>
public record QuantityShippingCondition : ShippingRuleCondition
{
    /// <summary>
    /// 初始化 RuleType 為 "quantity"
    /// </summary>
    public QuantityShippingCondition()
    {
        RuleType = "quantity";
    }

    /// <summary>
    /// 驗證條件是否有效
    /// </summary>
    public override bool IsValid()
    {
        // 至少需要設定一個邊界
        if (MinAmount == null && MaxAmount == null)
            return false;

        // 如果兩個邊界都設定，最小值不能大於最大值
        if (MinAmount != null && MaxAmount != null && MinAmount > MaxAmount)
            return false;

        // 數量不能為負
        if (MinAmount < 0 || MaxAmount < 0)
            return false;

        return true;
    }

    /// <summary>
    /// 檢查指定數量是否符合條件
    /// </summary>
    /// <param name="amount">要檢查的數量</param>
    /// <returns>如果符合條件返回 true，否則返回 false</returns>
    public override bool Matches(int amount)
    {
        // 檢查最小數量
        if (MinAmount != null && amount < MinAmount)
            return false;

        // 檢查最大數量
        if (MaxAmount != null && amount > MaxAmount)
            return false;

        return true;
    }
}

/// <summary>
/// 按金額計算的運費條件
/// </summary>
public record AmountShippingCondition : ShippingRuleCondition
{
    /// <summary>
    /// 初始化 RuleType 為 "amount"
    /// </summary>
    public AmountShippingCondition()
    {
        RuleType = "amount";
    }

    /// <summary>
    /// 驗證條件是否有效
    /// </summary>
    public override bool IsValid()
    {
        // 至少需要設定一個邊界
        if (MinAmount == null && MaxAmount == null)
            return false;

        // 如果兩個邊界都設定，最小值不能大於最大值
        if (MinAmount != null && MaxAmount != null && MinAmount > MaxAmount)
            return false;

        // 金額不能為負
        if (MinAmount < 0 || MaxAmount < 0)
            return false;

        return true;
    }

    /// <summary>
    /// 檢查指定金額是否符合條件
    /// </summary>
    /// <param name="amount">要檢查的金額</param>
    /// <returns>如果符合條件返回 true，否則返回 false</returns>
    public override bool Matches(int amount)
    {
        // 檢查最小金額
        if (MinAmount != null && amount < MinAmount)
            return false;

        // 檢查最大金額
        if (MaxAmount != null && amount > MaxAmount)
            return false;

        return true;
    }
}
