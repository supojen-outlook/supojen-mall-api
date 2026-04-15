using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢品牌路徑快取的請求物件
/// 
/// 用途：
/// - 取得指定品牌 ID 的完整路徑快取
/// - 路徑快取包含從根節點到當前節點的所有品牌 ID
/// - 用於顯示麵包屑導航（Breadcrumb Navigation）
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<int>?>，表示這是一個查詢請求
/// - 回傳可空的整數集合（可能為 null）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 BrandPathCacheQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 顯示品牌的完整路徑（如：Nike > 服飾 > 運動鞋）
/// - 麵包屑導航
/// - 品牌層級導航
/// 
/// 路徑快取格式：
/// - 類型：IEnumerable<int>
/// - 內容：從根節點到當前節點的所有品牌 ID
/// - 範例：[1, 5, 8] 表示路徑為：品牌 1 > 品牌 5 > 品牌 8
/// 
/// 注意事項：
/// - 路徑快取由資料庫觸發器自動維護
/// - 如果品牌不存在，會拋出 BadRequest 例外
/// - 回傳值可能為 null（雖然正常情況下不應該）
/// 
/// 參考實作：
/// - CategoryPathCacheQuery：類似的實作模式，用於查詢類別路徑
/// </summary>
public class BrandPathCacheQuery : IRequest<IEnumerable<int>?>
{
    /// <summary>
    /// 品牌唯一識別碼
    /// 
    /// 用途：
    /// - 用於查詢指定的品牌
    /// - 必須是資料庫中已存在的品牌 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的品牌
    /// 
    /// 錯誤處理：
    /// - 如果品牌不存在，會拋出 Failure.BadRequest("品牌不存在")
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 品牌路徑快取查詢處理器
/// 
/// 職責：
/// - 接收 BrandPathCacheQuery 請求
/// - 從資料庫取得指定品牌的路徑快取
/// - 回傳從根節點到當前節點的所有品牌 ID
/// 
/// 設計模式：
/// - 實作 IRequestHandler<BrandPathCacheQuery, IEnumerable<int>?> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IBrandRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - CategoryPathCacheQueryHandler：類似的實作模式
/// </summary>
public class BrandPathCacheQueryHandler : IRequestHandler<BrandPathCacheQuery, IEnumerable<int>?>
{
    /// <summary>
    /// 品牌倉儲介面
    /// 
    /// 用途：
    /// - 存取品牌資料
    /// - 查詢指定 ID 的品牌實體
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/BrandRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 查詢實體
    /// - 繼承自 Repository<Brand>，獲得通用 CRUD 功能
    /// </summary>
    private readonly IBrandRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">品牌倉儲，用於查詢品牌資料</param>
    public BrandPathCacheQueryHandler(IBrandRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理品牌路徑快取查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據品牌 ID 查詢品牌實體
    /// 2. 驗證品牌是否存在
    /// 3. 回傳品牌的路徑快取
    /// 
    /// 路徑快取說明：
    /// - PathCache 是一個整數陣列
    /// - 包含從根節點到當前節點的所有品牌 ID
    /// - 由資料庫觸發器自動維護
    /// - 範例：[1, 5, 8] 表示路徑為：品牌 1 > 品牌 5 > 品牌 8
    /// 
    /// 錯誤處理：
    /// - 品牌不存在：拋出 Failure.BadRequest("品牌不存在")
    /// </summary>
    /// <param name="request">品牌路徑快取查詢請求物件，包含品牌 ID</param>
    /// <returns>
    /// 品牌路徑快取（整數集合）
    /// 格式：從根節點到當前節點的所有品牌 ID
    /// 範例：[1, 5, 8]
    /// </returns>
    public async Task<IEnumerable<int>?> HandleAsync(BrandPathCacheQuery request)
    {
        // ========== 第一步：根據品牌 ID 查詢品牌實體 ==========
        // 使用 IBrandRepository.GetByIdAsync() 查詢品牌
        // 這個方法會從資料庫中取得完整的品牌實體
        var brand = await _repository.GetByIdAsync(request.Id);

        // ========== 第二步：驗證品牌是否存在 ==========
        // 如果找不到品牌，拋出 400 錯誤
        // 這種情況可能發生在：
        // - 品牌 ID 不存在
        // - 品牌已被刪除（軟刪除）
        if (brand == null)
            throw Failure.BadRequest(title:"品牌不存在");

        // ========== 第三步：回傳品牌的路徑快取 ==========
        // PathCache 是一個整數陣列，包含從根節點到當前節點的所有品牌 ID
        // 由資料庫觸發器自動維護，確保資料一致性
        return brand.PathCache;
    }
}
