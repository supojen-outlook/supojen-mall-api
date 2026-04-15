using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 更新促銷活動命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新促銷活動所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員修改促銷活動資訊
/// - 促銷活動資料維護
/// - 促銷活動調整
/// 
/// 設計特點：
/// - 所有屬性皆為可空（nullable），支援部分更新
/// - 未提供的欄位保持原值不變
/// - 遵循 HTTP PATCH 語意（部分更新）
/// 
/// 注意事項：
/// - 更新促銷活動可能會影響已關聯的規則
/// - 建議在更新前檢查是否有規則使用此促銷活動
/// - 更新時間範圍可能會影響已生效的優惠
/// </summary>
public class PromotionUpdateCommand : IRequest
{
    /// <summary>
    /// 促銷活動唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的促銷活動
    /// - 必須是資料庫中已存在的促銷活動 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的促銷活動
    /// 
    /// 錯誤處理：
    /// - 如果促銷活動不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 促銷活動名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的促銷活動名稱
    /// - 用於促銷活動列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議長度限制：1-200 字元
    /// - 不應為空白或僅包含空白字元
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 促銷活動描述
    /// 
    /// 用途：
    /// - 提供促銷活動的詳細說明
    /// - 可用於 SEO 優化
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 必須早於 EndDate
    /// - 建議使用 UTC 時間
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 促銷活動結束時間
    /// 
    /// 用途：
    /// - 定義促銷活動的結束時間
    /// - 在此時間之後，促銷活動不生效
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 必須晚於 StartDate
    /// - 建議使用 UTC 時間
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// </summary>
    public DateTime? EndDate { get; set; }

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
    /// - "all"：全平台適用
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// - "all"：全體會員
    /// - "bronze"：青銅會員
    /// - "silver"：白銀會員
    /// - "gold"：黃金會員
    /// - "vip"：尊榮會員
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
/// 更新促銷活動命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 PromotionUpdateCommand 命令
/// - 查詢促銷活動是否存在
/// - 更新促銷活動資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PromotionUpdateCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查 Channel 是否為有效值（app/web/all）
/// - 未檢查 UserScope 是否為有效值（all/bronze/silver/gold/vip）
/// - 未檢查 StartDate 是否早於 EndDate
/// - 建議在實際專案中加入這些檢查
/// 
/// 參考實作：
/// - BrandUpdateHandler：類似的更新邏輯
/// - CategoryUpdateHandler：類似的更新邏輯
/// </summary>
internal class PromotionUpdateHandler : IRequestHandler<PromotionUpdateCommand>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷活動資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 繼承自 Repository<Promotion>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢和更新促銷活動</param>
    public PromotionUpdateHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新促銷活動命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢促銷活動實體
    /// 2. 驗證促銷活動是否存在
    /// 3. 更新促銷活動屬性（只更新非 null 的欄位）
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 促銷活動不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否有規則使用此促銷活動
    /// - 建議檢查更新時間範圍是否會影響已生效的優惠
    /// </summary>
    /// <param name="request">更新促銷活動命令物件，包含促銷活動 ID 和要更新的欄位</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(PromotionUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢促銷活動實體 ==========
        // 使用 IPromotionRepository.GetByIdAsync() 查詢促銷活動
        // 這個方法會從資料庫中取得完整的促銷活動實體
        var promotion = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證促銷活動是否存在 ==========
        // 如果找不到促銷活動，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 促銷活動 ID 不存在
        // - 促銷活動已被刪除（軟刪除）
        if (promotion == null)
            throw Failure.NotFound($"促銷活動不存在，ID: {request.Id}");

        // ========== 第三步：更新促銷活動屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新基本屬性
        if (request.Name != null) promotion.Name = request.Name;
        if (request.Description != null) promotion.Description = request.Description;
        
        // 更新時間屬性
        if (request.StartDate != null) promotion.StartDate = request.StartDate.Value;
        if (request.EndDate != null) promotion.EndDate = request.EndDate.Value;
        
        // 更新適用範圍
        if (request.Channel != null) promotion.Channel = request.Channel;
        if (request.UserScope != null) promotion.UserScope = request.UserScope;
        
        // 更新使用限制
        if (request.LimitPerUser != null) promotion.LimitPerUser = request.LimitPerUser;
        if (request.LimitTotal != null) promotion.LimitTotal = request.LimitTotal;

        // ========== 第四步：儲存變更 ==========
        // 使用 IPromotionRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
