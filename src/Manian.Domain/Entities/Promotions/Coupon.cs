using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Promotions;

/// <summary>
/// 優惠券實體
/// 
/// 用途：
/// - 提供給特定用戶的折扣工具
/// - 可指定適用商品、折扣方式
/// - 獨立於 promotion 系統，單純針對特定用戶或特定商品給予折扣
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 不包含導航屬性（Navigation Property）
/// 
/// 使用場景：
/// - 指定用戶的專屬優惠券
/// - 公開領取的優惠券
/// - 特定商品/類別/品牌的優惠券
/// </summary>
public class Coupon : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 優惠券唯一識別碼
    /// 主鍵約束：pk_coupons
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 優惠券代碼，用戶輸入
    /// 唯一約束：uk_coupons_code
    /// </summary>
    public string CouponCode { get; set; }

    /// <summary>
    /// 優惠券名稱，如：VIP專屬85折
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 優惠券描述
    /// </summary>
    public string? Description { get; set; }

    // =========================================================================
    // 擁有者資訊 (Owner Information)
    // =========================================================================

    /// <summary>
    /// 指定給特定用戶，NULL 表示不指定
    /// 外鍵約束：fk_coupons_user
    /// </summary>
    public int? UserId { get; set; }

    // =========================================================================
    // 折扣內容 (Discount Content)
    // =========================================================================

    /// <summary>
    /// 折扣方式：amount金額
    /// </summary>
    public decimal DiscountAmount { get; set; }

    // =========================================================================
    // 適用範圍 (Applicable Scope)
    // =========================================================================

    /// <summary>
    /// 適用範圍：all全部/product商品/category類別/brand品牌
    /// 預設值：all
    /// 檢查約束：ck_coupons_scope_type
    /// </summary>
    private string _scopeType = "all";

    public string ScopeType
    {
        get => _scopeType;
        set
        {
            if (value != "all" && value != "product" && 
                value != "category" && value != "brand")
                throw new ArgumentException("ScopeType 必須是 'all'、'product'、'category' 或 'brand'");
            _scopeType = value;
        }
    }

    /// <summary>
    /// 根據 scope_type 對應到不同表的 ID
    /// 檢查約束：ck_coupons_scope_id
    /// </summary>
    private int? _scopeId;

    public int? ScopeId
    {
        get => _scopeId;
        set
        {
            // 當 ScopeType = 'all' 時，ScopeId 必須為 NULL
            if (_scopeType == "all" && value != null)
                throw new ArgumentException("當 ScopeType 為 'all' 時，ScopeId 必須為 NULL");
            
            // 當 ScopeType ≠ 'all' 時，ScopeId 必須有值
            if (_scopeType != "all" && value == null)
                throw new ArgumentException("當 ScopeType 不為 'all' 時，ScopeId 必須有值");
            
            _scopeId = value;
        }
    }

    // =========================================================================
    // 使用狀態 (Usage Status)
    // =========================================================================

    /// <summary>
    /// 是否已使用
    /// 預設值：FALSE
    /// 檢查約束：ck_coupons_used
    /// </summary>
    private bool _isUsed = false;

    public bool IsUsed
    {
        get => _isUsed;
        set
        {
            // 已使用的優惠券必須有 used_at 和 order_id
            if (value && (UsedAt == null || OrderId == null))
                throw new ArgumentException("已使用的優惠券必須有 used_at 和 order_id");
            _isUsed = value;
        }
    }

    /// <summary>
    /// 使用時間
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>
    /// 使用的訂單 ID
    /// 外鍵約束：fk_coupons_order
    /// </summary>
    public int? OrderId { get; set; }

    // =========================================================================
    // 有效期 (Validity Period)
    // =========================================================================

    /// <summary>
    /// 有效開始時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset ValidFrom { get; set; }

    /// <summary>
    /// 有效截止時間，NULL 表示永久有效
    /// 檢查約束：ck_coupons_dates
    /// </summary>
    private DateTimeOffset? _validUntil;

    public DateTimeOffset? ValidUntil
    {
        get => _validUntil;
        set
        {
            // 有效期必須合理：valid_until > valid_from
            if (value != null && value <= ValidFrom)
                throw new ArgumentException("有效截止時間必須晚於有效開始時間");
            _validUntil = value;
        }
    }

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 記錄建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
