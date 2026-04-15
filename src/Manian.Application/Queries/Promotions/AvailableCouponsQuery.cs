using Manian.Application.Services;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Carts;
using Manian.Domain.Repositories.Promotions;
using Shared.Mediator.Interface;


/// <summary>
/// 查詢用戶購物車可用的優惠券請求
/// 
/// 用途：
/// - 查詢當前用戶購物車中所有商品可用的優惠券
/// - 支援購物車(shopping)和願望清單(wishlist)兩種類型
/// - 過濾出當前時間有效且未使用的優惠券
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<Coupon>>，表示這是一個查詢請求
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 AvailableCouponsQueryHandler 配合使用
/// </summary>
public class AvailableCouponsQuery : IRequest<IEnumerable<Coupon>>
{
    /// <summary>
    /// 購物車類型
    /// 
    /// 可選值：
    /// - "shopping"：購物車（預設值）
    /// - "wishlist"：願望清單
    /// 
    /// 用途：
    /// - 指定要查詢的購物車類型
    /// - 影響查詢結果的範圍
    /// </summary>
    public string? CartType { get; set; } = "shopping";
}

/// <summary>
/// 可用優惠券查詢處理器
/// 
/// 職責：
/// - 查詢用戶購物車中的所有商品
/// - 過濾出當前時間有效且未使用的優惠券
/// - 根據優惠券的適用範圍進行匹配
/// - 回傳符合條件的優惠券集合
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AvailableCouponsQuery, IEnumerable<Coupon>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICouponRepository 和 ICartItemRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
public class AvailableCouponsQueryHandler : IRequestHandler<AvailableCouponsQuery, IEnumerable<Coupon>>
{
    /// <summary>
    /// 優惠券倉儲介面
    /// 
    /// 用途：
    /// - 存取優惠券資料
    /// - 提供查詢優惠券的方法
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/CouponRepository.cs）
    /// - 繼承自 Repository<Coupon>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/ICouponRepository.cs
    /// </summary>
    private readonly ICouponRepository _couponRepository;

    /// <summary>
    /// 購物車項目倉儲介面
    /// 
    /// 用途：
    /// - 存取購物車項目資料
    /// - 提供查詢購物車項目的方法
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 繼承自 Repository<CartItem>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Carts/ICartItemRepository.cs
    /// </summary>
    private readonly ICartItemRepository _cartItemRepository;

    /// <summary>
    /// 用戶聲明服務
    /// 
    /// 用途：
    /// - 取得當前登入用戶的資訊
    /// - 提供用戶 ID 等基本資訊
    /// 
    /// 實作方式：
    /// - 從 HTTP 請求的 Claims 中提取用戶資訊
    /// - 見 Infrastructure/Services/UserClaim.cs
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="couponRepository">優惠券倉儲，用於查詢優惠券</param>
    /// <param name="cartItemRepository">購物車項目倉儲，用於查詢購物車商品</param>
    /// <param name="userClaim">用戶聲明服務，用於取得當前用戶 ID</param>
    public AvailableCouponsQueryHandler(
        ICouponRepository couponRepository,
        ICartItemRepository cartItemRepository,
        IUserClaim userClaim)
    {
        _couponRepository = couponRepository;
        _cartItemRepository = cartItemRepository;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理可用優惠券查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 取得當前用戶的購物車項目
    /// 2. 取得當前用戶的所有優惠券（包括全局優惠券和用戶專屬優惠券）
    /// 3. 過濾出未使用且在有效期內的優惠券
    /// 4. 根據優惠券的適用範圍進行匹配
    /// 5. 回傳符合條件的優惠券集合
    /// 
    /// 優惠券適用範圍匹配規則：
    /// - "all"：適用於所有商品
    /// - "product"：適用於特定商品（根據 ProductId 匹配）
    /// - "category"：適用於特定類別的商品（根據 ProductId 匹配）
    /// - "brand"：適用於特定品牌的商品（根據 ProductId 匹配）
    /// 
    /// 注意事項：
    /// - 只回傳未使用且在有效期內的優惠券
    /// - 優惠券必須適用於購物車中的至少一個商品
    /// - 不考慮優惠券的使用次數限制
    /// </summary>
    /// <param name="request">可用優惠券查詢請求物件，包含 CartType</param>
    /// <returns>符合條件的優惠券集合</returns>
    public async Task<IEnumerable<Coupon>> HandleAsync(AvailableCouponsQuery request)
    {
        // ========== 第一步：獲取當前用戶的購物車項目 ==========
        var userId = _userClaim.Id;
        var cartItems = await _cartItemRepository.GetAllAsync(q => 
            q.Where(x => 
                x.UserId == userId && 
                x.CartType == request.CartType)
        );

        // ========== 第二步：獲取當前用戶的所有優惠券 ==========
        var allCoupons = await _couponRepository.GetAllAsync(q => 
            q.Where(c => 
                (c.UserId == null || c.UserId == userId) && // 全局優惠券或用戶專屬優惠券
                !c.IsUsed && // 未使用
                c.ValidFrom <= DateTimeOffset.UtcNow && // 已開始
                (c.ValidUntil == null || c.ValidUntil >= DateTimeOffset.UtcNow)) // 未過期
        );

        // ========== 第三步：過濾出適用於購物車商品的優惠券 ==========
        var availableCoupons = new List<Coupon>();
        
        foreach (var coupon in allCoupons)
        {
            // 根據優惠券的適用範圍進行匹配
            bool isApplicable = coupon.ScopeType switch
            {
                "all" => true, // 全部商品可用
                "product" => cartItems.Any(ci => ci.ProductId == coupon.ScopeId),  // 特定商品可用
                "category" => cartItems.Any(ci => ci.ProductId == coupon.ScopeId), // 特定類別可用
                "brand" => cartItems.Any(ci => ci.ProductId == coupon.ScopeId),    // 特定品牌可用
                _ => false
            };

            // 如果優惠券適用於購物車中的任何商品，則加入結果
            if (isApplicable)
            {
                availableCoupons.Add(coupon);
            }
        }

        return availableCoupons;
    }
}
