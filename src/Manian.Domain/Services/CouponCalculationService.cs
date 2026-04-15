using Manian.Domain.Entities.Carts;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Products;

namespace Manian.Domain.Services;

/// <summary>
/// 優惠券計算服務
/// 
/// 職責：
/// - 驗證優惠券是否在有效期內
/// - 檢查優惠券是否已被使用
/// - 計算優惠券折扣金額
/// - 支援不同適用範圍（全館、特定商品、類別、品牌）
/// 
/// 設計原則：
/// - 遵循單一職責原則 (SRP)
/// - 優惠券驗證由調用方負責
/// - 依賴 IProductRepository 查詢商品資訊
/// - 可單元測試
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 屬於領域服務 (Domain Service)
/// - 不依賴任何基礎設施層的實作
/// 
/// 使用場景：
/// - 購物車頁面計算優惠券折扣
/// - 結帳頁面計算最終金額
/// - 訂單建立時計算優惠券折扣
/// 
/// 計算邏輯：
/// 1. 驗證優惠券是否已被使用
/// 2. 驗證優惠券是否在有效期內
/// 3. 根據優惠券的適用範圍篩選適用的購物車項目
/// 4. 計算折扣金額
/// 5. 返回折扣金額
/// 
/// 注意事項：
/// - 優惠券折扣是固定金額折扣
/// - 優惠券可以適用於全館、特定商品、類別或品牌
/// - 優惠券只能使用一次
/// </summary>
public class CouponCalculationService
{
    /// <summary>
    /// 商品倉儲介面
    /// CouponCalculationServic
    /// 用途：
    /// - 查詢商品資訊
    /// - 批次查詢商品以提升效能
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetProductsByIdsAsync 批次查詢
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 建構函式 - 初始化服務並注入依賴
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於查詢商品資訊</param>
    public CouponCalculationService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// 計算優惠券折扣
    /// 
    /// 執行流程：
    /// 1. 驗證優惠券是否已被使用
    /// 2. 驗證優惠券是否在有效期內
    /// 3. 根據優惠券的適用範圍篩選適用的購物車項目
    /// 4. 計算折扣金額
    /// 5. 返回折扣金額
    /// 
    /// 設計考量：
    /// - 優惠券驗證由調用方負責
    /// - 只計算折扣，不處理優惠券查詢
    /// - 批次查詢商品資訊，提升效能
    /// 
    /// 注意事項：
    /// - 調用方需確保優惠券有效且未使用
    /// - 調用方需確保優惠券在有效期內
    /// - 如果優惠券無效或過期，返回 0
    /// </summary>
    /// <param name="coupon">優惠券實體</param>
    /// <param name="cartItems">購物車項目</param>
    /// <returns>優惠券折扣金額</returns>
    public async Task<decimal> CalculateDiscountAsync(
        Coupon coupon, 
        IEnumerable<CartItem> cartItems)
    {
        // 轉換為列表以便多次遍歷
        var cartItemsList = cartItems.ToList();

        // ========== 第一步：驗證優惠券是否已被使用 ==========
        if (coupon.IsUsed)
        {
            return 0;
        }

        // ========== 第二步：驗證優惠券是否在有效期內 ==========
        var now = DateTimeOffset.UtcNow;
        if (coupon.ValidFrom > now)
        {
            return 0;
        }

        if (coupon.ValidUntil.HasValue && coupon.ValidUntil < now)
        {
            return 0;
        }

        // ========== 第三步：根據優惠券的適用範圍篩選適用的購物車項目 ==========
        var applicableItems = new List<CartItem>();

        // 收集所有商品 ID
        var productIds = cartItemsList.Select(ci => ci.ProductId).Distinct().ToList();

        // 批次查詢商品資訊
        var products = await _productRepository.GetAllAsync(
            q => q.Where(p => productIds.Contains(p.Id))
        );
        var productDict = products.ToDictionary(p => p.Id);

        foreach (var cartItem in cartItemsList)
        {
            // 取得商品資訊
            if (!productDict.TryGetValue(cartItem.ProductId, out var product))
                continue;

            // 根據優惠券的適用範圍判斷是否適用
            bool isApplicable = coupon.ScopeType switch
            {
                "all" => true, // 全館優惠
                "product" => cartItem.ProductId == coupon.ScopeId, // 特定商品優惠
                "category" => product.CategoryId == coupon.ScopeId, // 特定類別優惠
                "brand" => product.BrandId == coupon.ScopeId, // 特定品牌優惠
                _ => false
            };

            if (isApplicable)
            {
                applicableItems.Add(cartItem);
            }
        }

        // ========== 第四步：計算折扣金額 ==========
        decimal discountAmount = 0;

        if (applicableItems.Any())
        {
            // 計算所有適用項目的總數量
            var totalQuantity = applicableItems.Sum(item => item.Quantity);

            // 計算折扣金額：折扣金額 × 數量
            discountAmount = coupon.DiscountAmount * totalQuantity;
        }

        // ========== 第五步：返回折扣金額 ==========
        return discountAmount;
    }
}
