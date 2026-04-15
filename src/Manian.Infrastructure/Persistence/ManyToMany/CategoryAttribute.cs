using Manian.Domain.Entities.Products;

namespace Manian.Infrastructure.Persistence.ManyToMany;

/// <summary>
/// 類別屬性關聯實體
/// 用途：建立產品類別與屬性鍵之間的多對多關聯關係
/// 設計考量：
/// - 定義每個類別下可以使用哪些屬性，用於商品發布時的屬性選擇
/// - 複合主鍵確保同一類別不會重複關聯同一屬性
/// - 使用級聯刪除，當類別或屬性刪除時自動刪除關聯
/// </summary>
public class CategoryAttribute
{
    /// <summary>
    /// 類別 ID
    /// 外鍵關聯到 categories 表
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// 屬性鍵 ID
    /// 外鍵關聯到 attribute_keys 表
    /// </summary>
    public int AttributeKeyId { get; set; }

    /// <summary>
    /// 導航屬性：關聯的屬性鍵實體
    /// 外鍵關聯到 attribute_keys 表
    /// </summary>
    public AttributeKey AttributeKey { get; set; }
}