using Manian.Application.Commands.Assets;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增品牌命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增品牌所需的資訊
/// 設計模式：實作 IRequest<Brand>，表示這是一個會回傳 Brand 實體的命令
/// 
/// 使用場景：
/// - 管理員建立新品牌
/// - 系統初始化時建立預設品牌
/// - 匯入品牌資料
/// </summary>
public class BrandAddCommand : IRequest<Brand>
{
    /// <summary>
    /// 品牌名稱
    /// 
    /// 驗證規則：
    /// - 必填欄位（使用 required 關鍵字）
    /// - 不應為空白或僅包含空白字元
    /// - 建議長度限制：2-100 字元
    /// 
    /// 使用範例：
    /// - "Nike"
    /// - "Apple"
    /// - "Sony"
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// 品牌狀態
    /// 
    /// 可選值：
    /// - "active"：啟用狀態，品牌可被使用
    /// - "inactive"：停用狀態，品牌不可被使用
    /// 
    /// 預設值：
    /// - 若未提供，預設為 "active"
    /// 
    /// 使用場景：
    /// - 暫時停用某個品牌（而非刪除）
    /// - 預先建立品牌但尚未啟用
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Slug（URL 友好名稱）
    /// 
    /// 用途：
    /// - 用於產生 SEO 友好的 URL
    /// - 例如：/brands/nike 而非 /brands/123
    /// 
    /// 產生規則：
    /// - 若未提供，自動從 Name 產生
    /// - 轉為小寫並將空格替換為連字號
    /// - 範例："Nike Air" → "nike-air"
    /// 
    /// 注意事項：
    /// - 應確保在整個系統中唯一
    /// - 只能包含小寫字母、數字和連字號
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// 排序順序
    /// 
    /// 用途：
    /// - 控制品牌列表的顯示順序
    /// - 數字越小越前面
    /// 
    /// 預設值：
    /// - 若未提供，預設為 0
    /// 
    /// 使用場景：
    /// - 將熱門品牌排在前面
    /// - 自定義品牌分類順序
    /// </summary>
    public int? SortOrder { get; set; }
    
    /// <summary>
    /// 上層品牌 ID
    /// 
    /// 用途：
    /// - 建立品牌層級結構（父子關係）
    /// - NULL 表示這是一個根品牌
    /// 
    /// 使用場景：
    /// - 建立品牌分類（如：Nike > 服飾 > 運動鞋）
    /// - 多層級品牌管理
    /// 
    /// 注意事項：
    /// - 使用 init 關鍵字，只能在物件初始化時設定
    /// - 父品牌必須已存在
    /// </summary>
    public int? ParentId { get; init; }
    
    /// <summary>
    /// 品牌描述
    /// 
    /// 用途：
    /// - 提供品牌的詳細資訊
    /// - 可用於 SEO 描述
    /// 
    /// 預設值：
    /// - 若未提供，預設為空字串
    /// 
    /// 使用場景：
    /// - 品牌介紹頁面
    /// - 搜尋結果摘要
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 品牌圖片 URL
    /// 
    /// 用途：
    /// - 顯示品牌標誌或代表圖片
    /// - 用於品牌列表和詳細頁面
    /// 
    /// 預設值：
    /// - 若未提供，使用預設圖片：
    ///   https://demo-po.sgp1.cdn.digitaloceanspaces.com/brand_d.png
    /// 
    /// 注意事項：
    /// - 應使用 HTTPS 協議
    /// - 建議使用 CDN 加速存取
    /// - 圖片尺寸建議：200x200 px
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// 是否為葉子節點
    /// 
    /// 用途：
    /// - 控制品牌是否可以有子品牌
    /// - true：不能有子品牌
    /// - false：可以有子品牌
    /// 
    /// 使用場景：
    /// - 終端品牌（如具體產品線）設為 true
    /// - 分類品牌（如品牌類別）設為 false
    /// 
    /// 注意事項：
    /// - 由資料庫觸發器自動維護
    /// - 當新增子品牌時，父品牌的 IsLeaf 會自動設為 false
    /// </summary>
    public bool IsLeaf { get; set; }
}

/// <summary>
/// 新增品牌命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 BrandAddCommand 命令
/// - 建立新的 Brand 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Brand 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<BrandAddCommand, Brand> 介面
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
public class BrandAddHandler : IRequestHandler<BrandAddCommand, Brand>
{
    /// <summary>
    /// 品牌倉儲介面
    /// 
    /// 用途：
    /// - 存取品牌資料
    /// - 提供新增、查詢、更新、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/BrandRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// </summary>
    private readonly IBrandRepository _repository;

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
    /// <param name="repository">品牌倉儲，用於存取資料庫</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生品牌 ID</param>
    /// <<param name="mediator">中介者服務，用於傳遞命令和事件</param>
    public BrandAddHandler(
        IBrandRepository repository, 
        IUniqueIdentifier uniqueIdentifier,
        IMediator mediator)
    {
        _repository = repository;
        _uniqueIdentifier = uniqueIdentifier;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理新增品牌命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 建立新的 Brand 實體
    /// 2. 設定實體屬性
    /// 3. 將實體加入倉儲
    /// 4. 儲存變更到資料庫
    /// 5. 重新查詢實體以確保資料一致性
    /// 6. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 儲存後查詢不到實體：拋出 Failure.BadRequest("新增品牌失敗")
    /// </summary>
    /// <param name="request">新增品牌命令物件，包含品牌的所有資訊</param>
    /// <returns>儲存後的 Brand 實體，包含資料庫自動生成的欄位</returns>
    public async Task<Brand> HandleAsync(BrandAddCommand request)
    {
        // ========== 第一步：建立新的 Brand 實體 ==========
        var brand = new Brand
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            Name = request.Name,
            
            // Slug：若未提供則自動產生
            // 轉為小寫並將空格替換為連字號
            Slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-"),
            
            // LogoUrl：若未提供則使用預設圖片
            LogoUrl = request.LogoUrl ?? "https://demo-po.sgp1.cdn.digitaloceanspaces.com/brand_d.png",
            
            // Description：若未提供則為空字串
            Description = request.Description ?? "",
            
            // Status：若未提供則預設為 "active"
            Status = request.Status ?? "active",
            
            // SortOrder：若未提供則預設為 0
            SortOrder = request.SortOrder ?? 0,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow,
            
            // 設定父品牌 ID
            ParentId = request.ParentId,
            
            // 設定是否為葉節點
            IsLeaf = request.IsLeaf
        };

        // ========== 第二步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _repository.Add(brand);
        
        // ========== 第三步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        await _repository.SaveChangeAsync();

        // ========== 第四步：更新資產庫 ==========
        // 將LOGO圖片更新到資產庫
        if(request.LogoUrl != null)
        {
            await _mediator.SendAsync(new AssetUpdateCommand()
            {
                Urls = [ request.LogoUrl ],
                TargetType = "brand",
                TargetId = brand.Id
            });   
        }

        // ========== 第五步：重新查詢實體 ==========
        // 為什麼要重新查詢？
        // 1. 確保實體包含資料庫觸發器自動更新的欄位（如 Level、PathCache、PathText）
        // 2. 確保實體狀態與資料庫一致
        // 3. 避免快取問題
        brand = await _repository.GetByIdAsync(brand.Id);
        
        // ========== 第六步：驗證實體是否存在 ==========
        // 如果查詢不到實體，表示儲存失敗
        if(brand == null) 
            throw Failure.BadRequest("新增品牌失敗");

        // ========== 第七步：回傳儲存後的實體 ==========
        return brand;
    }
}
