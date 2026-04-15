using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Warehouses;

/// <summary>
/// 庫存實體
/// 用途：記錄每個 SKU 在各個儲位的即時庫存數量
/// 設計考量：
/// - 一個 SKU 可以放在多個儲位，透過 (sku_id + location_id) 唯一識別
/// - 可銷售庫存 = quantity_on_hand - quantity_reserved，由資料庫觸發器自動維護
/// </summary>
public class Inventory : IEntity
{
    /// <summary>
    /// 庫存記錄唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// SKU ID，關聯到 skus 表
    /// </summary>
    public int SkuId { get; set; }

    /// <summary>
    /// 儲位 ID，關聯到 locations 表
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// 實際庫存數量
    /// 約束：不能為負
    /// </summary>
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// 預占庫存量（已訂未出）
    /// 約束：不能為負
    /// </summary>
    public int QuantityReserved { get; set; }

    /// <summary>
    /// 可銷售庫存量
    /// 計算方式：quantity_on_hand - quantity_reserved
    /// 由資料庫觸發器自動維護
    /// </summary>
    public int QuantityAvailable { get; set; }

    /// <summary>
    /// 庫存狀態：active啟用/inactive停用/quarantined隔離
    /// 預設值：active
    /// </summary>
    private string _status = "active";

    public string Status
    {
        get => _status;
        set
        {
            if (value != "active" && value != "inactive" && value != "quarantined")
                throw new ArgumentException("Status 必須是 'active'、'inactive' 或 'quarantined'");
            _status = value;
        }
    }

    /// <summary>
    /// 是否可用於銷售
    /// 預設值：true
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 庫存記錄建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
