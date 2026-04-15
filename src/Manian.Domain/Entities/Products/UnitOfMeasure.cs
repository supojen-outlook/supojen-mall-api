using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 計量單位實體
/// 用途：定義商品的多種計量單位，支援不同包裝規格的換算
/// 設計考量：所有單位以 EA (單件) 為基準單位，conversion_to_base 定義與基準單位的轉換率
/// </summary>
public class UnitOfMeasure : IEntity
{
    /// <summary>
    /// 唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 單位代碼：EA, BOX, CTN, PALLET...
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 單位名稱：Each, Box, Carton...
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 單位描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 轉換率：此單位等於多少基準單位 (EA)
    /// </summary>
    public int ConversionToBase { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
