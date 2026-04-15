using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 商品屬性關聯實體
/// 
/// 用途：
/// - 建立商品與屬性值之間的多對多關聯關係
/// - 定義商品擁有哪些非銷售屬性（如材質、產地、風格等）
/// 
/// 設計考量：
/// - 純關聯表，只包含外鍵和時間戳
/// - 使用複合主鍵確保同一商品不會重複關聯同一屬性值
/// - 級聯刪除：刪除商品或屬性值時自動刪除關聯
/// 
/// 使用場景：
/// - 商品描述性屬性管理（材質、產地、風格等）
/// - 商品篩選功能
/// - 商品比較功能
/// 
/// 注意事項：
/// - 銷售屬性（用於 SKU 的顏色、尺寸等）應放在 Sku.Specs 欄位中
/// - 這個實體只用於非銷售屬性的關聯
/// </summary>
public class ProductAttribute
{
    /// <summary>
    /// 商品 ID
    /// 外鍵關聯到 products 表
    /// 級聯：刪除商品時自動刪除關聯
    /// </summary>
    public int ProductId { get; set; }

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
