using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 新增促銷規則命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增促銷規則所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<PromotionRule>，表示這是一個會回傳 PromotionRule 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 RuleAddHandler 配合使用，完成新增促銷規則的業務邏輯
/// 
/// 使用場景：
/// - 管理員為促銷活動新增規則
/// - 促銷活動初始化時建立預設規則
/// - API 端點接收促銷規則新增請求
/// 
/// 設計特點：
/// - 包含規則基本資訊（名稱、類型等）
/// - 根據規則類型不同，只會有部分欄位有值
/// - 支援多種規則類型（滿額減、折扣、贈品、免運）
/// 
/// 注意事項：
/// - 規則類型專屬欄位會根據 RuleType 進行驗證
/// - 同一促銷活動的規則類型必須一致（除了滿額減和折扣可以混合）
/// </summary>
public class RuleAddCommand : IRequest<PromotionRule>
{
    /// <summary>
    /// 所屬促銷活動 ID
    /// 
    /// 用途：
    /// - 識別規則所屬的促銷活動
    /// - 建立規則與促銷活動的關聯
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的促銷活動
    /// 
    /// 錯誤處理：
    /// - 如果促銷活動不存在，會拋出 Failure.BadRequest("沒有對應的促銷活動")
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// 規則名稱/標籤
    /// 
    /// 用途：
    /// - 顯示給使用者看的規則名稱
    /// - 用於規則列表和詳細頁面
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-50 字元
    /// 
    /// 範例：
    /// - "滿千送百"
    /// - "全館88折"
    /// - "滿額免運"
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// 規則類型
    /// 
    /// 用途：
    /// - 定義規則的類型
    /// - 決定哪些欄位是必填的
    /// 
    /// 可選值：
    /// - "full_reduction"：滿額減（需要 ThresholdAmount 和 DiscountAmount）
    /// - "discount"：折扣（需要 DiscountRate）
    /// - "gift"：贈品（需要 GiftItemId）
    /// - "free_shipping"：免運（需要 ThresholdAmount）
    /// 
    /// 驗證規則：
    /// - 必須是上述四個值之一
    /// - 同一促銷活動的規則類型必須一致（除了滿額減和折扣可以混合）
    /// 
    /// 錯誤處理：
    /// - 如果規則類型無效，會拋出 ArgumentException
    /// - 如果規則類型與現有規則不一致，會拋出 Failure.BadRequest()
    /// </summary>
    public string RuleType { get; set; }

    /// <summary>
    /// 滿額門檻
    /// 
    /// 用途：
    /// - 定義規則生效的最低金額
    /// - 用於滿額減和免運規則
    /// 
    /// 驗證規則：
    /// - 當 RuleType = "full_reduction" 或 "free_shipping" 時，必須有值且大於 0
    /// - 其他情況可以為 null
    /// 
    /// 範例：
    /// - 1000 表示滿 1000 元
    /// - 500 表示滿 500 元
    /// </summary>
    public decimal? ThresholdAmount { get; set; }

    /// <summary>
    /// 折抵金額（滿減規則專用）
    /// 
    /// 用途：
    /// - 定義滿額減規則的折抵金額
    /// - 只用於 "full_reduction" 規則類型
    /// 
    /// 驗證規則：
    /// - 當 RuleType = "full_reduction" 時，必須有值且大於等於 0
    /// - 其他情況可以為 null
    /// 
    /// 範例：
    /// - 100 表示減 100 元
    /// - 50 表示減 50 元
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// 折扣率（折扣規則專用）
    /// 
    /// 用途：
    /// - 定義折扣規則的折扣率
    /// - 只用於 "discount" 規則類型
    /// 
    /// 驗證規則：
    /// - 當 RuleType = "discount" 時，必須有值且在 0-100 之間
    /// - 其他情況可以為 null
    /// 
    /// 範例：
    /// - 20 表示 20% off（打8折）
    /// - 50 表示 50% off（打5折）
    /// </summary>
    public decimal? DiscountRate { get; set; }

    /// <summary>
    /// 贈品商品 ID
    /// 
    /// 用途：
    /// - 定義贈品規則的贈品商品
    /// - 只用於 "gift" 規則類型
    /// 
    /// 驗證規則：
    /// - 當 RuleType = "gift" 時，必須有值且大於 0
    /// - 其他情況可以為 null
    /// 
    /// 範例：
    /// - 12345 表示商品 ID 為 12345 的商品作為贈品
    /// </summary>
    public long? GiftItemId { get; set; }
}

/// <summary>
/// 新增促銷規則命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 RuleAddCommand 命令
/// - 驗證促銷活動是否存在
/// - 驗證規則類型是否與現有規則一致
/// - 驗證規則類型專屬欄位
/// - 建立新的 PromotionRule 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 PromotionRule 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<RuleAddCommand, PromotionRule> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository 和 IUniqueIdentifier
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - CategoryAddHandler：類似的新增邏輯
/// - SkuAddHandler：類似的驗證和新增邏輯
/// </summary>
internal class RuleAddHandler : IRequestHandler<RuleAddCommand, PromotionRule>
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
    /// - 擴展了 AddRule、GetRulesAsync 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _promotionRepository;

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
    /// <param name="promotionRepository">促銷活動倉儲，用於新增規則</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生規則 ID</param>
    public RuleAddHandler(
        IPromotionRepository promotionRepository,
        IUniqueIdentifier uniqueIdentifier)
    {
        _promotionRepository = promotionRepository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增促銷規則命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證促銷活動是否存在
    /// 2. 查詢促銷活動的現有規則
    /// 3. 驗證規則類型是否與現有規則一致
    /// 4. 驗證規則類型專屬欄位
    /// 5. 建立新的 PromotionRule 實體
    /// 6. 將實體加入倉儲
    /// 7. 儲存變更到資料庫
    /// 8. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 促銷活動不存在：拋出 Failure.BadRequest("沒有對應的促銷活動")
    /// - 規則類型不一致：拋出 Failure.BadRequest("規則類型必須一致")
    /// - 規則類型專屬欄位缺失：拋出 Failure.BadRequest()
    /// 
    /// 返回值：
    /// - PromotionRule：儲存後的促銷規則實體，包含自動生成的 ID
    /// 
    /// 注意事項：
    /// - 同一促銷活動的規則類型必須一致（除了滿額減和折扣可以混合）
    /// - 規則類型專屬欄位會根據 RuleType 進行驗證
    /// - 新增後的實體會包含自動生成的 ID
    /// </summary>
    /// <param name="request">新增促銷規則命令物件，包含規則的所有資訊</param>
    /// <returns>儲存後的促銷規則實體，包含自動生成的 ID</returns>
    public async Task<PromotionRule> HandleAsync(RuleAddCommand request)
    {
        // ========== 第一步：驗證促銷活動是否存在 ==========
        var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId);

        if (promotion == null)
            throw Failure.BadRequest("沒有對應的促銷活動");

        // ========== 第二步：查詢促銷活動的現有規則 ==========
        var rules = await _promotionRepository.GetRulesAsync(request.PromotionId);


        // ========== 第三步：驗證規則類型是否可以共存 ==========
        if (rules != null && rules.Any())
        {
            if(rules.Any(r => r.RuleType == "discount") && request.RuleType == "full_reduction")
            {
                throw Failure.BadRequest("折扣規則和滿額減規則不能共存");
            }

            if(rules.Any(r => r.RuleType == "full_reduction") && request.RuleType == "discount")
            {
                throw Failure.BadRequest("折扣規則和滿額減規則不能共存");
            }
        }
        
        // ========== 第三步：驗證規則類型是否與現有規則一致 ==========
        if (rules != null && rules.Any())
        {
            // 取得現有規則的類型
            var existingRuleType = rules.First().RuleType;
            
            // 規則類型分組：
            // - 金額相關：full_reduction（滿額減）、discount（折扣）
            // - 非金額相關：gift（贈品）、free_shipping（免運）
            // 
            // 規則：
            // - 金額相關的規則可以混合（full_reduction 和 discount 可以共存）
            // - 非金額相關的規則不能與其他規則混合
            // - 金額相關的規則不能與非金額相關的規則混合
            
            var isAmountRelated = existingRuleType == "full_reduction" || existingRuleType == "discount";
            var isAmountRelatedNew = request.RuleType == "full_reduction" || request.RuleType == "discount";
            
            // 如果現有規則是非金額相關的，且新規則類型不同，則拋出錯誤
            if (!isAmountRelated && existingRuleType != request.RuleType)
            {
                throw Failure.BadRequest("規則類型必須一致");
            }
            
            // 如果現有規則是金額相關的，且新規則是非金額相關的，則拋出錯誤
            if (isAmountRelated && !isAmountRelatedNew)
            {
                throw Failure.BadRequest("規則類型必須一致");
            }
        }

        // ========== 第四步：驗證規則類型專屬欄位 ==========
        switch (request.RuleType)
        {
            case "full_reduction":
                // 滿額減規則：需要 ThresholdAmount 和 DiscountAmount
                if (request.ThresholdAmount == null)
                    throw Failure.BadRequest("請輸入滿額金額");

                if (request.DiscountAmount == null)
                    throw Failure.BadRequest("請輸入折抵金額");
                
                if (request.DiscountAmount <= 0)
                    throw Failure.BadRequest("折抵金額必須大於 0");
                
                break;

            case "discount":
                // 折扣規則：需要 DiscountRate
                if (request.DiscountRate == null)
                    throw Failure.BadRequest("請輸入折扣率");
                
                if (request.DiscountRate <= 0 || request.DiscountRate > 100)
                    throw Failure.BadRequest("折扣率必須在 0-100 之間");
                
                break;

            case "gift":
                // 贈品規則：需要 GiftItemId
                if (request.GiftItemId == null)
                    throw Failure.BadRequest("請輸入贈品ID");
                
                if (request.GiftItemId <= 0)
                    throw Failure.BadRequest("贈品ID必須大於 0");
                
                break;

            case "free_shipping":
                // 免運規則：需要 ThresholdAmount
                if (request.ThresholdAmount == null)
                    throw Failure.BadRequest("請輸入滿額金額");
                
                if (request.ThresholdAmount <= 0)
                    throw Failure.BadRequest("滿額金額必須大於 0");
                
                break;

            default:
                // 無效的規則類型
                throw Failure.BadRequest("無效的規則類型");
        }

        // ========== 第五步：建立新的 PromotionRule 實體 ==========
        var rule = new PromotionRule
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            PromotionId = request.PromotionId,
            TabName = request.TabName,
            RuleType = request.RuleType,
            
            // 設定規則類型專屬欄位
            ThresholdAmount = request.ThresholdAmount,
            DiscountAmount = request.DiscountAmount,
            DiscountRate = request.DiscountRate,
            GiftItemId = (int?)request.GiftItemId,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第六步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        // 需要呼叫 SaveChangeAsync 才會實際執行 INSERT SQL
        _promotionRepository.AddRule(rule);

        // ========== 第七步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 包括新增、修改、刪除的實體
        await _promotionRepository.SaveChangeAsync();

        // ========== 第八步：回傳儲存後的實體 ==========
        // 回傳儲存後的 PromotionRule 實體
        // 包含所有屬性值，包括自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return rule;
    }
}
