using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Orders;

/// <summary>
/// 揀貨項目實體
/// 
/// 用途：
/// - 記錄訂單項目應從哪個具體儲位提取多少數量的指引
/// - 支援拆單揀貨：一個 order_item 可對應多個 pick_items
/// - 追蹤揀貨狀態從已分配到已揀貨的過程
/// 
/// 設計考量：
/// - 不使用 Attribute 註解，遵循專案慣例
/// - 使用屬性驗證確保資料完整性
/// - 與資料庫約束保持一致
/// - 不包含導航屬性
/// </summary>
public class PickItem : IEntity
{
    // =========================================================================
    // 基礎資訊欄位 (Basic Information)
    // =========================================================================

    /// <summary>
    /// 揀貨項目唯一識別碼
    /// 主鍵約束：pk_pick_items
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 關聯的訂單 ID
    /// 外鍵約束：fk_pick_items_order
    /// 級聯：刪除訂單時自動刪除揀貨清單
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 關聯的訂單項目 ID
    /// 外鍵約束：fk_pick_items_order_item
    /// 級聯：刪除訂單項目時自動刪除揀貨清單
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// 關聯的庫存記錄 ID
    /// 外鍵約束：fk_pick_items_inventory
    /// 約束：刪除庫存記錄時禁止刪除揀貨項目
    /// </summary>
    public int InventoryId { get; set; }


    /// <summary>
    /// 指向具體的儲位/貨架 ID
    /// 外鍵約束：fk_pick_items_location
    /// 約束：刪除儲位時禁止刪除揀貨項目
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// 產品圖片 URL
    /// </summary>
    public string ProductImageUrl { get; set; }

    // =========================================================================
    // 數量資訊 (Quantity Information)
    // =========================================================================

    /// <summary>
    /// 應揀貨數量
    /// 檢查約束：ck_pick_items_quantity_to_pick (quantity_to_pick > 0)
    /// </summary>
    private int _quantityToPick;

    /// <summary>
    /// 應揀貨數量
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// </summary>
    public int QuantityToPick
    {
        get => _quantityToPick;
        set
        {
            if (value <= 0)
                throw new ArgumentException("應揀貨數量必須大於 0");
            
            _quantityToPick = value;
        }
    }

    /// <summary>
    /// 實際已揀貨數量
    /// 預設值：0
    /// 檢查約束：ck_pick_items_quantity_picked (quantity_picked >= 0)
    /// </summary>
    private int _quantityPicked = 0;

    /// <summary>
    /// 實際已揀貨數量
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 不能超過應揀貨數量
    /// </summary>
    public int QuantityPicked
    {
        get => _quantityPicked;
        set
        {
            if (value < 0)
                throw new ArgumentException("實際已揀貨數量不能為負數");
            
            if (value > _quantityToPick)
                throw new ArgumentException("實際已揀貨數量不能超過應揀貨數量");
            
            _quantityPicked = value;
        }
    }

    // =========================================================================
    // 狀態與控制欄位 (Status & Control Fields)
    // =========================================================================

    /// <summary>
    /// 揀貨狀態
    /// 預設值：allocated
    /// 狀態流程：allocated(已分配/待揀貨) → picked(已完成揀貨) → cancelled(已取消)
    /// 檢查約束：ck_pick_items_status
    /// </summary>
    private string _status = "allocated";

    /// <summary>
    /// 揀貨狀態：allocated已分配/picked已完成/cancelled已取消
    /// 
    /// 驗證規則：
    /// - 只能接受 "allocated"、"picked" 或 "cancelled" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// pickItem.Status = "allocated";   // 正確
    /// pickItem.Status = "picked";     // 正確
    /// pickItem.Status = "cancelled";  // 正確
    /// pickItem.Status = "shipped";    // 會拋出 ArgumentException
    /// </code>
    /// 
    /// 狀態轉換規則：
    /// - allocated → picked
    /// - allocated → cancelled
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "allocated"、"picked" 或 "cancelled" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "allocated" && value != "picked" && value != "cancelled")
                throw new ArgumentException("Status 必須是 'allocated'、'picked' 或 'cancelled'");
            
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
    /// 實際完成揀貨的時間
    /// 可為 null
    /// </summary>
    public DateTimeOffset? PickedAt { get; set; }
}
