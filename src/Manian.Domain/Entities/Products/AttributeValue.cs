using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 屬性值實體
/// 用途：儲存屬性的具體可選值，如顏色屬性的"紅色"、"藍色"、"黑色"
/// 設計考量：
/// - 與 AttributeKey 為多對一關係，每個屬性鍵可以有多個屬性值
/// - 同一屬性下不能有重複的值 (attribute_id + value 唯一)
/// </summary>
public class AttributeValue : IEntity
{
    /// <summary>
    /// 屬性值唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所屬屬性鍵 ID
    /// </summary>
    public int AttributeId { get; set; }

    /// <summary>
    /// 屬性值內容，如：紅色、XL
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 屬性值詳細描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序順序，數字越小越前面
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 屬性值建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
