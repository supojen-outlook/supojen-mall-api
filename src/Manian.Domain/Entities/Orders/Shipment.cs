using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 物流單實體
/// 
/// 用途：
/// - 記錄訂單項目的出貨資訊
/// - 包含物流方式、追蹤編號、寄送地址等資訊
/// - 支援一個訂單項目分批出貨
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 支援多種物流方式
/// 
/// 使用場景：
/// - 訂單出貨處理
/// - 物流狀態追蹤
/// - 出貨記錄查詢
/// </summary>
public class Shipment : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 物流單唯一識別碼
    /// 主鍵約束：pk_shipments
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 訂單唯一識別碼
    /// 外鍵約束：fk_shipments_orders
    /// </summary>
    public int OrderId { get; set; }

    // =========================================================================
    // 物流資訊 (Shipping Information)
    // =========================================================================

    /// <summary>
    /// 物流方式
    /// 檢查約束：ck_shipments_method
    /// 可選值：post中華郵政/seven-11/family全家/hilife萊爾富/ok Ok Mart/tcat黑貓/ecam宅配通
    /// </summary>
    private string? _method;

    /// <summary>
    /// 物流方式
    /// 
    /// 驗證規則：
    /// - 只能接受 "post"、"seven"、"family"、"hilife"、"ok"、"tcat" 或 "ecam" 七個值
    /// - 可以為 null
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// shipment.Method = "post";   // 正確
    /// shipment.Method = "tcat";   // 正確
    /// shipment.Method = "seven"; // 正確
    /// shipment.Method = null;     // 正確
    /// shipment.Method = "dhl";    // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 物流方式說明：
    /// - post：中華郵政
    /// - seven：7-11
    /// - family：全家
    /// - hilife：萊爾富
    /// - ok：OK Mart
    /// - tcat：黑貓
    /// - ecam：宅配通
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "post"、"seven"、"family"、"hilife"、"ok"、"tcat" 或 "ecam" 時拋出
    /// </exception>
    public string? Method
    {
        get => _method;
        set
        {
            if (value != null && value != "post" && value != "seven" && 
                value != "family" && value != "hilife" && value != "ok" && 
                value != "tcat" && value != "ecam")
                throw new ArgumentException("Method 必須是 'post'、'seven'、'family'、'hilife'、'ok'、'tcat' 或 'ecam'");
            
            _method = value;
        }
    }

    /// <summary>
    /// 物流追蹤編號
    /// 用途：用於追蹤包裹的運送狀態
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// 寄送地址
    /// 用途：記錄包裹的寄送地址
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// 收件人姓名
    /// 用途：記錄包裹的收件人姓名
    /// </summary>
    public string RecipientName { get; set; }

    /// <summary>   
    /// 收件人電話
    /// 用途：記錄包裹的收件人電話
    /// </summary>
    public string RecipientPhone { get; set; }

    // =========================================================================
    // 時間戳欄位 (Timestamp Fields)
    // =========================================================================

    /// <summary>
    /// 出貨日期
    /// 用途：記錄實際出貨的日期時間
    /// </summary>
    public DateTimeOffset? ShipDate { get; set; }

    /// <summary>
    /// 到貨日期
    /// 用途：記錄包裹實際送達的日期時間
    /// </summary>
    public DateTimeOffset? DeliveredDate { get; set; }


    /// <summary>
    /// 記錄建立時間
    /// 預設值：NOW()
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
