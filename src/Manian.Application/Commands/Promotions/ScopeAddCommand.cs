using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 新增促銷範圍命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增促銷範圍所需的資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<PromotionScope>，表示這是一個會回傳 PromotionScope 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ScopeAddHandler 配合使用，完成新增促銷範圍的業務邏輯
/// 
/// 使用場景：
/// - 管理員為促銷活動新增適用範圍
/// - 促銷活動初始化時建立預設範圍
/// - API 端點接收促銷範圍新增請求
/// 
/// 設計特點：
/// - 包含促銷範圍基本資訊（類型、範圍 ID、是否排除）
/// - 支援多種範圍類型（商品、類別、品牌、全館）
/// - 支援排除特定範圍的功能
/// 
/// 注意事項：
/// - 當 ScopeType = 'all' 時，ScopeId 必須為 0
/// - 當 ScopeType != 'all' 時，ScopeId 必須為正整數
/// </summary>
public class ScopeAddCommand : IRequest<PromotionScope>
{
    /// <summary>
    /// 促銷活動 ID
    /// 
    /// 用途：
    /// - 識別要新增範圍的促銷活動
    /// - 建立促銷活動與範圍的關聯
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的促銷活動
    /// 
    /// 錯誤處理：
    /// - 如果促銷活動不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// 範圍類型
    /// 
    /// 用途：
    /// - 指定促銷活動適用的範圍類型
    /// - 決定 ScopeId 對應的實體類型
    /// 
    /// 可選值：
    /// - "product"：商品範圍
    /// - "category"：類別範圍
    /// - "brand"：品牌範圍
    /// - "all"：全館範圍
    /// 
    /// 驗證規則：
    /// - 只能接受 "product"、"category"、"brand" 或 "all" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 特殊規則：
    /// - 當 ScopeType = 'all' 時，ScopeId 必須為 0
    /// </summary>
    public string ScopeType { get; set; }

    /// <summary>
    /// 範圍 ID
    /// 
    /// 用途：
    /// - 根據 ScopeType 對應到不同表的 ID
    /// - ScopeType = 'product'：商品 ID
    /// - ScopeType = 'category'：類別 ID
    /// - ScopeType = 'brand'：品牌 ID
    /// - ScopeType = 'all'：固定為 0
    /// 
    /// 驗證規則：
    /// - 當 ScopeType = 'all' 時，必須為 0
    /// - 其他情況必須為正整數
    /// </summary>
    public int ScopeId { get; set; }

    /// <summary>
    /// 是否排除
    /// 
    /// 用途：
    /// - 標識是否排除該範圍
    /// - 用於實現「全館優惠，排除特定商品」等場景
    /// 
    /// 預設值：
    /// - false（包含該範圍）
    /// 
    /// 使用場景：
    /// - 全館優惠，排除特定商品（ScopeType = 'all', IsExclude = true）
    /// - 類別優惠，排除特定商品（ScopeType = 'category', IsExclude = true）
    /// </summary>
    public bool IsExclude { get; set; }
}

/// <summary>
/// 新增促銷範圍命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ScopeAddCommand 命令
/// - 建立新的 PromotionScope 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 PromotionScope 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ScopeAddCommand, PromotionScope> 介面
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
/// - RuleAddHandler：類似的新增邏輯
/// - AttributeValueAddHandler：類似的新增邏輯
/// </summary>
internal class ScopeAddHandler : IRequestHandler<ScopeAddCommand, PromotionScope>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷活動和範圍資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 擴展了 AddScope、GetScopesAsync 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _repository;

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
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於新增促銷範圍</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生促銷範圍 ID</param>
    public ScopeAddHandler(
        IPromotionRepository repository,
        IUniqueIdentifier uniqueIdentifier)
    {
        _repository = repository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增促銷範圍命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證促銷活動是否存在
    /// 2. 驗證範圍類型和範圍 ID 的組合是否有效
    /// 3. 建立新的 PromotionScope 實體
    /// 4. 將實體加入倉儲
    /// 5. 儲存變更到資料庫
    /// 6. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 促銷活動不存在：拋出 Failure.NotFound()
    /// - 無效的範圍類型：拋出 ArgumentException
    /// - 無效的範圍 ID：拋出 ArgumentException
    /// 
    /// 注意事項：
    /// - 新增後的實體會包含自動生成的 ID
    /// - 建議在 UI 層顯示新增成功的訊息
    /// </summary>
    /// <param name="request">新增促銷範圍命令物件，包含促銷範圍的所有資訊</param>
    /// <returns>儲存後的促銷範圍實體，包含自動生成的 ID</returns>
    public async Task<PromotionScope> HandleAsync(ScopeAddCommand request)
    {
        // ========== 第一步：驗證促銷活動是否存在 ==========
        var promotion = await _repository.GetByIdAsync(request.PromotionId);
        if (promotion == null)
            throw Failure.NotFound($"促銷活動不存在，ID: {request.PromotionId}");

        // ========== 第二步：驗證範圍類型和範圍 ID 的組合是否有效 ==========
        // 當 ScopeType = 'all' 時，ScopeId 必須為 0
        if (request.ScopeType == "all" && request.ScopeId != 0)
            throw Failure.BadRequest("當 ScopeType 為 'all' 時，ScopeId 必須為 0");
        
        // 當 ScopeType != 'all' 時，ScopeId 必須為正整數
        if (request.ScopeType != "all" && request.ScopeId <= 0)
            throw Failure.BadRequest("ScopeId 必須為正整數");

        // ========== 第三步：建立新的 PromotionScope 實體 ==========
        var scope = new PromotionScope
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            PromotionId = request.PromotionId,
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            IsExclude = request.IsExclude,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第四步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        // 需要呼叫 SaveChangeAsync 才會實際執行 INSERT SQL
        _repository.AddScope(scope);

        // ========== 第五步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 包括新增、修改、刪除的實體
        await _repository.SaveChangeAsync();

        // ========== 第六步：回傳儲存後的實體 ==========
        // 回傳儲存後的 PromotionScope 實體
        // 包含所有屬性值，包括自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return scope;
    }
}
