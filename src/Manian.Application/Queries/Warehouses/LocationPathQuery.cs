using Manian.Domain.Repositories.Warehouses;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Warehouses;

/// <summary>
/// 查詢儲位路徑快取的請求物件
/// 
/// 用途：
/// - 取得指定儲位 ID 的完整路徑快取
/// - 路徑快取包含從根節點到當前節點的所有儲位 ID
/// - 用於顯示麵包屑導航（Breadcrumb Navigation）
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<int>?>，表示這是一個查詢請求
/// - 回傳可空的整數集合（可能為 null）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 LocationPathQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 顯示儲位的完整路徑（如：A區 > A01貨架 > A01-01儲位）
/// - 麵包屑導航
/// - 儲位層級導航
/// 
/// 路徑快取格式：
/// - 類型：IEnumerable<int>
/// - 內容：從根節點到當前節點的所有儲位 ID
/// - 範例：[1, 5, 8] 表示路徑為：儲位 1 > 儲位 5 > 儲位 8
/// 
/// 注意事項：
/// - 路徑快取由資料庫觸發器自動維護
/// - 如果儲位不存在，會拋出 BadRequest 例外
/// - 回傳值可能為 null（雖然正常情況下不應該）
/// 
/// 參考實作：
/// - CategoryPathQuery：類似的實作模式，用於查詢類別路徑
/// </summary>
public class LocationPathQuery : IRequest<IEnumerable<int>?>
{
    /// <summary>
    /// 儲位唯一識別碼
    /// 
    /// 用途：
    /// - 用於查詢指定的儲位
    /// - 必須是資料庫中已存在的儲位 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的儲位
    /// 
    /// 錯誤處理：
    /// - 如果儲位不存在，會拋出 Failure.BadRequest("儲位不存在")
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 儲位路徑快取查詢處理器
/// 
/// 職責：
/// - 接收 LocationPathQuery 請求
/// - 從資料庫取得指定儲位的路徑快取
/// - 回傳從根節點到當前節點的所有儲位 ID
/// 
/// 設計模式：
/// - 實作 IRequestHandler<LocationPathQuery, IEnumerable<int>?> 介面
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
/// 參考實作：
/// - CategoryPathQueryHandler：類似的實作模式
/// </summary>
public class LocationPathQueryHandler : IRequestHandler<LocationPathQuery, IEnumerable<int>?>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取儲位資料
    /// - 查詢指定 ID 的儲位實體
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 查詢實體
    /// - 繼承自 Repository<Location>，獲得通用 CRUD 功能
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢儲位資料</param>
    public LocationPathQueryHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理儲位路徑快取查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據儲位 ID 查詢儲位實體
    /// 2. 驗證儲位是否存在
    /// 3. 回傳儲位的路徑快取
    /// 
    /// 路徑快取說明：
    /// - PathCache 是一個整數陣列
    /// - 包含從根節點到當前節點的所有儲位 ID
    /// - 由資料庫觸發器自動維護
    /// - 範例：[1, 5, 8] 表示路徑為：儲位 1 > 儲位 5 > 儲位 8
    /// 
    /// 錯誤處理：
    /// - 儲位不存在：拋出 Failure.BadRequest("儲位不存在")
    /// </summary>
    /// <param name="request">儲位路徑快取查詢請求物件，包含儲位 ID</param>
    /// <returns>
    /// 儲位路徑快取（整數集合）
    /// 格式：從根節點到當前節點的所有儲位 ID
    /// 範例：[1, 5, 8]
    /// </returns>
    public async Task<IEnumerable<int>?> HandleAsync(LocationPathQuery request)
    {
        // ========== 第一步：根據儲位 ID 查詢儲位實體 ==========
        // 使用 ILocationRepository.GetByIdAsync() 查詢儲位
        // 這個方法會從資料庫中取得完整的儲位實體
        var location = await _repository.GetByIdAsync(request.Id);

        // ========== 第二步：驗證儲位是否存在 ==========
        // 如果找不到儲位，拋出 400 錯誤
        // 這種情況可能發生在：
        // - 儲位 ID 不存在
        // - 儲位已被刪除（軟刪除）
        if (location == null)
            throw Failure.BadRequest(title:"儲位不存在");

        // ========== 第三步：回傳儲位的路徑快取 ==========
        // PathCache 是一個整數陣列，包含從根節點到當前節點的所有儲位 ID
        // 由資料庫觸發器自動維護，確保資料一致性
        return location.PathCache;
    }
}
