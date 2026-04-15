using Manian.Domain.Repositories.Carts;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Manian.Application.Services;
using Shared.Mediator.Interface;
using Manian.Domain.ValueObjects;

namespace Manian.Application.Queries.Promotions;

/// <summary>
/// 購物車折扣查詢 (CQRS 模式中的 Query)
/// 
/// 用途：
/// - 查詢當前使用者購物車可用的最佳促銷折扣
/// - 判斷適用的促銷規則（滿額減、折扣、贈品、免運）
/// - 提供前端展示折扣資訊的資料來源
/// 
/// 設計模式：
/// - 實作 IRequest<Discount?>，表示這是一個查詢請求
/// - 回傳可空的 Discount 物件（可能沒有折扣）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 DiscountHandler 配合使用，完成查詢邏輯
/// 
/// 使用場景：
/// - 購物車頁面顯示折扣資訊
/// - 結帳頁面計算最終金額
/// - 促銷活動效果追蹤
/// 
/// 查詢結果：
/// - 成功：回傳折扣力度最大的 Discount 物件
/// - 無折扣：回傳 null
/// 
/// 注意事項：
/// - 此查詢不會修改任何資料
/// - 折扣計算由 PromotionCalculationService 負責
/// - 只回傳最佳折扣，不回傳所有折扣
/// </summary>
public class DiscountQuery : IRequest<Discount?>;

/// <summary>
/// 購物車折扣查詢處理器
/// 
/// 職責：
/// - 接收 DiscountQuery 請求
/// - 查詢當前使用者的購物車項目
/// - 查詢所有有效的促銷活動
/// - 委託 PromotionCalculationService 計算折扣
/// - 回傳折扣力度最大的折扣
/// 
/// 設計模式：
/// - 實作 IRequestHandler<DiscountQuery, Discount?> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// - 遵循依賴倒置原則 (DIP)，依賴抽象而非實作
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// 
/// 效能考量：
/// - 一次性查詢所有購物車項目
/// - 一次性查詢所有有效促銷活動
/// - 折扣計算在記憶體中完成，避免多次資料庫查詢
/// </summary>
internal class DiscountHandler : IRequestHandler<DiscountQuery, Discount?>
{
    /// <summary>
    /// 購物車項目倉儲介面
    /// 
    /// 用途：
    /// - 查詢當前使用者的購物車項目
    /// - 取得購物車中的商品資訊
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 提供泛型方法 GetAllAsync 查詢實體集合
    /// 
    /// 查詢條件：
    /// - UserId：當前登入使用者的 ID
    /// - CartType：固定為 "shopping"（購物車類型）
    /// </summary>
    private readonly ICartItemRepository _cartRepository;

    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 查詢所有有效的促銷活動
    /// - 取得促銷活動的規則和範圍資訊
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 GetAllAsync 查詢實體集合
    /// 
    /// 查詢條件：
    /// - Status：必須為 "active"（啟用狀態）
    /// - StartDate：必須小於等於當前時間
    /// - EndDate：必須大於等於當前時間
    /// </summary>
    private readonly IPromotionRepository _promotionRepository;

    /// <summary>
    /// 促銷計算服務
    /// 
    /// 用途：
    /// - 計算購物車項目的促銷折扣
    /// - 判斷適用的促銷規則
    /// - 處理多個促銷活動的優先級
    /// 
    /// 實作方式：
    /// - 見 Domain/Services/PromotionCalculationService.cs
    /// - 提供方法 CalculateDiscountsAsync 計算折扣
    /// 
    /// 計算邏輯：
    /// - 按促銷活動遍歷計算折扣
    /// - 按規則類型分組取最大折扣
    /// - 最終按折扣金額降序排列
    /// </summary>
    private readonly PromotionCalculationService _calculationService;

    /// <summary>
    /// 使用者聲明服務
    /// 
    /// 用途：
    /// - 取得當前登入使用者的 ID
    /// - 取得使用者的其他聲明資訊
    /// 
    /// 實作方式：
    /// - 見 Application/Services/UserClaim.cs
    /// - 從 JWT Token 中解析使用者資訊
    /// 
    /// 使用場景：
    /// - 查詢當前使用者的購物車
    /// - 驗證使用者權限
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// 
    /// 設計原則：
    /// - 使用建構函式注入 (Constructor Injection)
    /// - 所有依賴都是介面，符合依賴倒置原則
    /// - 所有依賴都是 readonly，確保不可變性
    /// </summary>
    /// <param name="cartRepository">購物車項目倉儲</param>
    /// <param name="promotionRepository">促銷活動倉儲</param>
    /// <param name="calculationService">促銷計算服務</param>
    /// <param name="userClaim">使用者聲明服務</param>
    public DiscountHandler(
        ICartItemRepository cartRepository,
        IPromotionRepository promotionRepository,
        PromotionCalculationService calculationService,
        IUserClaim userClaim)
    {
        _cartRepository = cartRepository;
        _promotionRepository = promotionRepository;
        _calculationService = calculationService;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理購物車折扣查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 取得當前使用者的購物車項目
    /// 2. 取得所有有效的促銷活動
    /// 3. 委託 PromotionCalculationService 計算折扣
    /// 4. 回傳折扣力度最大的折扣
    /// 
    /// 錯誤處理：
    /// - 如果沒有折扣，回傳 null
    /// - 不拋出例外，由呼叫端決定如何處理
    /// 
    /// 效能考量：
    /// - 一次性查詢所有購物車項目
    /// - 一次性查詢所有有效促銷活動
    /// - 折扣計算在記憶體中完成
    /// 
    /// 與 PromotionCalculationService 的配合：
    /// - 該服務已經按 RuleType 分組並取最大折扣
    /// - 最終結果按 DiscountAmount 降序排列
    /// - 包含所有必要的折扣資訊（包括 Gift 和免運規則）
    /// </summary>
    /// <param name="request">購物車折扣查詢請求物件（不包含任何屬性）</param>
    /// <returns>
    /// 折扣力度最大的 Discount 物件，如果沒有折扣則回傳 null
    /// </returns>
    public async Task<Discount?> HandleAsync(DiscountQuery request)
    {
        // ========== 第一步：取得當前使用者的購物車項目 ==========
        // 從 UserClaim 取得當前登入使用者的 ID
        var userId = _userClaim.Id;
        
        // 查詢該使用者的購物車項目
        // 過濾條件：
        // 1. UserId 必須等於當前使用者 ID
        // 2. CartType 必須為 "shopping"（購物車類型）
        var cartItems = await _cartRepository.GetAllAsync(q => 
            q.Where(x => 
                x.UserId == userId && 
                x.CartType == "shopping")
        );

        // ========== 第二步：取得所有有效的促銷活動 ==========
        // 查詢所有有效的促銷活動
        // 過濾條件：
        // 1. Status 必須為 "active"（啟用狀態）
        // 2. StartDate 必須小於等於當前時間
        // 3. EndDate 必須大於等於當前時間
        var promotions = await _promotionRepository.GetAllAsync(
            q => q.Where(x => 
                x.Status == "active" && 
                x.StartDate <= DateTime.Now && 
                x.EndDate >= DateTime.Now)
        );

        // ========== 第三步：委託 PromotionCalculationService 計算折扣 ==========
        // 將購物車項目和促銷活動傳入計算服務
        // 該服務會：
        // 1. 遍歷每個促銷活動
        // 2. 計算每個活動的折扣
        // 3. 按規則類型分組取最大折扣
        // 4. 按折扣金額降序排列
        var discounts = await _calculationService.CalculateDiscountsAsync(cartItems, promotions);

        // ========== 第四步：如果沒有折扣，返回 null ==========
        // 檢查折扣集合是否為空
        // 以下情況會返回 null：
        // - discounts 為 null（理論上不會發生）
        // - discounts 為空集合（沒有適用的促銷活動）
        if (discounts == null || !discounts.Any())
            return null;

        // ========== 第五步：回傳打折力度最大的折扣 ==========
        // 使用 OrderByDescending 按折扣金額降序排列
        // 使用 First 取得第一個（折扣金額最大的）
        // 由於 PromotionCalculationService 已經降序排列，這裡可以優化為 First()
        return discounts.OrderByDescending(d => d.DiscountAmount).First();
    }
}
