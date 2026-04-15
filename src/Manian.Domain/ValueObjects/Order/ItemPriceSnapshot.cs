using Manian.Domain.ValueObjects;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 訂單項目價格快照
/// </summary>
public class ItemPriceSnapshot
{
    /// <summary>
    /// 商品名稱（快照）
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// SKU 屬性（快照）
    /// </summary>
    public List<Specification> SkuAttributes { get; set; }

    /// <summary>
    /// 原始單價（快照）
    /// </summary>
    public decimal UnitPrice { get; set; }
    /// <summary>
    /// 數量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 小計金額（折扣後）
    /// </summary>
    public decimal Subtotal => UnitPrice * Quantity;
}
