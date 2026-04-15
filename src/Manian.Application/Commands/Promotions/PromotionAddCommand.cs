using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 新增促銷活動命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增促銷活動所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Promotion>，表示這是一個會回傳 Promotion 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PromotionAddHandler 配合使用，完成新增促銷活動的業務邏輯
/// 
/// 使用場景：
/// - 管理員建立新的促銷活動
/// - 系統自動建立促銷活動
/// - API 端點接收促銷活動新增請求
/// 
/// 設計特點：
/// - 包含促銷活動基本資訊（名稱、描述、時間等）
/// - 包含促銷活動限制資訊（每人限制、總限制）
/// - 支援可選屬性（如 Channel、UserScope）
/// </summary>
public class PromotionAddCommand : IRequest<Promotion>
{
    /// <summary>
    /// 促銷活動名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的促銷活動名稱
    /// - 用於促銷活動列表和詳細頁面
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-200 字元
    /// 
    /// 範例：
    /// - 「雙11全館88折」
    /// - 「夏季清倉大特賣」
    /// - 「新會員專屬優惠」
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 促銷活動描述
    /// 
    /// 用途：
    /// - 提供促銷活動的詳細說明
    /// - 可用於 SEO 優化
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：10-5000 字元
    /// 
    /// 使用場景：
    /// - 促銷活動詳細頁面
    /// - 搜尋結果摘要
    /// - 行銷推廣文案
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 促銷活動開始時間
    /// 
    /// 用途：
    /// - 定義促銷活動的開始時間
    /// - 在此時間之前，促銷活動不生效
    /// 
    /// 驗證規則：
    /// - 必須早於 EndDate
    /// - 建議使用 UTC 時間
    /// 
    /// 注意事項：
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// - 建議在前端進行時區轉換
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// 促銷活動結束時間
    /// 
    /// 用途：
    /// - 定義促銷活動的結束時間
    /// - 在此時間之後，促銷活動不生效
    /// 
    /// 驗證規則：
    /// - 必須晚於 StartDate
    /// - 建議使用 UTC 時間
    /// 
    /// 注意事項：
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// - 建議在前端進行時區轉換
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// 適用通路
    /// 
    /// 用途：
    /// - 定義促銷活動適用的平台
    /// - 可限制特定通路的使用
    /// 
    /// 可選值：
    /// - "app"：僅限行動版
    /// - "web"：僅限網頁版
    /// - "all"：全平台適用（預設值）
    /// 
    /// 預設值：
    /// - "all"（如果未提供）
    /// 
    /// 使用場景：
    /// - 行動版專屬優惠
    /// - 網頁版專屬優惠
    /// - 全平台通用優惠
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// 適用會員等級
    /// 
    /// 用途：
    /// - 定義促銷活動適用的會員等級
    /// - 可限制特定會員等級的使用
    /// 
    /// 可選值：
    /// - "all"：全體會員（預設值）
    /// - "bronze"：青銅會員
    /// - "silver"：白銀會員
    /// - "gold"：黃金會員
    /// - "vip"：尊榮會員
    /// 
    /// 預設值：
    /// - "all"（如果未提供）
    /// 
    /// 使用場景：
    /// - VIP 專屬優惠
    /// - 新會員專屬優惠
    /// - 高等級會員專屬優惠
    /// </summary>
    public string? UserScope { get; set; }

    /// <summary>
    /// 每人可使用次數
    /// 
    /// 用途：
    /// - 限制每個使用者可以使用此促銷活動的次數
    /// - 防止單一使用者過度使用優惠
    /// 
    /// 預設值：
    /// - null（不限制）
    /// 
    /// 使用場景：
    /// - 限制每人只能使用一次
    /// - 限制每人最多使用三次
    /// 
    /// 注意事項：
    /// - 設定為 null 表示不限制
    /// - 建議在 UI 層顯示剩餘使用次數
    /// </summary>
    public int? LimitPerUser { get; set; }

    /// <summary>
    /// 總可使用次數
    /// 
    /// 用途：
    /// - 限制促銷活動的總使用次數
    /// - 防止優惠被過度使用
    /// 
    /// 預設值：
    /// - null（不限制）
    /// 
    /// 使用場景：
    /// - 限量優惠（如前100名）
    /// - 預算控制（如優惠總額上限）
    /// 
    /// 注意事項：
    /// - 設定為 null 表示不限制
    /// - 建議在 UI 層顯示剩餘使用次數
    /// - 建議在達到上限時自動停用促銷活動
    /// </summary>
    public int? LimitTotal { get; set; }

}


/// <summary>
/// 新增促銷活動命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 PromotionAddCommand 命令
/// - 建立新的 Promotion 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Promotion 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PromotionAddCommand, Promotion> 介面
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
internal class PromotionAddHandler : IRequestHandler<PromotionAddCommand, Promotion>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷活動資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<Promotion>，獲得通用 CRUD 功能
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
    /// 
    /// 設計考量：
    /// - 確保在分散式環境下的唯一性
    /// - 避免使用資料庫自增 ID（不適合分散式環境）
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於新增促銷活動</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生促銷活動 ID</param>
    public PromotionAddHandler(
        IPromotionRepository repository, 
        IUniqueIdentifier uniqueIdentifier)
    {
        _repository = repository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增促銷活動命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 建立新的 Promotion 實體
    /// 2. 設定實體屬性
    /// 3. 將實體加入倉儲
    /// 4. 儲存變更到資料庫
    /// 5. 回傳儲存後的實體
    /// 
    /// 返回值：
    /// - Promotion：儲存後的促銷活動實體，包含自動生成的 ID
    /// 
    /// 注意事項：
    /// - 新增後的實體會包含自動生成的 ID
    /// - 建議在 UI 層顯示新增成功的訊息
    /// </summary>
    /// <param name="request">新增促銷活動命令物件，包含促銷活動的所有資訊</param>
    /// <returns>儲存後的促銷活動實體，包含自動生成的 ID</returns>
    public async Task<Promotion> HandleAsync(PromotionAddCommand request)
    {
        // ========== 第一步：建立新的 Promotion 實體 ==========
        var promotion = new Promotion
        {
            // 產生全域唯一的整數 ID
            // 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            Name = request.Name,
            Description = request.Description,
            
            // 設定時間屬性
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            
            // 設定適用範圍（使用 null-coalescing operator ?? 提供預設值）
            // 如果 request.Channel 為 null，則使用 "all"
            Channel = request.Channel ?? "all",
            
            // 如果 request.UserScope 為 null，則使用 "all"
            UserScope = request.UserScope ?? "all",
            
            // 設定使用限制
            LimitPerUser = request.LimitPerUser,
            LimitTotal = request.LimitTotal
        };

        // ========== 第二步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        // 需要呼叫 SaveChangeAsync 才會實際執行 INSERT SQL
        _repository.Add(promotion);

        // ========== 第三步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 包括新增、修改、刪除的實體
        await _repository.SaveChangeAsync();

        // ========== 第四步：回傳儲存後的實體 ==========
        // 回傳儲存後的 Promotion 實體
        // 包含所有屬性值，包括自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return promotion;
    }
}
