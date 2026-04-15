using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 訂單項目實體
/// 
/// 用途：
/// - 記錄訂單中的每個商品明細
/// - 包含商品名稱、價格、數量等資訊
/// - 支援退貨數量追蹤
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 商品名稱和單價為快照，避免商品資訊變更影響歷史訂單
/// 
/// 使用場景：
/// - 訂單建立時新增項目
/// - 訂單出貨處理
/// - 退貨處理
/// </summary>
public class OrderItem : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 訂單項目唯一識別碼
    /// 主鍵約束：pk_order_items
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所屬訂單 ID
    /// 外鍵約束：fk_order_items_order
    /// 級聯：刪除訂單時自動刪除項目
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 商品 ID
    /// 外鍵約束：fk_order_items_product
    /// 約束：有訂單的商品不能被刪除
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// SKU ID
    /// 外鍵約束：fk_order_items_sku
    /// 約束：有訂單的 SKU 不能被刪除
    /// </summary>
    public int SkuId { get; set; }

    // =========================================================================
    // 商品快照資訊 (Product Snapshot Information)
    // =========================================================================

    /// <summary>
    /// 下單時的商品名稱（快照）
    /// 用途：避免商品資訊變更影響歷史訂單
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 下單時的商品圖片（快照）
    /// 用途：避免商品資訊變更影響歷史訂單
    /// </summary>
    public string ProductImageUrl { get; set; }

    /// <summary>
    /// 下單時的單價（快照）
    /// 用途：避免價格變更影響歷史訂單
    /// 檢查約束：ck_order_items_unit_price (unit_price >= 0)
    /// </summary>
    private decimal _unitPrice;

    /// <summary>
    /// 下單時的單價（快照）
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// </summary>
    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (value < 0)
                throw new ArgumentException("單價不能為負數");
            
            _unitPrice = value;
        }
    }

    /// <summary>
    /// 購買數量
    /// 檢查約束：ck_order_items_quantity (quantity > 0)
    /// </summary>
    private int _quantity;

    /// <summary>
    /// 購買數量
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// </summary>
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value <= 0)
                throw new ArgumentException("購買數量必須大於 0");
            
            _quantity = value;
        }
    }

    // =========================================================================
    // 退貨數量追蹤 (Return Tracking)
    // =========================================================================

    /// <summary>
    /// 已退貨數量
    /// 預設值：0
    /// 檢查約束：ck_order_items_returned (returned_quantity <= quantity)
    /// </summary>
    private int _returnedQuantity = 0;

    /// <summary>
    /// 已退貨數量
    /// 
    /// 驗證規則：
    /// - 不能超過購買數量
    /// - 必須大於或等於 0
    /// </summary>
    public int ReturnedQuantity
    {
        get => _returnedQuantity;
        set
        {
            if (value < 0)
                throw new ArgumentException("退貨數量不能為負數");
            
            if (value > _quantity)
                throw new ArgumentException("退貨數量不能超過購買數量");
            
            _returnedQuantity = value;
        }
    }

    // =========================================================================
    // 項目狀態 (Item Status)
    // =========================================================================

    /// <summary>
    /// 項目狀態
    /// 預設值：pending
    /// 狀態流程：pending(待處理) → shipped(已出貨) 
    ///                     ↘ refunded(已退款) / cancelled(已取消)
    /// 檢查約束：ck_order_items_status
    /// </summary>
    private string _status = "pending";

    /// <summary>
    /// 項目狀態：pending待處理/shipped已出貨/refunded已退款/cancelled已取消
    /// 
    /// 驗證規則：
    /// - 只能接受 "pending"、"shipped"、"refunded" 或 "cancelled" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// orderItem.Status = "pending";   // 正確
    /// orderItem.Status = "shipped";   // 正確
    /// orderItem.Status = "refunded";  // 正確
    /// orderItem.Status = "cancelled"; // 正確
    /// orderItem.Status = "delivered"; // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 狀態轉換規則：
    /// - pending → shipped
    /// - pending → cancelled
    /// - shipped → refunded
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "pending"、"shipped"、"refunded" 或 "cancelled" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "pending" && value != "shipped" && 
                value != "refunded" && value != "cancelled")
                throw new ArgumentException("Status 必須是 'pending'、'shipped'、'refunded' 或 'cancelled'");
            
            _status = value;
        }
    }

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 項目建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
