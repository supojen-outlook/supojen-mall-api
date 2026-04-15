using System;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 更新促銷規則命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新促銷規則所需的資訊
/// 設計模式：實作 IRequest<PromotionRule>，表示這是一個會回傳更新後實體的命令
/// 
/// 使用場景：
/// - 管理員修改促銷規則資訊
/// - 促銷規則資料維護
/// - 促銷規則調整
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 只更新非 null 的欄位，保持 null 欄位的原值不變
/// - 自動驗證規則類型專屬欄位
/// 
/// 注意事項：
/// - 更新規則可能會影響已關聯的訂單
/// - 建議在更新前檢查是否有訂單使用此規則
/// </summary>
public class RuleUpdateCommand : IRequest<PromotionRule>
{
    /// <summary>
    /// 規則唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的規則
    /// - 必須是資料庫中已存在的規則 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的規則
    /// 
    /// 錯誤處理：
    /// - 如果規則不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// 規則名稱/標籤
    /// 
    /// 用途：
    /// - 顯示給使用者看的規則名稱
    /// - 用於規則列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用範例：
    /// - "滿千送百"
    /// - "雙11折扣"
    /// </summary>
    public string? TabName { get; set; }

    /// <summary>
    /// 滿額門檻
    /// 
    /// 用途：
    /// - 設定滿額減或免運的門檻金額
    /// - 範例：1000 表示滿 1000 元
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 適用規則類型：
    /// - full_reduction（滿額減）
    /// - free_shipping（免運）
    /// </summary>
    public decimal? ThresholdAmount { get; set; }

    /// <summary>
    /// 折抵金額（滿減規則專用）
    /// 
    /// 用途：
    /// - 設定滿額減的折抵金額
    /// - 範例：100 表示折抵 100 元
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 適用規則類型：
    /// - full_reduction（滿額減）
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// 折扣率（折扣規則專用）
    /// 
    /// 用途：
    /// - 設定折扣的折扣率
    /// - 範例：20 表示 20% off
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 適用規則類型：
    /// - discount（折扣）
    /// 
    /// 驗證規則：
    /// - 必須在 0-100 之間
    /// </summary>
    public decimal? DiscountRate { get; set; }
}

/// <summary>
/// 更新促銷規則命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 RuleUpdateCommand 命令
/// - 查詢規則是否存在
/// - 驗證規則類型專屬欄位
/// - 更新規則資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<RuleUpdateCommand, PromotionRule> 介面
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
/// - 未檢查規則是否有關聯的訂單
/// - 未檢查規則類型是否可以共存（應在 RuleAddCommand 中處理）
/// - 建議考慮使用樂觀鎖（Optimistic Concurrency）防止並發更新衝突
/// </summary>
internal class RuleUpdateHandler : IRequestHandler<RuleUpdateCommand, PromotionRule>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷規則資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 擴展了 GetRuleAsync、AddRule、DeleteRule 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢和更新規則</param>
    public RuleUpdateHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新促銷規則命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢規則實體
    /// 2. 驗證規則是否存在
    /// 3. 驗證規則類型專屬欄位
    /// 4. 更新規則屬性
    /// 5. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 規則不存在：拋出 Failure.NotFound()
    /// - 規則類型專屬欄位驗證失敗：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否有訂單使用此規則
    /// - 不會檢查規則類型是否可以共存（應在 RuleAddCommand 中處理）
    /// </summary>
    /// <param name="request">更新促銷規則命令物件，包含規則 ID 和要更新的欄位</param>
    /// <returns>更新後的 PromotionRule 實體</returns>
    public async Task<PromotionRule> HandleAsync(RuleUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢規則實體 ==========
        // 使用 IPromotionRepository.GetRuleAsync() 查詢規則
        // 這個方法會從資料庫中取得完整的規則實體
        var rule = await _repository.GetRuleAsync(request.RuleId);
        
        // ========== 第二步：驗證規則是否存在 ==========
        // 如果找不到規則，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 規則 ID 不存在
        // - 規則已被刪除
        if (rule == null)
            throw Failure.NotFound($"規則不存在，ID: {request.RuleId}");

        // ========== 第三步：驗證規則類型專屬欄位 ==========
        // 根據規則類型驗證專屬欄位
        switch (rule.RuleType)
        {
            case "full_reduction":
                // 滿額減規則：需要 ThresholdAmount 和 DiscountAmount
                if (request.ThresholdAmount.HasValue && request.ThresholdAmount.Value <= 0)
                    throw Failure.BadRequest("滿額金額必須大於 0");
                
                if (request.DiscountAmount.HasValue && request.DiscountAmount.Value <= 0)
                    throw Failure.BadRequest("折抵金額必須大於 0");
                
                break;

            case "discount":
                // 折扣規則：需要 DiscountRate
                if (request.DiscountRate.HasValue && (request.DiscountRate.Value <= 0 || request.DiscountRate.Value > 100))
                    throw Failure.BadRequest("折扣率必須在 0-100 之間");
                
                break;

            case "gift":
                // 贈品規則：不需要驗證（因為 RuleUpdateCommand 沒有 GiftItemId 欄位）
                // 如果需要更新贈品，應該刪除舊規則並新增新規則
                break;

            case "free_shipping":
                // 免運規則：需要 ThresholdAmount
                if (request.ThresholdAmount.HasValue && request.ThresholdAmount.Value <= 0)
                    throw Failure.BadRequest("滿額金額必須大於 0");
                
                break;

            default:
                // 無效的規則類型
                throw Failure.BadRequest("無效的規則類型");
        }

        // ========== 第四步：更新規則屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新基本屬性
        if (request.TabName != null) rule.TabName = request.TabName;
        
        // 更新規則類型專屬欄位
        if (request.ThresholdAmount.HasValue) rule.ThresholdAmount = request.ThresholdAmount;
        if (request.DiscountAmount.HasValue) rule.DiscountAmount = request.DiscountAmount;
        if (request.DiscountRate.HasValue) rule.DiscountRate = request.DiscountRate;

        // ========== 第五步：儲存變更 ==========
        // 使用 IPromotionRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第六步：回傳更新後的實體 ==========
        // 回傳更新後的 PromotionRule 實體
        // 包含所有更新後的屬性值
        return rule;
    }
}
