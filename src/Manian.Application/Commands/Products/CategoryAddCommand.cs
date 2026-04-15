using Manian.Application.Commands.Assets;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;


namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增產品類別命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增產品類別所需的資訊
/// 設計模式：實作 IRequest<Category>，表示這是一個會回傳 Category 實體的命令
/// </summary>
public class CategoryAddCommand : IRequest<Category>
{
    /// <summary>
    /// 做 SEO 用
    /// URL 友好名稱，用於優化搜尋引擎排名
    /// 例如："smartphones" 而非 "智慧型手機"
    /// </summary>
    public string? Slug { get; set; }
    
    /// <summary>
    /// 分類名稱
    /// 顯示給使用者看的類別名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 排序順序
    /// 用於控制同層級類別的顯示順序，數字越小越前面
    /// </summary>
    public int? SortOrder { get; set; }
    
    /// <summary>
    /// 類目狀態
    /// 可能值："active"（啟用）或 "inactive"（停用）
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 分類圖片
    /// 類別的縮圖或圖示 URL
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// 上層分類 ID
    /// 用於建立多層級的分類結構
    /// null 表示這是一個根類別
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// 是否為最終類目
    /// true 表示這個類別沒有子類別
    /// false 表示這個類別可以包含其他類別
    /// </summary>
    public bool IsLeaf { get; set; }
}


/// <summary>
/// 新增產品類別命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CategoryAddCommand 命令
/// - 建立新的 Category 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Category 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoryAddCommand, Category> 介面
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
/// </summary>
public class CategoryAddHandler : IRequestHandler<CategoryAddCommand, Category>
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
    /// 產品類別倉儲介面
    /// 
    /// 用途：
    /// - 存取產品類別資料
    /// - 提供新增、查詢、更新、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/CategoryRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// </summary>
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// 中介者服務
    /// 
    /// 用途：
    /// - 傳遞命令和事件
    /// - 處理跨邊界邏輯
    /// 
    /// 實作方式：
    /// - 使用 MediatR 框架（見 Infrastructure/MediatR/MediatorExtensions.cs）
    /// - 提供 SendAsync 方法傳遞命令和事件
    /// </summary>
    private readonly IMediator _mediator;
    
    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生類別 ID</param>
    /// <param name="repository">產品類別倉儲，用於存取資料庫</param>
    /// <<param name="mediator">中介者服務，用於傳遞命令和事件</param>
    public CategoryAddHandler(
        IUniqueIdentifier uniqueIdentifier,
        ICategoryRepository repository,
        IMediator mediator)
    {
        _uniqueIdentifier = uniqueIdentifier;
        _repository = repository;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理新增產品類別命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 建立新的 Category 實體
    /// 2. 設定實體屬性
    /// 3. 將實體加入倉儲
    /// 4. 儲存變更到資料庫
    /// 5. 重新查詢實體以確保資料一致性
    /// 6. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 儲存後查詢不到實體：拋出 Failure.BadRequest("新增分類失敗")
    /// </summary>
    /// <param name="request">新增產品類別命令物件，包含類別的所有資訊</param>
    /// <returns>儲存後的 Category 實體，包含資料庫自動生成的欄位</returns>
    public async Task<Category> HandleAsync(CategoryAddCommand request)
    {
        // ========== 第一步：建立新的 Category 實體 ==========
        var category = new Category
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            Name = request.Name,
            Slug = request.Slug,
            ParentId = request.ParentId,
            
            // 如果未提供排序順序，預設為 0
            SortOrder = request.SortOrder ?? 0,
            
            // 設定狀態
            Status = request.Status,
            
            // 設定是否為葉節點
            IsLeaf = request.IsLeaf,
            
            // 如果未提供圖片 URL，使用預設圖片
            ImageUrl = request.ImageUrl ?? "https://demo-po.sgp1.digitaloceanspaces.com/default_category.png",
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第二步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _repository.Add(category);
        
        // ========== 第三步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        await _repository.SaveChangeAsync();

        // ========== 第四步：更新資產庫 ==========
        // 將LOGO圖片更新到資產庫
        if(request.ImageUrl != null)
        {
            await _mediator.SendAsync(new AssetUpdateCommand()
            {
                Urls = [ request.ImageUrl ],
                TargetType = "category",
                TargetId = category.Id
            });   
        }

        // ========== 第五步：重新查詢實體 ==========
        // 為什麼要重新查詢？
        // 1. 確保實體包含資料庫觸發器自動更新的欄位（如 Level、PathCache、PathText）
        // 2. 確保實體狀態與資料庫一致
        // 3. 避免快取問題
        category = await _repository.GetByIdAsync(category.Id);
        
        // ========== 第六步：驗證實體是否存在 ==========
        // 如果查詢不到實體，表示儲存失敗
        if(category == null) throw Failure.BadRequest("新增分類失敗");

        // ========== 第七步：回傳儲存後的實體 ==========
        return category;
    }
}
