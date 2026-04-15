namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 退貨單實體
/// 
/// 用途：記錄客戶退貨申請、處理流程、退款狀態等
/// 設計考量：一個訂單項目可以多次退貨（部分退貨），但通常一次退貨對應一個訂單項目
/// </summary>
public class Return
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================
    
    /// <summary>
    /// 退貨單唯一識別碼
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 退貨單編號
    /// </summary>
    public string ReturnNumber { get; set; }
    
    /// <summary>
    /// 訂單項目 ID
    /// </summary>
    public int OrderItemId { get; set; }
    
    // =========================================================================
    // 退貨資訊 (Return Information)
    // =========================================================================
    
    /// <summary>
    /// 退貨數量
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// 退貨原因
    /// </summary>
    public string Reason { get; set; }
    
    // =========================================================================
    // 退貨狀態 (Return Status)
    // =========================================================================
    
    /// <summary>
    /// 退貨狀態
    /// 預設值：requested
    /// 狀態流程：requested(申請) → approved(核准) → received(收到貨) → refunded(已退款)
    ///                                  ↘ rejected(拒絕)
    /// </summary>
    private string _status = "requested";

    /// <summary>
    /// 退貨狀態：requested申請/approved核准/rejected拒絕/received收到貨/refunded已退款
    /// 
    /// 驗證規則：
    /// - 只能接受 "requested"、"approved"、"rejected"、"received" 或 "refunded" 五個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// return.Status = "requested";  // 正確
    /// return.Status = "approved";  // 正確
    /// return.Status = "rejected";  // 正確
    /// return.Status = "received";  // 正確
    /// return.Status = "refunded";  // 正確
    /// return.Status = "pending";    // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "requested"、"approved"、"rejected"、"received" 或 "refunded" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "requested" && value != "approved" && 
                value != "rejected" && value != "received" && value != "refunded")
                throw new ArgumentException("Status 必須是 'requested'、'approved'、'rejected'、'received' 或 'refunded'");
            
            _status = value;
        }
    }
    
    // =========================================================================
    // 退款資訊 (Refund Information)
    // =========================================================================
    
    /// <summary>
    /// 退款金額
    /// </summary>
    public decimal? RefundAmount { get; set; }
    
    /// <summary>
    /// 退款方式：original原路退回/balance購物金/bank_transfer銀行轉帳
    /// </summary>
    public string RefundMethod { get; set; }
    
    /// <summary>
    /// 退款時間
    /// </summary>
    public DateTimeOffset? RefundedAt { get; set; }
    
    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================
    
    /// <summary>
    /// 申請時間
    /// </summary>
    public DateTimeOffset RequestedAt { get; set; }
    
    /// <summary>
    /// 核准時間
    /// </summary>
    public DateTimeOffset? ApprovedAt { get; set; }
    
    /// <summary>
    /// 收到退貨時間
    /// </summary>
    public DateTimeOffset? ReceivedAt { get; set; }
    
    /// <summary>
    /// 記錄建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    // =========================================================================
    // 備註 (Notes)
    // =========================================================================
    
    /// <summary>
    /// 客服/倉管備註
    /// </summary>
    public string StaffNotes { get; set; }
    
    /// <summary>
    /// 客戶備註
    /// </summary>
    public string CustomerNotes { get; set; }
}
