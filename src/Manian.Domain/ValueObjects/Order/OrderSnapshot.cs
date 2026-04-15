using Manian.Domain.Entities.Orders;

namespace Manian.Domain.ValueObjects.Order;

/// <summary>
/// 訂單快照（值物件）
/// 
/// 職責：
/// - 記錄訂單建立時的商品價格和優惠方案
/// - 避免商品價格或優惠規則變更影響歷史訂單
/// - 提供完整的訂單歷史追蹤
/// 
/// 設計原則：
/// - 作為值物件 (Value Object)，強調不可變性
/// - 與實體 (Entity) 分離，專注於資料傳遞
/// - 使用 POCO 類別而非 JsonDocument，提高型別安全性
/// </summary>
public record OrderSnapshot
{
    // =========================================================================
    // 商品價格快照 (Product Price Snapshot)
    // =========================================================================

    /// <summary>
    /// 商品價格快照集合
    /// Key: OrderItem.Id
    /// Value: 該項目當時的價格資訊
    /// </summary>
    public Dictionary<int, ItemPriceSnapshot> ItemPrices { get; set; }

    // =========================================================================
    // 優惠方案快照 (Promotion Snapshot)
    // =========================================================================

    /// <summary>
    /// 使用的促銷規則快照集合
    /// </summary>
    public List<PromotionRuleSnapshot> PromotionRules { get; set; }

    /// <summary>
    /// 使用的優惠券快照
    /// </summary>
    public CouponSnapshot? Coupon { get; set; }

    // =========================================================================
    // 計算結果快照 (Calculation Result Snapshot)
    // =========================================================================

    /// <summary>
    /// 運費
    /// </summary>
    public decimal ShippingFee { get; set; }

    /// <summary>
    /// 稅金總額
    /// </summary>
    public decimal TotalTaxAmount { get; set; }
}
