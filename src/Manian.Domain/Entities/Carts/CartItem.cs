using System.Text.Json;
using Manian.Domain.ValueObjects;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Carts;

/// <summary>
/// 購物車項目實體
/// 
/// 用途：
/// - 記錄使用者的購物車項目
/// - 支援購物車(shopping)和願望清單(wishlist)兩種類型
/// - 快照商品資訊以確保歷史一致性
/// - 支援多使用者裝置（透過 session_id）
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// </summary>
public class CartItem : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 購物車項目唯一識別碼
    /// 主鍵約束：pk_cart_items
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 使用者 ID
    /// 外鍵約束：fk_cart_items_user
    /// 級聯：刪除使用者時自動刪除購物車項目
    /// </summary>
    public int UserId { get; set; }

    // =========================================================================
    // 類型欄位 (Type Field)
    // =========================================================================

    /// <summary>
    /// 購物車類型
    /// 檢查約束：ck_cart_items_type
    /// 可選值：shopping購物車/wishlist願望清單
    /// 預設值：shopping
    /// </summary>
    private string _cartType = "shopping";

    public string CartType
    {
        get => _cartType;
        set
        {
            if (value != "shopping" && value != "wishlist")
                throw new ArgumentException("CartType 必須是 'shopping' 或 'wishlist'");
            _cartType = value;
        }
    }

    // =========================================================================
    // 商品資訊 (Product Information)
    // =========================================================================

    /// <summary>
    /// 商品 ID
    /// 外鍵約束：fk_cart_items_product
    /// 約束：刪除商品時保留購物車項目（僅標記商品不存在）
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// SKU ID
    /// 外鍵約束：fk_cart_items_sku
    /// 約束：刪除 SKU 時保留購物車項目（僅標記 SKU 不存在）
    /// </summary>
    public int SkuId { get; set; }

    // =========================================================================
    // 商品快照資訊 (Product Snapshot)
    // =========================================================================

    /// <summary>
    /// 商品名稱（快照）
    /// 用途：避免商品資訊變更影響歷史購物車
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// SKU 屬性（快照）
    /// 儲存格式：JSONB
    /// 範例：{"顏色":"黑色","尺寸":"XL"}
    /// </summary>
    public List<Specification> SkuAttributes { get; set; }

    /// <summary>
    /// 單價（快照）
    /// 用途：避免價格變更影響歷史購物車
    /// 檢查約束：ck_cart_items_price (unit_price > 0)
    /// </summary>
    private decimal _unitPrice;

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (value <= 0)
                throw new ArgumentException("單價必須大於 0");
            _unitPrice = value;
        }
    }

    /// <summary>
    /// 貨幣代碼
    /// 預設值：NTD
    /// </summary>
    public string Currency { get; set; } = "NTD";

    /// <summary>
    /// 數量
    /// 檢查約束：ck_cart_items_quantity (quantity > 0)
    /// 預設值：1
    /// </summary>
    private int _quantity = 1;

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value <= 0)
                throw new ArgumentException("數量必須大於 0");
            _quantity = value;
        }
    }

    /// <summary>
    /// 商品圖片（快照）
    /// 用途：優先使用 SKU 圖片
    /// </summary>
    public string ProductImage { get; set; } = string.Empty;

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 記錄建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 記錄更新時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
