using Manian.Domain.Repositories.Warehouses;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Warehouses;

/// <summary>
/// 更新儲位命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新儲位所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員修改儲位資訊
/// - 儲位資料維護
/// - 儲位結構調整
/// 
/// 設計特點：
/// - 所有屬性皆為可空（nullable），支援部分更新
/// - 未提供的欄位保持原值不變
/// - 遵循 HTTP PATCH 語意（部分更新）
/// 
/// 注意事項：
/// - 更新儲位可能會影響已關聯的庫存
/// - 建議在更新前檢查是否有庫存使用此儲位
/// - 更新 ParentId 會影響路徑結構
/// </summary>
public class LocationUpdateCommand : IRequest
{
    /// <summary>
    /// 儲位唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的儲位
    /// - 必須是資料庫中已存在的儲位 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的儲位
    /// 
    /// 錯誤處理：
    /// - 如果儲位不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 位置名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的儲位名稱
    /// - 用於儲位列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議長度限制：2-50 字元
    /// - 不應為空白或僅包含空白字元
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 位置編號
    /// 
    /// 用途：
    /// - 用於條碼/RFID 掃描
    /// - 唯一識別儲位（除了 ID 之外）
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議使用統一的編碼規則
    /// - 不應包含特殊字元（除了連字號）
    /// </summary>
    public string? LocationNumber { get; set; }

    /// <summary>
    /// 位置類型
    /// 
    /// 可選值：
    /// - "ZONE"：區域（第一層級）
    /// - "BIN"：儲位（第二層級）
    /// - "INTERNAL"：虛擬位置
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 只能接受 "ZONE"、"BIN" 或 "INTERNAL" 三個值
    /// - 設定其他值會拋出 ArgumentException
    /// </summary>
    public string? LocationType { get; set; }

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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 建立兩層級結構：ZONE > BIN
    /// - 父節點必須是 ZONE 類型
    /// - 子節點必須是 BIN 類型
    /// 
    /// 注意事項：
    /// - 父節點必須已存在
    /// - 更新後會影響儲位的層級結構
    /// - 更新後會自動重新計算路徑相關欄位
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 狀態
    /// 
    /// 可選值：
    /// - "active"：啟用狀態，儲位可被使用
    /// - "inactive"：停用狀態，儲位不可被使用
    /// - "maintenance"：維護中，儲位暫時不可用
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    public string? Status { get; set; }

    /// <summary>
    /// 容量
    /// 
    /// 用途：
    /// - 記錄儲位的最大容量
    /// - 用於庫存管理和規劃
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// 儲存單元 ID
    /// 
    /// 用途：
    /// - 定義儲位容量的計量單位
    /// - 如：個、箱、托盤
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
/// 更新儲位命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 LocationUpdateCommand 命令
/// - 查詢儲位是否存在
/// - 更新儲位資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<LocationUpdateCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ILocationRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查 LocationType 是否為有效值（ZONE/BIN/INTERNAL）
/// - 未檢查 Status 是否為有效值（active/inactive/maintenance）
/// - 未檢查 ParentId 是否存在
/// - 未檢查是否會造成循環引用
/// - 建議考慮使用樂觀鎖（Optimistic Concurrency）防止並發更新衝突
/// 
/// 參考實作：
/// - CategoryUpdateHandler：類似的更新邏輯
/// - BrandUpdateHandler：類似的更新邏輯
/// </summary>
internal class LocationUpdateHandler : IRequestHandler<LocationUpdateCommand>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取儲位資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 繼承自 Repository<Location>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Warehouses/ILocationRepository.cs
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢和更新儲位</param>
    public LocationUpdateHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新儲位命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢儲位實體
    /// 2. 驗證儲位是否存在
    /// 3. 更新儲位屬性（只更新非 null 的欄位）
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 儲位不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否有子節點或庫存使用此儲位
    /// - 建議檢查是否會造成循環引用
    /// - 更新 ParentId 後會自動重新計算路徑相關欄位
    /// 
    /// 參考實作：
    /// - CategoryUpdateHandler.HandleAsync：類似的更新邏輯
    /// - BrandUpdateHandler.HandleAsync：類似的更新邏輯
    /// </summary>
    /// <param name="request">更新儲位命令物件，包含儲位 ID 和要更新的欄位</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(LocationUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢儲位實體 ==========
        // 使用 ILocationRepository.GetByIdAsync() 查詢儲位
        // 這個方法會從資料庫中取得完整的儲位實體
        var location = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證儲位是否存在 ==========
        // 如果找不到儲位，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 儲位 ID 不存在
        // - 儲位已被刪除（軟刪除）
        if (location == null)
            throw Failure.NotFound($"儲位不存在，ID: {request.Id}");

        // ========== 第三步：更新儲位屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新基本屬性
        if (request.Name != null) location.Name = request.Name;
        if (request.LocationNumber != null) location.LocationNumber = request.LocationNumber;
        if (request.LocationType != null) location.LocationType = request.LocationType;
        if (request.ZoneType != null) location.ZoneType = request.ZoneType;
        if (request.Address != null) location.Address = request.Address;
        
        // 更新層級結構相關屬性
        if (request.ParentId != null) location.ParentId = request.ParentId;
        
        // 更新狀態和容量相關屬性
        if (request.Status != null) location.Status = request.Status;
        if (request.MaxQuantity != null) location.MaxQuantity = request.MaxQuantity.Value;
        if (request.UnitOfMeasureId != null) location.UnitOfMeasureId = request.UnitOfMeasureId.Value;

        // ========== 第四步：儲存變更 ==========
        // 使用 ILocationRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        // EF Core 會自動產生 UPDATE SQL 語句
        // 同時會觸發資料庫觸發器，自動維護路徑相關欄位
        await _repository.SaveChangeAsync();
    }
}
