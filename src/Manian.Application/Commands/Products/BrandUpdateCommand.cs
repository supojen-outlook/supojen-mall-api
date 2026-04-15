using Manian.Application.Commands.Assets;
using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 更新品牌命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新品牌所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員修改品牌資訊
/// - 品牌資料維護
/// - 品牌結構調整
/// 
/// 設計特點：
/// - 所有屬性皆為可空（nullable），支援部分更新
/// - 未提供的欄位保持原值不變
/// - 遵循 HTTP PATCH 語意（部分更新）
/// </summary>
public class BrandUpdateCommand : IRequest
{
    /// <summary>
    /// 品牌唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的品牌
    /// - 必須是資料庫中已存在的品牌 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的品牌
    /// 
    /// 錯誤處理：
    /// - 如果品牌不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 品牌顯示名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的品牌名稱
    /// - 用於品牌列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議長度限制：2-100 字元
    /// - 不應為空白或僅包含空白字元
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// 品牌詳細描述
    /// 
    /// 用途：
    /// - 提供品牌的詳細資訊
    /// - 可用於 SEO 描述
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 品牌介紹頁面
    /// - 搜尋結果摘要
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 品牌標誌圖片 URL
    /// 
    /// 用途：
    /// - 顯示品牌標誌或代表圖片
    /// - 用於品牌列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 應使用 HTTPS 協議
    /// - 建議使用 CDN 加速存取
    /// - 圖片尺寸建議：200x200 px
    /// </summary>
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// 品牌狀態
    /// 
    /// 可選值：
    /// - "active"：啟用狀態，品牌可被使用
    /// - "inactive"：停用狀態，品牌不可被使用
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 暫時停用某個品牌（而非刪除）
    /// - 預先建立品牌但尚未啟用
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Slug（URL 友好名稱）
    /// 
    /// 用途：
    /// - 用於產生 SEO 友好的 URL
    /// - 例如：/brands/nike 而非 /brands/123
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 應確保在整個系統中唯一
    /// - 只能包含小寫字母、數字和連字號
    /// </summary>
    public string? Slug { get; set; }
    
    /// <summary>
    /// 上層品牌 ID
    /// 
    /// 用途：
    /// - 建立品牌層級結構（父子關係）
    /// - NULL 表示這是一個根品牌
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 建立品牌分類（如：Nike > 服飾 > 運動鞋）
    /// - 多層級品牌管理
    /// 
    /// 注意事項：
    /// - 父品牌必須已存在
    /// - 更新後會影響品牌的層級結構
    /// </summary>
    public int? ParentId { get; set; }
    
    /// <summary>
    /// 是否為葉子節點
    /// 
    /// 用途：
    /// - 控制品牌是否可以有子品牌
    /// - true：不能有子品牌
    /// - false：可以有子品牌
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 終端品牌（如具體產品線）設為 true
    /// - 分類品牌（如品牌類別）設為 false
    /// 
    /// 注意事項：
    /// - 由資料庫觸發器自動維護
    /// - 當新增子品牌時，父品牌的 IsLeaf 會自動設為 false
    /// </summary>
    public bool? IsLeaf { get; set; }

    /// <summary>
    /// 排序順序
    /// 
    /// 用途：
    /// - 用於品牌列表的排序
    /// - 較小的值會排在前面
    ///     
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 自訂品牌顯示順序
    /// - 按照熱門程度排序
    /// </summary>
    public int? SortOrder { get; set; }
}

/// <summary>
/// 更新品牌命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 BrandUpdateCommand 命令
/// - 查詢品牌是否存在
/// - 更新品牌資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<BrandUpdateCommand> 介面
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
/// 潛在問題：
/// - 未檢查 Status 是否為有效值（active/inactive）
/// - 未檢查 Slug 是否唯一
/// - 未檢查 ParentId 是否存在
/// - 未檢查是否會造成循環引用
/// </summary>
public class BrandUpdateHandler : IRequestHandler<BrandUpdateCommand>
{
    /// <summary>
    /// 品牌倉儲介面
    /// 
    /// 用途：
    /// - 存取品牌資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/BrandRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// </summary>
    private readonly IBrandRepository _brandRepository;

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
    /// <param name="brandRepository">品牌倉儲，用於查詢和更新品牌</param>
    /// <param name="mediator">中介者，用於處理命令和事件</param>
    public BrandUpdateHandler(IBrandRepository brandRepository, IMediator mediator)
    {
        _brandRepository = brandRepository;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理更新品牌命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢品牌實體
    /// 2. 驗證品牌是否存在
    /// 3. 更新品牌屬性（只更新非 null 的欄位）
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 品牌不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否有子品牌或產品使用此品牌
    /// - 建議檢查是否會造成循環引用
    /// </summary>
    /// <param name="request">更新品牌命令物件，包含品牌 ID 和要更新的欄位</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(BrandUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢品牌實體 ==========
        // 使用 IBrandRepository.GetByIdAsync() 查詢品牌
        // 這個方法會從資料庫中取得完整的品牌實體
        var brand = await _brandRepository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證品牌是否存在 ==========
        // 如果找不到品牌，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 品牌 ID 不存在
        // - 品牌已被刪除（軟刪除）
        if (brand == null)
            throw Failure.NotFound($"品牌不存在，ID: {request.Id}");

        // ========== 第三步：更新品牌屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新基本屬性
        if (request.Name != null) brand.Name = request.Name;
        if (request.LogoUrl != null)
        {
            // 如果新的影像 URL 不為 null，則更新類別影像
            if(brand.LogoUrl != null)
            {
                await _mediator.SendAsync(new AssetDeleteCommand()
                {
                    Urls = [ brand.LogoUrl ]
                });   
            }
            // 更新品牌影像 URL
            await _mediator.SendAsync(new AssetUpdateCommand()
            {
                Urls = [ request.LogoUrl ],
                TargetType = "brand",
                TargetId = brand.Id
            });
            // 更新類別影像 URL
            brand.LogoUrl = request.LogoUrl;
        }
        if (request.Status != null) brand.Status = request.Status;
        if (request.Slug != null) brand.Slug = request.Slug;
        if (request.SortOrder != null) brand.SortOrder = request.SortOrder.Value;
        
        // 更新層級結構相關屬性
        if (request.ParentId != null) brand.ParentId = request.ParentId;
        if (request.IsLeaf != null) brand.IsLeaf = request.IsLeaf.Value; 

        // ========== 第四步：儲存變更 ==========
        // 使用 IBrandRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _brandRepository.SaveChangeAsync();
    }
}
