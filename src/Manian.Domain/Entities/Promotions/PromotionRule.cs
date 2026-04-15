using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Promotions;

/// <summary>
/// 促銷規則實體
/// 
/// 用途：
/// - 定義促銷活動的具體規則（滿額折扣、贈品等）
/// - 支援多種規則類型（滿額減、折扣、贈品、免運）
/// - 根據規則類型不同，只會有部分欄位有值
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 規則類型專屬欄位會根據 RuleType 進行驗證
/// 
/// 使用場景：
/// - 滿額減活動（如滿千送百）
/// - 折扣活動（如全館88折）
/// - 贈品活動（如買一送一）
/// - 免運活動（如滿額免運）
/// </summary>
public class PromotionRule : IEntity
{
    /// <summary>
    /// 規則唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所屬促銷活動 ID
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// 規則名稱/標籤
    /// 範例：「滿千送百」、「雙11折扣」
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// 規則類型
    /// 預設值：discount
    /// </summary>
    private string _ruleType = "discount";

    /// <summary>
    /// 規則類型：full_reduction滿額減/discount折扣/gift贈品/free_shipping免運
    /// 
    /// 驗證規則：
    /// - 只能接受 "full_reduction"、"discount"、"gift" 或 "free_shipping" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// promotionRule.RuleType = "full_reduction"; // 正確
    /// promotionRule.RuleType = "discount";       // 正確
    /// promotionRule.RuleType = "gift";          // 正確
    /// promotionRule.RuleType = "free_shipping"; // 正確
    /// promotionRule.RuleType = "coupon";        // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 特殊規則：
    /// - 當 RuleType = 'full_reduction' 時，DiscountAmount 必須有值
    /// - 當 RuleType = 'discount' 時，DiscountRate 必須有值
    /// - 當 RuleType = 'gift' 時，GiftItemId 必須有值
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "full_reduction"、"discount"、"gift" 或 "free_shipping" 時拋出
    /// </exception>
    public string RuleType
    {
        get => _ruleType;
        set
        {
            if (value != "full_reduction" && value != "discount" && 
                value != "gift" && value != "free_shipping")
                throw new ArgumentException("RuleType 必須是 'full_reduction'、'discount'、'gift' 或 'free_shipping'");
            
            _ruleType = value;
        }
    }

    /// <summary>
    /// 滿額門檻
    /// 範例：1000 表示滿 1000 元
    /// </summary>
    public decimal? ThresholdAmount { get; set; }

    /// <summary>
    /// 滿件門檻
    /// 範例：2 表示買 2 件
    /// </summary>
    public int? ThresholdQuantity { get; set; }

    /// <summary>
    /// 折抵金額（滿減規則專用）
    /// </summary>
    private decimal? _discountAmount;

    /// <summary>
    /// 折抵金額（滿減規則專用）
    /// 
    /// 驗證規則：
    /// - 當 RuleType = 'full_reduction' 時，必須有值且大於等於 0
    /// - 其他情況可以為 null
    /// </summary>
    public decimal? DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (_ruleType == "full_reduction" && (value == null || value < 0))
                throw new ArgumentException("滿減規則必須設定折抵金額且大於等於 0");
            
            _discountAmount = value;
        }
    }

    /// <summary>
    /// 折扣率（折扣規則專用）
    /// 範例：20.00 表示 20% off
    /// </summary>
    private decimal? _discountRate;

    /// <summary>
    /// 折扣率（折扣規則專用）
    /// 
    /// 驗證規則：
    /// - 當 RuleType = 'discount' 時，必須有值且在 0-100 之間
    /// - 其他情況可以為 null
    /// </summary>
    public decimal? DiscountRate
    {
        get => _discountRate;
        set
        {
            if (_ruleType == "discount" && (value == null || value < 0 || value > 100))
                throw new ArgumentException("折扣規則必須設定折扣率且在 0-100 之間");
            
            _discountRate = value;
        }
    }

    /// <summary>
    /// 最高折抵金額（防止折扣無上限）
    /// </summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>
    /// 贈品商品 ID
    /// </summary>
    private int? _giftItemId;

    /// <summary>
    /// 贈品商品 ID
    /// 
    /// 驗證規則：
    /// - 當 RuleType = 'gift' 時，必須有值且大於 0
    /// - 其他情況可以為 null
    /// </summary>
    public int? GiftItemId
    {
        get => _giftItemId;
        set
        {
            if (_ruleType == "gift" && (value == null || value <= 0))
                throw new ArgumentException("贈品規則必須設定贈品商品 ID 且大於 0");
            
            _giftItemId = value;
        }
    }

    /// <summary>
    /// 贈品數量
    /// 預設值：1
    /// </summary>
    private int _giftQuantity = 1;

    /// <summary>
    /// 贈品數量
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// </summary>
    public int GiftQuantity
    {
        get => _giftQuantity;
        set
        {
            if (value <= 0)
                throw new ArgumentException("贈品數量必須大於 0");
            
            _giftQuantity = value;
        }
    }

    /// <summary>
    /// 記錄建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
