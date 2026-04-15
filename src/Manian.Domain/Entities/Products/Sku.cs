using Manian.Domain.ValueObjects;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 商品 SKU (Stock Keeping Unit) 實體
/// 
/// 用途：
/// - 儲存商品的具體規格組合
/// - 管理每個規格的價格和庫存
/// - 支援多規格商品的庫存管理
/// 
/// 設計考量：
/// - 與 Product 為多對一關係
/// - 使用 JSONB 儲存規格組合
/// - 支援預占庫存機制
/// 
/// 使用場景：
/// - 商品規格選擇（如：顏色、尺寸）
/// - 庫存管理（實際庫存、預占庫存）
/// - 價格差異（不同規格不同價格）
/// </summary>
public class Sku : IEntity
{
    /// <summary>
    /// SKU 唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// SKU 編碼
    /// 用途：用於庫存管理和訂單系統
    /// 約束：必須唯一
    /// </summary>
    public string SkuCode { get; set; }

    /// <summary>
    /// SKU 顯示名稱
    /// 範例：iPhone 14 黑色 128G
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 所屬商品 ID
    /// 關聯：外鍵指向 products.id
    /// 級聯：刪除商品時一併刪除 SKU
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// SKU 銷售價格
    /// 說明：可覆蓋商品基礎價格
    /// 約束：必須 >= 0
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 實際庫存數量
    /// 約束：必須 >= 0
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// 預占庫存（已下單未付款）
    /// 約束：必須 >= 0 且 <= StockQuantity
    /// </summary>
    public int ReservedStock { get; set; }

    /// <summary>
    /// SKU 規格組合
    /// 儲存格式：JSONB
    /// 範例：{"顏色":"黑色","尺寸":"XL"}
    /// 處理方式：與 Product.Specs 相同，使用 List<Specification>
    /// </summary>
    public List<Specification> Specs { get; set; }

    /// <summary>
    /// SKU 專屬圖片 URL
    /// 說明：可為 null，未設定時使用商品圖片
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 計量單位 ID
    /// 關聯：外鍵指向 unit_of_measures.id
    /// </summary>
    public int? UnitOfMeasureId { get; set; }

    /// <summary>
    /// 是否為預設 SKU
    /// 用途：商品頁面預先顯示的 SKU
    /// 約束：每個商品只能有一個預設 SKU
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// SKU 狀態
    /// 可選值：active（啟用）、inactive（停用）
    /// 預設值：active
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// SKU 建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
