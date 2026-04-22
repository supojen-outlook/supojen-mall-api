using System.Text.Json;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 付款記錄實體
/// 
/// 用途：
/// - 記錄訂單的付款資訊
/// - 包含付款方式、金額、狀態等關鍵資訊
/// - 支援多筆付款記錄（如部分付款、分期）
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 支援 JSONB 快照功能
/// 
/// 使用場景：
/// - 訂單付款處理
/// - 付款狀態追蹤
/// - 金流平台對帳
/// </summary>
public class Payment : IEntity, IDisposable
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 付款記錄唯一識別碼
    /// 主鍵約束：pk_payments
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 訂單 ID
    /// 外鍵約束：fk_payments_order
    /// 約束：有付款記錄的訂單不能被刪除
    /// </summary>
    public int OrderId { get; set; }

    // =========================================================================
    // 付款方式 (Payment Method)
    // =========================================================================

    /// <summary>
    /// 付款方式
    /// 檢查約束：ck_payments_method
    /// </summary>
    private string _method = string.Empty;

    /// <summary>
    /// 付款方式
    /// 
    /// 驗證規則：
    /// - 只能接受指定的付款方式
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// payment.Method = "credit_card_one_time"; // 正確
    /// payment.Method = "mobile_payment";      // 正確
    /// payment.Method = "atm_virtual";          // 正確
    /// payment.Method = "paypal";               // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 付款方式說明：
    /// - credit_card_one_time: 一次付清信用卡
    /// - atm_virtual: 虛擬帳號轉帳
    /// - taiwan_pay: 台灣支付
    /// - cash: 現金
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是有效的付款方式時拋出
    /// </exception>
    public string Method
    {
        get => _method;
        set
        {
            if (value != "credit_card_one_time" && value != "atm_virtual" && value != "taiwan_pay" &&  value != "cash" && value != "cvs" && value != "other")
                throw new ArgumentException("Method 必須是有效的付款方式");
    
            _method = value;
        }
    }

    // =========================================================================
    // 金流資訊 (Payment Gateway Information)
    // =========================================================================

    /// <summary>
    /// 金流平台交易編號
    /// 用途：用於金流平台對帳和查詢
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// 付款金額
    /// 檢查約束：ck_payments_amount (amount > 0)
    /// </summary>
    private decimal _amount;

    /// <summary>
    /// 付款金額
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// </summary>
    public decimal Amount
    {
        get => _amount;
        set
        {
            if (value <= 0)
                throw new ArgumentException("付款金額必須大於 0");
            
            _amount = value;
        }
    }

    // =========================================================================
    // 銀行資訊 (Bank Information) 或是 超商資訊 (Convenience Store Information)
    // =========================================================================

    /// <summary>
    /// 銀行代碼
    /// </summary>
    public string? BankCode { get; set; }

    /// <summary>
    /// 銀行帳號或是超商代碼
    /// </summary>
    public string? CodeNo { get; set; }

    /// <summary>
    /// (ATM匯款)超商繳費期限
    /// </summary>
    public DateOnly? ExpiredAt { get; set; }
    
    // =========================================================================
    // 付款狀態 (Payment Status)
    // =========================================================================

    /// <summary>
    /// 付款狀態
    /// 預設值：pending
    /// 狀態流程：pending(處理中) → paid(已付款) 
    ///                               ↘ failed(失敗) / refunded(已退款)
    /// 檢查約束：ck_payments_status
    /// </summary>
    private string _status = "pending";

    /// <summary>
    /// 付款狀態：pending處理中/paid已付款/failed失敗/refunded已退款
    /// 
    /// 驗證規則：
    /// - 只能接受 "pending"、"paid"、"failed" 或 "refunded" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// payment.Status = "pending";   // 正確
    /// payment.Status = "paid";      // 正確
    /// payment.Status = "failed";    // 正確
    /// payment.Status = "refunded";  // 正確
    /// payment.Status = "cancelled"; // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 狀態轉換規則：
    /// - pending → paid
    /// - pending → failed
    /// - paid → refunded
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "pending"、"paid"、"failed" 或 "refunded" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "pending" && value != "paid" && 
                value != "failed" && value != "refunded")
                throw new ArgumentException("Status 必須是 'pending'、'paid'、'failed' 或 'refunded'");
            
            _status = value;
        }
    }

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 記錄建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 付款時間
    /// 檢查約束：ck_payments_timeline (paid_at >= created_at)
    /// </summary>
    private DateTimeOffset? _paidAt;

    /// <summary>
    /// 付款時間
    /// 
    /// 驗證規則：
    /// - 不能早於記錄建立時間
    /// </summary>
    public DateTimeOffset? PaidAt
    {
        get => _paidAt;
        set
        {
            if (value.HasValue && value.Value < CreatedAt)
                throw new ArgumentException("付款時間不能早於記錄建立時間");
            
            _paidAt = value;
        }
    }

    /// <summary>
    /// 退款時間
    /// 檢查約束：ck_payments_timeline (refunded_at >= paid_at)
    /// </summary>
    private DateTimeOffset? _refundedAt;

    /// <summary>
    /// 退款時間
    /// 
    /// 驗證規則：
    /// - 不能早於付款時間
    /// </summary>
    public DateTimeOffset? RefundedAt
    {
        get => _refundedAt;
        set
        {
            if (value.HasValue && _paidAt.HasValue && value.Value < _paidAt.Value)
                throw new ArgumentException("退款時間不能早於付款時間");
            
            _refundedAt = value;
        }
    }

    // =========================================================================
    // 快照資訊 (Snapshot Information)
    // =========================================================================

    /// <summary>
    /// 金流平台回傳的完整交易資訊
    /// 儲存格式：JSONB
    /// </summary>
    public JsonDocument? Snapshot { get; set; }

    // =========================================================================
    // 資源釋放 (Resource Disposal)
    // =========================================================================

    /// <summary>
    /// 釋放資源的方法
    /// 
    /// 職責：
    /// - 釋放 JsonDocument 物件占用的資源
    /// - 防止記憶體洩漏
    /// - 符合 IDisposable 模式
    /// 
    /// 設計考量：
    /// - JsonDocument 實作 IDisposable，需要手動釋放
    /// - 使用 null 條件運算子避免 NullReferenceException
    /// - 只釋放 Snapshot，不釋放其他屬性
    /// 
    /// 使用場景：
    /// - 付款記錄不再使用時
    /// - 付款記錄生命週期結束時
    /// - 由垃圾回收器自動呼叫（如果使用 using 區塊）
    /// 
    /// 注意事項：
    /// - 釋放後，Snapshot 屬性不可再使用
    /// - 建議使用 using 區塊確保資源被釋放
    /// - 不應在 Dispose 後再存取 Snapshot
    /// 
    /// 參考實作：
    /// - PointTransaction.Dispose：類似的資源釋放邏輯
    /// - Order.Dispose：類似的資源釋放邏輯
    /// </summary>
    public void Dispose()
    {
        // 使用 null 條件運算子 (?.) 安全地釋放 Snapshot
        // 如果 Snapshot 為 null，不會執行 Dispose 方法
        // 避免拋出 NullReferenceException
        Snapshot?.Dispose();
    }
}
