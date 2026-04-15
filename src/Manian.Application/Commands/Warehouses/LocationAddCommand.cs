using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Warehouses;

/// <summary>
/// 新增儲位命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增儲位所需的資訊
/// 設計模式：實作 IRequest<Location>，表示這是一個會回傳 Location 實體的命令
/// 
/// 使用場景：
/// - 管理員建立新的倉庫區域或儲位
/// - 初始化倉庫結構
/// - 擴充倉庫容量
/// 
/// 設計特點：
/// - 回傳型別為 Location，讓呼叫者可以取得新增後的實體（包含自動生成的 ID）
/// - 支援建立兩層級結構（ZONE 和 BIN）
/// - 路徑相關欄位由觸發器自動維護
/// 
/// 注意事項：
/// - 同一父節點下不能有重複的名稱（由資料庫唯一約束保證）
/// - 建議在新增前檢查父節點是否存在
/// - 新增後會自動更新父節點的 IsLeaf 狀態
/// </summary>
public class LocationAddCommand : IRequest<Location>
{
    /// <summary>
    /// 位置名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的儲位名稱
    /// - 用於儲位列表和詳細頁面
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 同一父節點下不能重複（由資料庫唯一約束保證）
    /// 
    /// 範例：
    /// - "A區"：第一層級的區域名稱
    /// - "A01貨架"：第二層級的儲位名稱
    /// - "A01-01"：具體的儲位編號
    /// 
    /// 注意事項：
    /// - 建議長度限制：2-50 字元
    /// - 不應包含特殊字元（除非必要）
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 位置編號
    /// 
    /// 用途：
    /// - 用於條碼/RFID 掃描
    /// - 唯一識別儲位（除了 ID 之外）
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 全域唯一（由資料庫唯一約束保證）
    /// 
    /// 範例：
    /// - "LOC-A01-01"：A區01貨架01號儲位
    /// - "BIN-A01-01"：A區01貨架01號儲位
    /// 
    /// 注意事項：
    /// - 建議使用統一的編碼規則
    /// - 不應包含特殊字元（除了連字號）
    /// </summary>
    public string LocationNumber { get; set; }

    /// <summary>
    /// 位置類型
    /// 
    /// 可選值：
    /// - "ZONE"：區域（第一層級）
    /// - "BIN"：儲位（第二層級）
    /// - "INTERNAL"：虛擬位置
    /// 
    /// 預設值：
    /// - "ZONE"
    /// 
    /// 使用場景：
    /// - ZONE：定義倉庫的功能區域（如收貨區、儲存區）
    /// - BIN：定義具體的儲存位置（如貨架、層板）
    /// - INTERNAL：虛擬位置，用於內部作業（如暫存區）
    /// 
    /// 注意事項：
    /// - 只能接受 "ZONE"、"BIN" 或 "INTERNAL" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// </summary>
    public string LocationType { get; set; }

    /// <summary>
    /// 區域類型
    /// 
    /// 可選值：
    /// - "RECEIVING"：收貨區
    /// - "STORAGE"：儲存區
    /// - "PICKING"：揀貨區
    /// - "PACKING"：包裝區
    /// - "SHIPPING"：出貨區
    /// - "QA"：品檢區
    /// - "RETURNING"：退貨區
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 定義區域的功能用途
    /// - 用於倉庫作業流程控制
    /// 
    /// 注意事項：
    /// - 只對 ZONE 類型的儲位有意義
    /// - BIN 類型的儲位通常繼承父 ZONE 的區域類型
    /// </summary>
    public string? ZoneType { get; set; }

    /// <summary>
    /// 地址
    /// 
    /// 用途：
    /// - 記錄儲位的實體地址
    /// - 用於跨廠區的倉庫管理
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 多廠區的倉庫管理
    /// - 實體地址標記
    /// 
    /// 注意事項：
    /// - 單一倉庫場景下通常不需要
    /// - 建議長度限制：0-200 字元
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 父級 ID
    /// 
    /// 用途：
    /// - 建立儲位的層級結構（父子關係）
    /// - NULL 表示這是一個根區域
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 建立兩層級結構：ZONE > BIN
    /// - 父節點必須是 ZONE 類型
    /// - 子節點必須是 BIN 類型
    /// 
    /// 注意事項：
    /// - 父節點必須已存在
    /// - 新增後會自動更新父節點的 IsLeaf 狀態
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 狀態
    /// 
    /// 可選值：
    /// - "active"：啟用狀態，儲位可被使用
    /// - "inactive"：停用狀態，儲位不可被使用
    /// - "maintenance"：維護中，儲位暫時不可用
    /// 
    /// 預設值：
    /// - "active"
    /// 
    /// 使用場景：
    /// - 暫時停用某個儲位（而非刪除）
    /// - 預先建立儲位但尚未啟用
    /// - 標記維護中的儲位
    /// 
    /// 注意事項：
    /// - 只能接受 "active"、"inactive" 或 "maintenance" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 容量
    /// 
    /// 用途：
    /// - 記錄儲位的最大容量
    /// - 用於庫存管理和規劃
    /// 
    /// 預設值：
    /// - 0
    /// 
    /// 使用場景：
    /// - 庫存規劃
    /// - 容量監控
    /// - 儲位分配
    /// 
    /// 注意事項：
    /// - 單位由 UnitOfMeasureId 決定
    /// - 不應為負數
    /// - 0 表示無限制
    /// </summary>
    public int MaxQuantity { get; set; }

    /// <summary>
    /// 儲存單元 ID
    /// 
    /// 用途：
    /// - 定義儲位容量的計量單位
    /// - 如：個、箱、托盤
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 定義儲位容量的單位
    /// - 庫存計算
    /// 
    /// 注意事項：
    /// - 必須對應資料庫中存在的計量單位
    /// - 預設為 1（個）
    /// </summary>
    public int? UnitOfMeasureId { get; set; }
}

/// <summary>
/// 新增儲位命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 LocationAddCommand 命令
/// - 建立新的 Location 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Location 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<LocationAddCommand, Location> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - CategoryAddHandler：類似的新增邏輯
/// - BrandAddHandler：類似的新增邏輯
/// </summary>
internal class LocationAddHandler : IRequestHandler<LocationAddCommand, Location>
{
    /// <summary>
    /// 唯一識別碼產生器服務
    /// 
    /// 用途：
    /// - 產生全域唯一的整數 ID
    /// - 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/Snowflake.cs
    /// - 註冊為單例模式 (Singleton)
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取儲位資料
    /// - 提供新增、查詢、更新、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<Location>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Warehouses/ILocationRepository.cs
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生儲位 ID</param>
    /// <param name="repository">儲位倉儲，用於存取資料庫</param>
    public LocationAddHandler(
        IUniqueIdentifier uniqueIdentifier,
        ILocationRepository repository)
    {
        _uniqueIdentifier = uniqueIdentifier;
        _repository = repository;
    }

    /// <summary>
    /// 處理新增儲位命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 建立新的 Location 實體
    /// 2. 設定實體屬性
    /// 3. 將實體加入倉儲
    /// 4. 儲存變更到資料庫
    /// 5. 重新查詢實體以確保資料一致性
    /// 6. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 儲存後查詢不到實體：拋出 Failure.BadRequest("新增儲位失敗")
    /// 
    /// 注意事項：
    /// - 路徑相關欄位（Level、PathCache、PathText）由觸發器自動維護
    /// - 新增後會自動更新父節點的 IsLeaf 狀態
    /// - 建議在 UI 層顯示新增成功的訊息
    /// 
    /// 參考實作：
    /// - CategoryAddHandler.HandleAsync：類似的新增邏輯
    /// - BrandAddHandler.HandleAsync：類似的新增邏輯
    /// </summary>
    /// <param name="request">新增儲位命令物件，包含儲位的所有資訊</param>
    /// <returns>儲存後的 Location 實體，包含資料庫自動生成的欄位</returns>
    public async Task<Location> HandleAsync(LocationAddCommand request)
    {
        // ========== 第一步：建立新的 Location 實體 ==========
        var location = new Location
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            Name = request.Name,
            LocationNumber = request.LocationNumber,
            LocationType = request.LocationType,
            ZoneType = request.ZoneType,
            Address = request.Address,
            ParentId = request.ParentId.HasValue ? (int?)request.ParentId.Value : null,
            
            // 設定容量相關屬性
            MaxQuantity = request.MaxQuantity,
            UnitOfMeasureId = request.UnitOfMeasureId ?? 1,
            
            // 設定狀態
            Status = request.Status,
            
            // 設定排序順序，預設為 0
            SortOrder = 0,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第二步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _repository.Add(location);
        
        // ========== 第三步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 同時會觸發資料庫觸發器，自動維護路徑相關欄位
        await _repository.SaveChangeAsync();

        // ========== 第四步：重新查詢實體 ==========
        // 為什麼要重新查詢？
        // 1. 確保實體包含資料庫觸發器自動更新的欄位（如 Level、PathCache、PathText）
        // 2. 確保實體狀態與資料庫一致
        // 3. 避免快取問題
        location = await _repository.GetByIdAsync(location.Id);
        
        // ========== 第五步：驗證實體是否存在 ==========
        // 如果查詢不到實體，表示儲存失敗
        if(location == null) throw Failure.BadRequest("新增儲位失敗");

        // ========== 第六步：回傳儲存後的實體 ==========
        return location;
    }
}
