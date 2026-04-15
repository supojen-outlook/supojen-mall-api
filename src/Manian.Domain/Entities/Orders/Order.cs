using System.Text.Json;
using Manian.Domain.ValueObjects.Order;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 訂單實體
/// 
/// 用途：
/// - 記錄訂單交易層級的核心資訊
/// - 包含金額、狀態、時間戳等關鍵資訊
/// - 支援訂單狀態流程管理
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 支援 JSONB 快照功能
/// 
/// 使用場景：
/// - 訂單建立和狀態管理
/// - 訂單金額計算
/// - 訂單歷史追蹤
/// </summary>
public class Order : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 訂單唯一識別碼
    /// 主鍵約束：pk_orders
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 訂單編號，用於查詢和物流
    /// 唯一約束：uk_orders_order_number
    /// </summary>
    public string OrderNumber { get; set; }

    /// <summary>
    /// 顧客 ID，關聯到 users 表
    /// 外鍵約束：fk_orders_user
    /// </summary>
    public int UserId { get; set; }

    // =========================================================================
    // 訂單狀態 (Order Status)
    // =========================================================================

    /// <summary>
    /// 訂單狀態
    /// 預設值：created
    /// 狀態流程：created(已建立) → paid(已付款) → shipped(已出貨) → completed(已完成)
    ///                     ↘ closed(已關閉/取消)
    /// 檢查約束：ck_orders_status
    /// </summary>
    private string _status = "created";

    /// <summary>
    /// 訂單狀態：created已建立/paid已付款/shipped已出貨/completed已完成/closed已關閉
    /// 
    /// 驗證規則：
    /// - 只能接受 "created"、"paid"、"shipped"、"completed" 或 "closed" 五個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// order.Status = "created";    // 正確
    /// order.Status = "paid";       // 正確
    /// order.Status = "shipped";    // 正確
    /// order.Status = "completed";  // 正確
    /// order.Status = "closed";     // 正確
    /// order.Status = "cancelled";  // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 狀態轉換規則：
    /// - created → paid
    /// - paid → shipped
    /// - shipped → completed
    /// - created/paid/shipped → closed
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "created"、"paid"、"shipped"、"completed" 或 "closed" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "created" && value != "paid" && value != "shipped" && 
                value != "completed" && value != "closed")
                throw new ArgumentException("Status 必須是 'created'、'paid'、'shipped'、'completed' 或 'closed'");
            
            _status = value;
        }
    }

    // =========================================================================
    // 金額資訊 (Amount Information)
    // =========================================================================

    /// <summary>
    /// 訂單總金額
    /// 檢查約束：ck_orders_amounts (total_amount >= 0)
    /// </summary>
    private decimal _totalAmount;

    /// <summary>
    /// 訂單總金額
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// </summary>
    public decimal TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (value < 0)
                throw new ArgumentException("訂單總金額不能為負數");
            
            _totalAmount = value;
        }
    }

    /// <summary>
    /// 折扣金額
    /// 預設值：0
    /// 檢查約束：ck_orders_amounts (discount_amount >= 0)
    /// </summary>
    private decimal _discountAmount = 0;

    /// <summary>
    /// 折扣金額
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// </summary>
    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (value < 0)
                throw new ArgumentException("折扣金額不能為負數");
            
            _discountAmount = value;
        }
    }

    /// <summary>
    /// 稅金金額
    /// 預設值：0
    /// 檢查約束：ck_orders_amounts (tax_amount >= 0)
    /// </summary>
    private decimal _taxAmount = 0;

    /// <summary>
    /// 稅金金額
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// </summary>
    public decimal TaxAmount
    {
        get => _taxAmount;
        set
        {
            if (value < 0)
                throw new ArgumentException("稅金金額不能為負數");
            
            _taxAmount = value;
        }
    }

    /// <summary>
    /// 運費金額
    /// 預設值：0
    /// 檢查約束：ck_orders_amounts (shipping_amount >= 0)
    /// </summary>
    private decimal _shippingAmount = 0;

    /// <summary>
    /// 運費金額
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// </summary>
    public decimal ShippingAmount
    {
        get => _shippingAmount;
        set
        {
            if (value < 0)
                throw new ArgumentException("運費金額不能為負數");
            
            _shippingAmount = value;
        }
    }

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 訂單建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 付款時間
    /// 檢查約束：ck_orders_timeline (paid_at >= created_at)
    /// </summary>
    private DateTimeOffset? _paidAt;

    /// <summary>
    /// 付款時間
    /// 
    /// 驗證規則：
    /// - 不能早於訂單建立時間
    /// </summary>
    public DateTimeOffset? PaidAt
    {
        get => _paidAt;
        set
        {
            if (value.HasValue && value.Value < CreatedAt)
                throw new ArgumentException("付款時間不能早於訂單建立時間");
            
            _paidAt = value;
        }
    }

    /// <summary>
    /// 出貨時間
    /// 檢查約束：ck_orders_timeline (shipped_at >= paid_at)
    /// </summary>
    private DateTimeOffset? _shippedAt;

    /// <summary>
    /// 出貨時間
    /// 
    /// 驗證規則：
    /// - 不能早於付款時間
    /// </summary>
    public DateTimeOffset? ShippedAt
    {
        get => _shippedAt;
        set
        {
            if (value.HasValue && _paidAt.HasValue && value.Value < _paidAt.Value)
                throw new ArgumentException("出貨時間不能早於付款時間");
            
            _shippedAt = value;
        }
    }

    /// <summary>
    /// 完成時間
    /// 檢查約束：ck_orders_timeline (completed_at >= shipped_at)
    /// </summary>
    private DateTimeOffset? _completedAt;

    /// <summary>
    /// 完成時間
    /// 
    /// 驗證規則：
    /// - 不能早於出貨時間
    /// </summary>
    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (value.HasValue && _shippedAt.HasValue && value.Value < _shippedAt.Value)
                throw new ArgumentException("完成時間不能早於出貨時間");
            
            _completedAt = value;
        }
    }

    // =========================================================================
    // 快照資訊 (Snapshot Information)
    // =========================================================================

    /// <summary>
    /// 訂單快照，記錄當時的商品資訊、價格等
    /// 儲存格式：JSONB
    /// </summary>
    public OrderSnapshot Snapshot { get; set; }
}
