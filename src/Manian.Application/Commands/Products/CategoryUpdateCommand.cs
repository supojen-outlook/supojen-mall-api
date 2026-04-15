using Manian.Application.Commands.Assets;
using Manian.Domain.Repositories.Assets;
using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 更新產品類別命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新產品類別所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 設計考量：
/// - 所有屬性皆為可空，支援部分更新
/// - 使用 IEnumerable<int> 表示屬性鍵集合
/// - 遵循 CQRS 原則，與查詢操作分離
/// </summary>
public class CategoryUpdateCommand : IRequest
{
    /// <summary>
    /// 類目 ID
    /// 
    /// 用途：
    /// - 用於識別要更新的類別
    /// - 必須是資料庫中已存在的類別 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的類別
    /// 
    /// 錯誤處理：
    /// - 如果類別不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; } 
    
    /// <summary>
    /// 類目名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的類別名稱
    /// - 支援多語言（可擴充）
    /// 
    /// 驗證規則：
    /// - 不為空時，長度限制為 1-100 字元
    /// - 不為空時，不能包含特殊字元
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// SEO Slug
    /// 
    /// 用途：
    /// - URL 友好名稱，用於優化搜尋引擎排名
    /// - 例如："smartphones" 而非 "智慧型手機"
    /// 
    /// 驗證規則：
    /// - 不為空時，只能包含小寫字母、數字和連字號
    /// - 不為空時，不能以連字號開頭或結尾
    /// - 不為空時，不能有連續的連字號
    /// </summary>
    public string? Slug { get; set; }
    
    /// <summary>
    /// 類目狀態
    /// 
    /// 用途：
    /// - 控制類別是否在前端顯示
    /// - 可用於暫時停用類別而不刪除
    /// 
    /// 可選值：
    /// - "active"：啟用（預設）
    /// - "inactive"：停用
    /// 
    /// 驗證規則：
    /// - 不為空時，只能是 "active" 或 "inactive"
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 類目影像
    /// 
    /// 用途：
    /// - 類別的縮圖或圖示 URL
    /// - 用於前端顯示
    /// 
    /// 驗證規則：
    /// - 不為空時，必須是有效的 URL
    /// - 不為空時，必須是圖片格式（jpg, png, webp 等）
    /// 
    /// 預設值：
    /// - 如果未提供，保持原值不變
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 父類目 ID
    /// 
    /// 用途：
    /// - 用於建立多層級的分類結構
    /// - null 表示這是一個根類別
    /// 
    /// 驗證規則：
    /// - 不為空時，必須對應資料庫中存在的類別
    /// - 不能設為自己的 ID（避免循環）
    /// - 不能設為自己的子類別 ID（避免循環）
    /// 
    /// 注意事項：
    /// - 修改父類別會影響 PathCache 和 PathText
    /// - 這些欄位由資料庫觸發器自動維護
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 是否為最終類目
    /// 
    /// 用途：
    /// - true 表示這個類別沒有子類別
    /// - false 表示這個類別可以包含其他類別
    /// 
    /// 驗證規則：
    /// - 設為 true 時，必須確保沒有子類別
    /// - 設為 false 時，可以新增子類別
    /// 
    /// 注意事項：
    /// - 由資料庫觸發器自動維護
    /// - 新增子類別時會自動設為 false
    /// - 刪除所有子類別時會自動設為 true
    /// </summary>
    public bool? IsLeaf { get; set; }
}

/// <summary>
/// 更新產品類別命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CategoryUpdateCommand 命令
/// - 驗證類別是否存在
/// - 更新類別屬性
/// - 更新類別與屬性鍵的關聯
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoryUpdateCommand> 介面
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
/// 
/// 潛在問題：
/// - 未檢查父類別是否存在
/// - 未檢查是否會造成循環引用
/// - 未檢查屬性鍵是否存在
/// </summary>
public class CategoryUpdateHandler : IRequestHandler<CategoryUpdateCommand>
{
    /// <summary>
    /// 產品類別倉儲介面
    /// 
    /// 用途：
    /// - 存取產品類別資料
    /// - 提供新增、查詢、更新、刪除等操作
    /// - 管理類別與屬性鍵的關聯
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/CategoryRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、UpdateAttributeKeysAsync 等
    /// </summary>
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// 中介者介面
    /// 
    /// 用途：
    /// - 處理命令和事件
    /// - 傳遞資訊到其他處理器
    /// - 支援 CQRS 模式
    /// 
    /// 實作方式：
    /// - 使用 MediatR 框架實作（見 Application/MediatR/MediatorExtensions.cs）
    /// - 提供方法 SendAsync、PublishAsync 等
    /// </summary>
    private readonly IMediator _mediator;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">產品類別倉儲，用於存取資料庫</param>
    /// <param name="mediator">中介者，用於處理命令和事件</param>
    public CategoryUpdateHandler(ICategoryRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理更新產品類別命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢類別實體
    /// 2. 驗證類別是否存在
    /// 3. 更新類別基本屬性
    /// 4. 更新類別與屬性鍵的關聯
    /// 5. 儲存變更
    /// 
    /// 設計考量：
    /// - 支援部分更新（只更新非 null 的屬性）
    /// - 使用條件判斷避免不必要的更新
    /// - 屬性鍵關聯使用集合差異演算法優化效能
    /// 
    /// 錯誤處理：
    /// - 類別不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新父類別會影響 PathCache 和 PathText
    /// - 這些欄位由資料庫觸發器自動維護
    /// - 屬性鍵關聯更新使用獨立方法，保持邏輯清晰
    /// </summary>
    /// <param name="request">更新產品類別命令物件，包含類別的所有可更新屬性</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(CategoryUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢類別實體 ==========
        // 使用 ICategoryRepository.GetByIdAsync() 查詢類別
        // 這個方法會從資料庫中取得完整的類別實體
        var category = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證類別是否存在 ==========
        // 如果找不到類別，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 類別 ID 不存在
        // - 類別已被刪除（軟刪除）
        if(category == null)
            throw Failure.NotFound();
        
        // ========== 第三步：更新類別基本屬性 ==========
        // 使用條件判斷，只更新非 null 的屬性
        // 這種設計支援部分更新，避免覆蓋未提供的屬性
        
        // 更新類別名稱
        if (request.Name != null) category.Name = request.Name;
        
        // 更新 SEO Slug
        if (request.Slug != null) category.Slug = request.Slug;
        
        // 更新類別狀態
        if (request.Status != null) category.Status = request.Status;
        
        // 更新類別影像
        if (request.ImageUrl != null)
        {
            // 如果新的影像 URL 不為 null，則更新類別影像
            if(category.ImageUrl != null)
            {
                await _mediator.SendAsync(new AssetDeleteCommand()
                {
                    Urls = [ category.ImageUrl ] 
                });   
            }
            // 更新類目影像 URL
            await _mediator.SendAsync(new AssetUpdateCommand()
            {
                Urls = [ request.ImageUrl ],
                TargetType = "brand",
                TargetId = category.Id
            });
            // 更新類別影像 URL
            category.ImageUrl = request.ImageUrl;
        }
        
        // 更新是否為葉節點
        if (request.IsLeaf != null) category.IsLeaf = request.IsLeaf.Value;
        
        // 更新父類別 ID
        if (request.ParentId != null) category.ParentId = request.ParentId;

        // ========== 第四步：儲存變更 ==========
        // 使用 ICategoryRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
