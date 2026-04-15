using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 品牌實體
/// 用途：定義商品的多層級品牌結構，支援無限層級的品牌分類
/// 設計考量：
/// - 使用 parent_id 建立自我參照的樹狀結構
/// - path_cache 和 level 優化樹狀結構的查詢效能
/// - 由觸發器自動維護層級相關欄位
/// </summary>
public class Brand : IEntity
{
    /// <summary>
    /// 品牌唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 品牌顯示名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// URL 友好名稱，用於 SEO 優化
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// 上層品牌 ID，NULL 表示根品牌
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 所在層級：根品牌為 1，子品牌遞增
    /// 由觸發器自動維護
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 是否為葉節點（沒有子品牌）
    /// 由觸發器自動維護
    /// </summary>
    public bool IsLeaf { get; set; }

    /// <summary>
    /// 從根到目前節點的所有 ID 陣列，如：{1,5,8}
    /// 由觸發器自動維護
    /// </summary>
    public int[] PathCache { get; set; }

    /// <summary>
    /// 從根到目前節點的路徑文字，如：'/精品/時尚/服飾'
    /// 由觸發器自動維護
    /// </summary>
    public string PathText { get; set; }

    /// <summary>
    /// 品牌標誌圖片網址
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// 品牌詳細描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 同層級間的排序順序，數字越小越前面
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 品牌狀態
    /// 預設值：active
    /// 可選值：active（啟用）、inactive（停用）
    /// </summary>
    private string _status = "active";

    /// <summary>
    /// 品牌狀態：active啟用，inactive停用
    /// 
    /// 驗證規則：
    /// - 只能接受 "active" 或 "inactive" 兩個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// brand.Status = "active";   // 正確
    /// brand.Status = "inactive"; // 正確
    /// brand.Status = "pending";  // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "active" 或 "inactive" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "active" && value != "inactive")
                throw new ArgumentException("Status 必須是 'active' 或 'inactive'");
            
            _status = value;
        }
    }

    /// <summary>
    /// 品牌建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
