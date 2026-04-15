using Manian.Domain.ValueObjects;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 運費規則實體
/// 
/// 用途：
/// - 定義訂單運費計算規則
/// - 支援按數量和按金額兩種計算方式
/// - 使用值物件確保條件的類型安全
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 使用值物件封裝條件邏輯
/// </summary>
public class ShippingRule : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 規則唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 規則名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 規則描述
    /// </summary>
    public string? Description { get; set; }

    // =========================================================================
    // 規則條件 (Rule Condition)
    // =========================================================================

    /// <summary>
    /// 運費規則條件
    /// 使用值物件確保類型安全
    /// </summary>
    public ShippingRuleCondition? Condition { get; set; }

    // =========================================================================
    // 運費金額 (Shipping Fee)
    // =========================================================================

    /// <summary>
    /// 運費金額
    /// </summary>
    private decimal _shippingFee;

    /// <summary>
    /// 運費金額
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// </summary>
    public decimal ShippingFee
    {
        get => _shippingFee;
        set
        {
            if (value < 0)
                throw new ArgumentException("運費金額不能為負數");
            _shippingFee = value;
        }
    }

    // =========================================================================
    // 規則狀態 (Rule Status)
    // =========================================================================

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; }

    // =========================================================================
    // 優先級 (Priority)
    // =========================================================================

    /// <summary>
    /// 優先級
    /// 數字越小優先級越高
    /// </summary>
    public int Priority { get; set; }

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 記錄建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
