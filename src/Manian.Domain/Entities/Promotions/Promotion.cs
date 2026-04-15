using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Promotions;

/// <summary>
/// 促銷活動實體
/// 
/// 用途：
/// - 定義促銷活動的基本資訊、適用對象、使用限制
/// - 一個活動可以有多條規則 (promotion_rules)、多個適用範圍 (promotion_scopes)
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// 
/// 使用場景：
/// - 全館折扣活動（如雙11全館88折）
/// - 滿額優惠活動（如滿千送百）
/// - 會員專屬活動（如VIP免運）
/// </summary>
public class Promotion : IEntity
{
    /// <summary>
    /// 促銷活動唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 促銷活動名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 促銷活動描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 促銷開始時間
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// 促銷結束時間
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// 適用通路
    /// 預設值：all
    /// </summary>
    private string _channel = "all";

    /// <summary>
    /// 適用通路：app行動版/web網頁版/all全部
    /// 
    /// 驗證規則：
    /// - 只能接受 "app"、"web" 或 "all" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// promotion.Channel = "app";   // 正確
    /// promotion.Channel = "web";   // 正確
    /// promotion.Channel = "all";   // 正確
    /// promotion.Channel = "mobile"; // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "app"、"web" 或 "all" 時拋出
    /// </exception>
    public string Channel
    {
        get => _channel;
        set
        {
            if (value != "app" && value != "web" && value != "all")
                throw new ArgumentException("Channel 必須是 'app'、'web' 或 'all'");
            
            _channel = value;
        }
    }

    /// <summary>
    /// 適用會員
    /// 預設值：all
    /// </summary>
    private string _userScope = "all";

    /// <summary>
    /// 適用會員：all全部/bronze青銅/silver白銀/gold黃金/vip尊榮
    /// 
    /// 驗證規則：
    /// - 只能接受 "all"、"bronze"、"silver"、"gold" 或 "vip" 五個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// promotion.UserScope = "all";     // 正確
    /// promotion.UserScope = "vip";     // 正確
    /// promotion.UserScope = "premium"; // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "all"、"bronze"、"silver"、"gold" 或 "vip" 時拋出
    /// </exception>
    public string UserScope
    {
        get => _userScope;
        set
        {
            if (value != "all" && value != "bronze" && value != "silver" && 
                value != "gold" && value != "vip")
                throw new ArgumentException("UserScope 必須是 'all'、'bronze'、'silver'、'gold' 或 'vip'");
            
            _userScope = value;
        }
    }

    /// <summary>
    /// 每人可使用次數，NULL 表示不限制
    /// </summary>
    public int? LimitPerUser { get; set; }

    /// <summary>
    /// 總可使用次數，NULL 表示不限制
    /// </summary>
    public int? LimitTotal { get; set; }

    /// <summary>
    /// 目前已使用次數（方便快速檢查）
    /// </summary>
    public int UsedCount { get; set; }

    /// <summary>
    /// 促銷活動狀態
    /// 預設值：active
    /// </summary>
    private string _status = "active";

    /// <summary>
    /// 促銷活動狀態：draft草稿/active啟用/expired過期/disabled停用
    /// 
    /// 驗證規則：
    /// - 只能接受 "draft"、"active"、"expired" 或 "disabled" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// promotion.Status = "active";   // 正確
    /// promotion.Status = "draft";   // 正確
    /// promotion.Status = "pending";  // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "draft"、"active"、"expired" 或 "disabled" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "draft" && value != "active" && value != "expired" && value != "disabled")
                throw new ArgumentException("Status 必須是 'draft'、'active'、'expired' 或 'disabled'");
            
            _status = value;
        }
    }

    /// <summary>
    /// 記錄建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}