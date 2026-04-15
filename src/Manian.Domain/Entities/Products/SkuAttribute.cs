using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// SKU 屬性關聯實體
/// 
/// 用途：
/// - 建立 SKU 與屬性值之間的多對多關聯關係
/// - 定義 SKU 擁有哪些銷售屬性組合（如顏色、尺寸的具體值）
/// 
/// 設計考量：
/// - 純關聯表，只包含外鍵和時間戳
/// - 使用複合主鍵確保同一 SKU 不會重複關聯同一屬性值
/// - 級聯刪除：刪除 SKU 或屬性值時自動刪除關聯
/// 
/// 使用場景：
/// - SKU 規格組合管理（如：顏色、尺寸）
/// - 庫存管理（按屬性值統計庫存）
/// - 商品篩選功能（如篩選「紅色」的商品）
/// 
/// 注意事項：
/// - 銷售屬性（用於 SKU 的顏色、尺寸等）使用此實體關聯
/// - 非銷售屬性（材質、產地、風格等）應使用 ProductAttribute 關聯
/// - 與 Sku.Specs JSON 欄位互補，此表確保資料正規化
/// </summary>
public class SkuAttribute
{
    /// <summary>
    /// SKU ID
    /// 外鍵關聯到 skus 表
    /// 級聯：刪除 SKU 時自動刪除關聯
    /// </summary>
    public int SkuId { get; set; }

    /// <summary>
    /// 屬性值 ID
    /// 外鍵關聯到 attribute_values 表
    /// 級聯：刪除屬性值時自動刪除關聯
    /// </summary>
    public int AttributeValueId { get; set; }

    /// <summary>
    /// 關聯建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
