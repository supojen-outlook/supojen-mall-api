using Manian.Domain.Entities.Carts;
using Manian.Domain.Entities.Products;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.ValueObjects;

namespace Manian.Domain.Services;

/// <summary>
/// 促銷計算服務
/// 
/// 職責：
/// - 計算購物車項目的促銷折扣
/// - 判斷適用的促銷規則
/// - 處理多個促銷活動的優先級
/// 
/// 設計原則：
/// - 遵循單一職責原則 (SRP)
/// - 依賴 IProductRepository 查詢商品資訊
/// - 依賴 IPromotionRepository 查詢促銷規則和範圍
/// - 可單元測試
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 屬於領域服務 (Domain Service)
/// - 不依賴任何基礎設施層的實作
/// 
/// 使用場景：
/// - 購物車頁面計算折扣
/// - 結帳頁面計算最終金額
/// - 促銷活動效果預覽
/// 
/// 計算邏輯：
/// 1. 遍歷每個促銷活動
/// 2. 篩選適用範圍內的商品
/// 3. 計算符合條件的商品總金額和總數量
/// 4. 遍歷每個促銷規則，計算折扣
/// 5. 按規則類型分組，取最大折扣
/// 6. 按折扣金額降序排列
/// 
/// 規則類型：
/// - full_reduction：滿額減（滿 X 元減 Y 元）
/// - discount：折扣（滿 X 元打 Y 折）
/// - gift：贈品（滿 X 件送 Y 件）
/// - free_shipping：免運（滿 X 元免運費）
/// 
/// 注意事項：
/// - 贈品規則的 DiscountAmount 為 0
/// - 免運規則的 DiscountAmount 為 0
/// - 但會透過 GiftItemId 和 IsFreeShipping 屬性識別
/// </summary>
public class PromotionCalculationService
{
    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 查詢購物車項目對應的商品資訊
    /// - 取得商品的類別、品牌等資訊
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetAllAsync 查詢實體集合
    /// 
    /// 查詢優化：
    /// - 一次性查詢所有商品，避免 N+1 問題
    /// - 使用 Dictionary 快取商品資訊
    /// </summary>
    private readonly IProductRepository _productRepository;
    
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 查詢促銷活動的規則
    /// - 查詢促銷活動的範圍
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供擴展方法 GetRulesAsync、GetScopesAsync
    /// 
    /// 查詢優化：
    /// - 一次性查詢所有規則和範圍
    /// - 避免多次資料庫查詢
    /// </summary>
    private readonly IPromotionRepository _promotionRepository;

    /// <summary>
    /// 建構函式 - 初始化服務並注入依賴
    /// 
    /// 設計原則：
    /// - 使用建構函式注入 (Constructor Injection)
    /// - 所有依賴都是介面，符合依賴倒置原則
    /// - 所有依賴都是 readonly，確保不可變性
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於查詢商品資訊</param>
    /// <param name="promotionRepository">促銷活動倉儲，用於查詢規則和範圍</param>
    public PromotionCalculationService(
        IProductRepository productRepository,
        IPromotionRepository promotionRepository)
    {
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
    }

    /// <summary>
    /// 計算所有適用的促銷折扣
    /// 
    /// 執行流程：
    /// 1. 一次性取得所有商品資訊（避免 N+1 查詢）
    /// 2. 遍歷每個促銷活動
    /// 3. 計算每個促銷活動的折扣
    /// 4. 按規則類型分組，取最大折扣
    /// 5. 按折扣金額降序排列
    /// 
    /// 設計考量：
    /// - 使用快取避免重複查詢商品資訊
    /// - 按規則類型分組，避免同一類型規則重疊
    /// - 按折扣金額降序排列，方便取最佳折扣
    /// 
    /// 與 DiscountHandler 的配合：
    /// - 該服務回傳所有折扣
    /// - DiscountHandler 取折扣金額最大的折扣
    /// 
    /// 注意事項：
    /// - 贈品規則的 DiscountAmount 為 0
    /// - 免運規則的 DiscountAmount 為 0
    /// - 但會透過 GiftItemId 和 IsFreeShipping 屬性識別
    /// </summary>
    /// <param name="cartItems">購物車項目集合</param>
    /// <param name="promotions">促銷活動集合</param>
    /// <returns>折扣結果集合，按折扣金額降序排列</returns>
    public async Task<IEnumerable<Discount>> CalculateDiscountsAsync(
        IEnumerable<CartItem> cartItems,
        IEnumerable<Promotion> promotions)
    {
        var results = new List<Discount>();

        // ========== 第一步：一次性取得所有商品資訊 ==========
        // 為什麼要一次性查詢？
        // 1. 避免在迴圈中查詢資料庫（N+1 問題）
        // 2. 減少資料庫連線開銷
        // 3. 提升查詢效能
        var productIds = cartItems
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var products = await _productRepository.GetAllAsync(
            q => q.Where(x => productIds.Contains(x.Id))
        );

        // 建立商品快取，避免重複查詢
        // 使用 Dictionary 提升查詢效能
        var productCache = products.ToDictionary(p => p.Id);

        // ========== 第二步：遍歷每個促銷活動 ==========
        foreach (var promotion in promotions)
        {
            // 檢查促銷活動是否有效
            if (!IsPromotionValid(promotion))
                continue;

            // 計算該促銷活動的折扣
            var discount = await CalculatePromotionDiscountAsync(
                cartItems, 
                promotion, 
                productCache);
            
            // 如果有折扣，加入結果集合
            if (discount != null)
                results.Add(discount);
        }

        // ========== 第三步：按 RuleType 分組並取最大折扣 ==========
        // 為什麼要按 RuleType 分組？
        // 1. 避免同一類型規則重疊（如兩個滿額減規則）
        // 2. 確保每種規則類型只取最大折扣
        // 3. 支援多種規則類型並存（如滿額減 + 贈品）
        var groupedDiscounts = results
            .GroupBy(d => d.RuleType)
            .Select(g => g.OrderByDescending(d => d.DiscountAmount).First());

        // 按折扣金額降序排列
        return groupedDiscounts.OrderByDescending(r => r.DiscountAmount);
    }

    /// <summary>
    /// 檢查促銷活動是否有效
    /// 
    /// 檢查項目：
    /// 1. 時間範圍：是否在活動期間內
    /// 2. 狀態：是否為啟用狀態
    /// 3. 使用次數：是否超過總使用次數限制
    /// 
    /// 設計考量：
    /// - 使用 DateTimeOffset.UtcNow 確保時區一致性
    /// - 使用簡單的條件判斷，提升效能
    /// - 不依賴外部服務，方便單元測試
    /// 
    /// 注意事項：
    /// - 不檢查每人使用次數限制（由呼叫端處理）
    /// - 不檢查會員等級限制（由呼叫端處理）
    /// - 不檢查通路限制（由呼叫端處理）
    /// </summary>
    /// <param name="promotion">促銷活動實體</param>
    /// <returns>如果促銷活動有效，返回 true；否則返回 false</returns>
    private bool IsPromotionValid(Promotion promotion)
    {
        var now = DateTimeOffset.UtcNow;
        
        // 檢查時間範圍
        if (now < promotion.StartDate || now > promotion.EndDate)
            return false;

        // 檢查狀態
        if (promotion.Status != "active")
            return false;

        // 檢查使用次數限制
        if (promotion.LimitTotal.HasValue && 
            promotion.UsedCount >= promotion.LimitTotal.Value)
            return false;

        return true;
    }

    /// <summary>
    /// 計算單個促銷活動的折扣
    /// 
    /// 執行流程：
    /// 1. 篩選適用範圍內的商品
    /// 2. 計算符合條件的商品總金額和總數量
    /// 3. 查詢促銷活動的所有規則
    /// 4. 遍歷每個規則，計算折扣
    /// 5. 取折扣金額最大的規則
    /// 6. 檢查是否有免運規則
    /// 7. 檢查是否有贈品規則
    /// 8. 返回折扣結果
    /// 
    /// 設計考量：
    /// - 使用快取避免重複查詢商品資訊
    /// - 按規則類型分組，避免同一類型規則重疊
    /// - 支援多種規則類型並存（如滿額減 + 贈品）
    /// 
    /// 注意事項：
    /// - 贈品規則的 DiscountAmount 為 0
    /// - 免運規則的 DiscountAmount 為 0
    /// - 但會透過 GiftItemId 和 IsFreeShipping 屬性識別
    /// </summary>
    /// <param name="cartItems">購物車項目集合</param>
    /// <param name="promotion">促銷活動實體</param>
    /// <param name="productCache">商品快取字典</param>
    /// <returns>折扣結果，如果沒有折扣則返回 null</returns>
    private async Task<Discount?> CalculatePromotionDiscountAsync(
        IEnumerable<CartItem> cartItems,
        Promotion promotion,
        Dictionary<int, Product> productCache)
    {
        // ========== 第一步：篩選適用範圍內的商品 ==========
        var applicableItems = await FilterApplicableItemsAsync(
            cartItems, 
            promotion, 
            productCache);
        
        // 如果沒有適用的商品，返回 null
        if (!applicableItems.Any())
            return null;

        // ========== 第二步：計算符合條件的商品總金額和總數量 ==========
        var totalAmount = applicableItems.Sum(item => 
            item.UnitPrice * item.Quantity);
        var totalQuantity = applicableItems.Sum(item => item.Quantity);

        // ========== 第三步：查詢促銷活動的所有規則 ==========
        var rules = await _promotionRepository.GetRulesAsync(promotion.Id);

        // 如果沒有規則，返回 null
        if(rules == null || !rules.Any())
            return null;

        // ========== 第四步：遍歷每個規則，計算折扣 ==========
        var validRules = new List<(PromotionRule rule, decimal discount)>();

        foreach (var rule in rules)
        {
            var discount = CalculateRuleDiscount(
                rule, 
                totalAmount, 
                totalQuantity);
            
            // 如果折扣大於 0，加入有效規則集合
            if (discount > 0)
            {
                validRules.Add((rule, discount));
            }
        }

        // 如果沒有有效規則，返回 null
        if (!validRules.Any())
            return null;

        // ========== 第五步：取折扣金額最大的規則 ==========
        var bestRule = validRules.OrderByDescending(r => r.discount).First();

        // ========== 第六步：檢查是否有免運規則 ==========
        var isFreeShipping = validRules.Any(x => x.rule.RuleType == "free_shipping");

        // ========== 第七步：檢查是否有贈品規則 ==========
        // 取門檻最高的贈品規則（因為門檻越高，贈品越好）
        var bestGiftRule = validRules
            .Where(x => x.rule.RuleType == "gift")
            .OrderByDescending(x => x.rule.ThresholdQuantity)
            .FirstOrDefault();

        // ========== 第八步：返回折扣結果 ==========
        return new Discount
        {
            PromotionId = promotion.Id,
            PromotionName = promotion.Name,
            RuleId = bestRule.rule.Id,
            RuleType = bestRule.rule.RuleType,
            TabName = bestRule.rule.TabName,
            DiscountAmount = bestRule.discount,
            IsFreeShipping = isFreeShipping,
            GiftItemId = bestGiftRule.rule?.GiftItemId ?? null,
            ApplicableItems = applicableItems
        };
    }

    /// <summary>
    /// 篩選適用範圍內的商品
    /// 
    /// 執行流程：
    /// 1. 查詢促銷活動的所有範圍
    /// 2. 遍歷每個購物車項目
    /// 3. 檢查每個範圍是否適用
    /// 4. 檢查是否被排除
    /// 5. 返回適用的商品集合
    /// 
    /// 範圍類型：
    /// - all：全館適用
    /// - product：特定商品
    /// - category：特定類別
    /// - brand：特定品牌
    /// 
    /// 設計考量：
    /// - 使用快取避免重複查詢商品資訊
    /// - 支援排除特定範圍（IsExclude）
    /// - 支援多個範圍並存
    /// 
    /// 注意事項：
    /// - 如果沒有範圍，返回空集合
    /// - 如果商品被排除，不加入結果集合
    /// - 如果商品適用多個範圍，只加入一次
    /// </summary>
    /// <param name="cartItems">購物車項目集合</param>
    /// <param name="promotion">促銷活動實體</param>
    /// <param name="productCache">商品快取字典</param>
    /// <returns>適用的商品集合</returns>
    private async Task<IEnumerable<CartItem>> FilterApplicableItemsAsync(
        IEnumerable<CartItem> cartItems,
        Promotion promotion,
        Dictionary<int, Product> productCache)
    {
        // ========== 第一步：查詢促銷活動的所有範圍 ==========
        var scopes = await _promotionRepository.GetScopesAsync(promotion.Id);

        // 如果沒有範圍，返回空集合
        if (scopes == null || !scopes.Any())
            return Enumerable.Empty<CartItem>();

        // ========== 第二步：遍歷每個購物車項目 ==========
        var applicableItems = new List<CartItem>();

        foreach (var item in cartItems)
        {
            bool isApplicable = false;
            bool isExcluded = false;

            // ========== 第三步：檢查每個範圍 ==========
            foreach (var scope in scopes)
            {
                switch (scope.ScopeType)
                {
                    case "all":
                        // 全館適用
                        isApplicable = true;
                        break;
                    case "product":
                        // 特定商品
                        if (scope.ScopeId == item.ProductId)
                            isApplicable = true;
                        break;
                    case "category":
                        // 特定類別
                        if (productCache.TryGetValue(item.ProductId, out var product) &&
                            product.CategoryId == scope.ScopeId)
                            isApplicable = true;
                        break;
                    case "brand":
                        // 特定品牌
                        if (productCache.TryGetValue(item.ProductId, out var brandProduct) &&
                            brandProduct.BrandId == scope.ScopeId)
                            isApplicable = true;
                        break;
                }

                // ========== 第四步：檢查是否被排除 ==========
                if (scope.IsExclude)
                    isExcluded = true;
            }

            // ========== 第五步：如果適用且未被排除，加入結果集合 ==========
            if (isApplicable && !isExcluded)
                applicableItems.Add(item);
        }

        return applicableItems;
    }

    /// <summary>
    /// 根據規則計算折扣金額
    /// 
    /// 規則類型：
    /// - full_reduction：滿額減（滿 X 元減 Y 元）
    /// - discount：折扣（滿 X 元打 Y 折）
    /// - gift：贈品（滿 X 件送 Y 件）
    /// - free_shipping：免運（滿 X 元免運費）
    /// 
    /// 設計考量：
    /// - 使用 switch 處理不同規則類型
    /// - 贈品規則和免運規則返回 0
    /// - 折扣規則檢查最高折抵金額
    /// 
    /// 注意事項：
    /// - 贈品規則的 DiscountAmount 為 0
    /// - 免運規則的 DiscountAmount 為 0
    /// - 但會透過 GiftItemId 和 IsFreeShipping 屬性識別
    /// </summary>
    /// <param name="rule">促銷規則實體</param>
    /// <param name="totalAmount">符合條件的商品總金額</param>
    /// <param name="totalQuantity">符合條件的商品總數量</param>
    /// <returns>折扣金額，如果不符合條件則返回 0</returns>
    private decimal CalculateRuleDiscount(
        PromotionRule rule,
        decimal totalAmount,
        int totalQuantity)
    {
        switch (rule.RuleType)
        {
            case "full_reduction":
                // 滿額減：滿 X 元減 Y 元
                if (rule.ThresholdAmount.HasValue && 
                    totalAmount >= rule.ThresholdAmount.Value &&
                    rule.DiscountAmount.HasValue)
                {
                    return rule.DiscountAmount.Value;
                }
                break;

            case "discount":
                // 折扣：滿 X 元打 Y 折
                if (rule.ThresholdAmount.HasValue && 
                    totalAmount >= rule.ThresholdAmount.Value &&
                    rule.DiscountRate.HasValue)
                {
                    var discount = totalAmount * (rule.DiscountRate.Value / 100);
                    
                    // 檢查最高折抵金額
                    if (rule.MaxDiscountAmount.HasValue)
                        discount = Math.Min(discount, rule.MaxDiscountAmount.Value);
                    
                    return discount;
                }
                break;

            case "gift":
                // 贈品：滿 X 件送 Y 件
                if (rule.ThresholdQuantity.HasValue && 
                    totalQuantity >= rule.ThresholdQuantity.Value)
                {
                    // 贈品規則不產生金額折扣
                    return 0;
                }
                break;

            case "free_shipping":
                // 免運：滿 X 元免運費
                if (rule.ThresholdAmount.HasValue && 
                    totalAmount >= rule.ThresholdAmount.Value)
                {
                    // 返回 0 表示規則適用，但無金額折扣
                    return 0;
                }
                break;
        }

        return 0;
    }
}
