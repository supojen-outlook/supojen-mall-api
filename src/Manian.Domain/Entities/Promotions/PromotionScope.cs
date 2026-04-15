using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Promotions;

/// <summary>
/// 促銷適用範圍實體
/// 
/// 用途：
/// - 定義促銷活動適用的商品、類別、品牌
/// - 支援多對多關係，一個活動可適用多個範圍
/// - 支援排除特定範圍的功能
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - scope_id 沒有直接的外鍵約束，需在應用層確保資料一致性
/// 
/// 使用場景：
/// - 全館折扣活動（scope_type = 'all'）
/// - 特定商品優惠（scope_type = 'product'）
/// - 特定類別優惠（scope_type = 'category'）
/// - 特定品牌優惠（scope_type = 'brand'）
/// - 排除特定商品（is_exclude = true）
/// </summary>
public class PromotionScope : IEntity
{
    /// <summary>
    /// 範圍記錄唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所屬促銷活動 ID
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// 範圍類型
    /// 預設值：product
    /// </summary>
    private string _scopeType = "product";

    /// <summary>
    /// 範圍類型：product商品/category類別/brand品牌/all全館
    /// 
    /// 驗證規則：
    /// - 只能接受 "product"、"category"、"brand" 或 "all" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// promotionScope.ScopeType = "product";   // 正確
    /// promotionScope.ScopeType = "category"; // 正確
    /// promotionScope.ScopeType = "brand";    // 正確
    /// promotionScope.ScopeType = "all";      // 正確
    /// promotionScope.ScopeType = "sku";     // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 特殊規則：
    /// - 當 ScopeType = 'all' 時，ScopeId 必須為 0
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "product"、"category"、"brand" 或 "all" 時拋出
    /// </exception>
    public string ScopeType
    {
        get => _scopeType;
        set
        {
            if (value != "product" && value != "category" && value != "brand" && value != "all")
                throw new ArgumentException("ScopeType 必須是 'product'、'category'、'brand' 或 'all'");
            
            _scopeType = value;
        }
    }

    /// <summary>
    /// 範圍 ID
    /// 
    /// 用途：
    /// - 根據 ScopeType 對應到不同表的 ID
    /// - ScopeType = 'product'：商品 ID
    /// - ScopeType = 'category'：類別 ID
    /// - ScopeType = 'brand'：品牌 ID
    /// - ScopeType = 'all'：固定為 0
    /// 
    /// 驗證規則：
    /// - 當 ScopeType = 'all' 時，必須為 0
    /// - 其他情況必須為正整數
    /// </summary>
    private int _scopeId;

    public int ScopeId
    {
        get => _scopeId;
        set
        {
            // 當 ScopeType = 'all' 時，ScopeId 必須為 0
            if (_scopeType == "all" && value != 0)
                throw new ArgumentException("當 ScopeType 為 'all' 時，ScopeId 必須為 0");
            
            // 其他情況必須為正整數
            if (_scopeType != "all" && value <= 0)
                throw new ArgumentException("ScopeId 必須為正整數");
            
            _scopeId = value;
        }
    }

    /// <summary>
    /// 是否排除
    /// 預設值：false
    /// </summary>
    public bool IsExclude { get; set; }

    /// <summary>
    /// 記錄建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
