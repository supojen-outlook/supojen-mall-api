using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 標籤實體
/// 
/// 用途：
/// - 儲存商品標籤，用於商品分類、行銷活動、搜尋篩選等
/// - 支援靈活的商品標記，不支援層級結構
/// 
/// 設計考量：
/// - 標籤為扁平化結構，不支援層級
/// - 與商品為多對多關係，透過 product_tags 關聯表連接
/// - 純實體設計，不包含驗證邏輯
/// 
/// 使用場景：
/// - 行銷標籤：新品、熱銷、限時優惠、限量
/// - 屬性標籤：手工製作、環保材質、台灣製
/// - 活動標籤：聖誕節、母親節、雙11
/// 
/// 注意事項：
/// - 標籤名稱不可重複（由資料庫唯一約束保證）
/// - 排序值必須大於等於 0（由資料庫檢查約束保證）
/// - 與 ProductAttribute、SkuAttribute 保持一致的設計風格
/// </summary>
public class Tag : IEntity
{
    /// <summary>
    /// 標籤唯一識別碼
    /// 主鍵約束：pk_tags
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 標籤名稱，如：新品、熱銷、限時優惠
    /// 唯一約束：uk_tags_name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 標籤詳細描述
    /// 可為 null
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序順序，數字越小越前面
    /// 預設值：0
    /// 檢查約束：ck_tags_sort_order (sort_order >= 0)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 標籤建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
