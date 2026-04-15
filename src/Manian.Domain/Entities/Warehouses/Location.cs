using System;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Warehouses;

/// <summary>
/// 儲位實體
/// 用途：定義倉庫內的所有實體位置，包含區域、儲位等
/// 設計考量：
/// - 使用 parent_id 建立自我參照的樹狀結構
/// - path_cache 和 level 優化樹狀結構的查詢效能
/// - 由觸發器自動維護層級相關欄位
/// - 目前只有單一倉庫，最高層級為區域 (ZONE)，不再有 DEPOT 層級
/// - 資料量 < 1000 筆，索引以精簡為原則
/// </summary>
public class Location : IEntity
{
    /// <summary>
    /// 位置唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 位置名稱，如：A區-貨架01
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 位置編號，用於條碼/RFID 掃描
    /// </summary>
    public string? LocationNumber { get; set; }

    /// <summary>
    /// 位置性質：ZONE區域/BIN儲位/INTERNAL虛擬
    /// 預設值：ZONE
    /// </summary>
    private string _locationType = "ZONE";

    /// <summary>
    /// 位置性質：ZONE區域/BIN儲位/INTERNAL虛擬
    /// 
    /// 驗證規則：
    /// - 只能接受 "ZONE"、"BIN" 或 "INTERNAL" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// location.LocationType = "ZONE";     // 正確
    /// location.LocationType = "BIN";      // 正確
    /// location.LocationType = "INTERNAL"; // 正確
    /// location.LocationType = "DEPOT";   // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "ZONE"、"BIN" 或 "INTERNAL" 時拋出
    /// </exception>
    public string LocationType
    {
        get => _locationType;
        set
        {
            if (value != "ZONE" && value != "BIN" && value != "INTERNAL")
                throw new ArgumentException("LocationType 必須是 'ZONE'、'BIN' 或 'INTERNAL'");
            
            _locationType = value;
        }
    }

    /// <summary>
    /// 區域功能：RECEIVING收貨/STORAGE儲存/PICKING揀貨/PACKING包裝/SHIPPING出貨/QA品檢/RETURNING退貨
    /// 預設值：null
    /// </summary>
    public string? ZoneType { get; set; }

    /// <summary>
    /// 上層位置 ID，NULL 表示根區域
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 所在層級：1為區域，2為儲位
    /// 由觸發器自動維護
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 從根到目前節點的所有 ID 陣列，如：{1,5,8}
    /// 由觸發器自動維護
    /// </summary>
    public int[] PathCache { get; set; }

    /// <summary>
    /// 從根到目前節點的路徑文字，如：'/A區/A01貨架'
    /// 由觸發器自動維護
    /// </summary>
    public string PathText { get; set; }

    /// <summary>
    /// 計量單位 ID，如：個、箱、托盤
    /// 預設值：1
    /// </summary>
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// 最大儲存數量
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// 實體地址（如果跨廠區才需要）
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 排序順序，數字越小越前面
    /// 預設值：0
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 狀態：active啟用/inactive停用/maintenance維護中
    /// 預設值：active
    /// </summary>
    private string _status = "active";

    /// <summary>
    /// 狀態：active啟用/inactive停用/maintenance維護中
    /// 
    /// 驗證規則：
    /// - 只能接受 "active"、"inactive" 或 "maintenance" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// location.Status = "active";       // 正確
    /// location.Status = "inactive";     // 正確
    /// location.Status = "maintenance";  // 正確
    /// location.Status = "deleted";       // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "active"、"inactive" 或 "maintenance" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "active" && value != "inactive" && value != "maintenance")
                throw new ArgumentException("Status 必須是 'active'、'inactive' 或 'maintenance'");
            
            _status = value;
        }
    }

    /// <summary>
    /// 位置建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
