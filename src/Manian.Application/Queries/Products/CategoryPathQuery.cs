using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢產品類別路徑快取的請求物件
/// 
/// 用途：
/// - 取得指定類別 ID 的完整路徑快取
/// - 路徑快取包含從根節點到當前節點的所有類別 ID
/// - 用於顯示麵包屑導航（Breadcrumb Navigation）
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<int>?>，表示這是一個查詢請求
/// - 回傳可空的整數集合（可能為 null）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CategoryPathCacheQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 顯示產品類別的完整路徑（如：3C > 手機 > 智慧型手機）
/// - 麵包屑導航
/// - 類別層級導航
/// 
/// 路徑快取格式：
/// - 類型：IEnumerable<int>
/// - 內容：從根節點到當前節點的所有類別 ID
/// - 範例：[1, 5, 8] 表示路徑為：類別 1 > 類別 5 > 類別 8
/// 
/// 注意事項：
/// - 路徑快取由資料庫觸發器自動維護
/// - 如果類別不存在，會拋出 BadRequest 例外
/// - 回傳值可能為 null（雖然正常情況下不應該）
/// </summary>
public class CategoryPathCacheQuery : IRequest<IEnumerable<int>?>
{
    /// <summary>
    /// 類別唯一識別碼
    /// 
    /// 用途：
    /// - 用於查詢指定的產品類別
    /// - 必須是資料庫中已存在的類別 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的類別
    /// 
    /// 錯誤處理：
    /// - 如果類別不存在，會拋出 Failure.BadRequest("類目不存在")
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 產品類別路徑快取查詢處理器
/// 
/// 職責：
/// - 接收 CategoryPathCacheQuery 請求
/// - 從資料庫取得指定類別的路徑快取
/// - 回傳從根節點到當前節點的所有類別 ID
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoryPathCacheQuery, IEnumerable<int>?> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICategoryRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
public class CategoryPathCacheQueryHandler : IRequestHandler<CategoryPathCacheQuery, IEnumerable<int>?>
{
    /// <summary>
    /// 產品類別倉儲介面
    /// 
    /// 用途：
    /// - 存取產品類別資料
    /// - 查詢指定 ID 的類別實體
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/CategoryRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 查詢實體
    /// </summary>
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">產品類別倉儲，用於查詢類別資料</param>
    public CategoryPathCacheQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理產品類別路徑快取查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據類別 ID 查詢類別實體
    /// 2. 驗證類別是否存在
    /// 3. 回傳類別的路徑快取
    /// 
    /// 路徑快取說明：
    /// - PathCache 是一個整數陣列
    /// - 包含從根節點到當前節點的所有類別 ID
    /// - 由資料庫觸發器自動維護
    /// - 範例：[1, 5, 8] 表示路徑為：類別 1 > 類別 5 > 類別 8
    /// 
    /// 錯誤處理：
    /// - 類別不存在：拋出 Failure.BadRequest("類目不存在")
    /// </summary>
    /// <param name="request">產品類別路徑快取查詢請求物件，包含類別 ID</param>
    /// <returns>
    /// 類別路徑快取（整數集合）
    /// 格式：從根節點到當前節點的所有類別 ID
    /// 範例：[1, 5, 8]
    /// </returns>
    public async Task<IEnumerable<int>?> HandleAsync(CategoryPathCacheQuery request)
    {
        // ========== 第一步：根據類別 ID 查詢類別實體 ==========
        // 使用 ICategoryRepository.GetByIdAsync() 查詢類別
        // 這個方法會從資料庫中取得完整的類別實體
        var category = await _repository.GetByIdAsync(request.Id);

        // ========== 第二步：驗證類別是否存在 ==========
        // 如果找不到類別，拋出 400 錯誤
        // 這種情況可能發生在：
        // - 類別 ID 不存在
        // - 類別已被刪除（軟刪除）
        if (category == null)
            throw Failure.BadRequest(title:"類目不存在");

        // ========== 第三步：回傳類別的路徑快取 ==========
        // PathCache 是一個整數陣列，包含從根節點到當前節點的所有類別 ID
        // 由資料庫觸發器自動維護，確保資料一致性
        return category.PathCache;
    }
}
