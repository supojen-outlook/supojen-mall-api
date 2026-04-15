using Manian.Application.Services;
using Manian.Domain.Entities.Carts;
using Manian.Domain.Repositories.Carts;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Manian.Domain.ValueObjects;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Promotions;

/// <summary>
/// 查詢目前可用的促銷折扣請求
/// 
/// 用途：
/// - 讓客戶查詢目前可用的促銷折扣
/// - 根據用戶購物車自動計算折扣
/// - 回傳格式為 IEnumerable<Discount>
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<Discount>>，表示這是一個查詢請求
/// - 回傳折扣集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 AvailableDiscountsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 購物車頁面顯示可用折扣
/// - 結帳頁面計算最終金額
/// - 促銷活動效果預覽
/// 
/// 設計特點：
/// - 不帶任何參數，直接從用戶購物車查詢折扣
/// - 不使用 Pagination，直接回傳 IEnumerable<Discount>
/// - 只回傳目前時間有效的促銷活動
/// - 只回傳啟用狀態的促銷活動
/// - 只回傳未超過總使用次數限制的促銷活動
/// - 自動從用戶購物車取得商品資訊
/// 
/// 變更說明：
/// - 移除 CartItems 屬性
/// - 改為從用戶購物車自動查詢
/// - 簡化 API 調用，前端不需傳遞購物車資料
/// 
/// 參考實作：
/// - PromotionCalculationService：核心計算邏輯
/// - DiscountQuery：類似的查詢模式
/// </summary>
public class AvailableDiscountsQuery : IRequest<IEnumerable<Discount>>;

/// <summary>
/// 可用促銷折扣查詢處理器
/// 
/// 職責：
/// - 接收 AvailableDiscountsQuery 請求
/// - 從用戶購物車取得商品資訊
/// - 查詢目前可用的促銷活動
/// - 使用 PromotionCalculationService 計算折扣
/// - 回傳折扣集合
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AvailableDiscountsQuery, IEnumerable<Discount>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository 和 PromotionCalculationService
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 自動從用戶購物車取得商品資訊
/// - 直接使用 PromotionCalculationService 計算折扣
/// - 不使用 Pagination，直接回傳 IEnumerable<Discount>
/// - 依賴 PromotionCalculationService 的實作細節
/// 
/// 變更說明：
/// - 新增 ICartItemRepository 依賴
/// - 新增 IUserClaim 依賴
/// - 從用戶購物車自動查詢商品資訊
/// 
/// 參考實作：
/// - PromotionCalculationService：核心計算邏輯
/// - DiscountQuery：類似的查詢模式
/// </summary>
public class AvailableDiscountsQueryHandler : IRequestHandler<AvailableDiscountsQuery, IEnumerable<Discount>>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷活動資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _repository;

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
    /// - 註冊為 Scoped（見 Domain/DI.cs）
    /// 
    /// 設計特點：
    /// - 支援多種規則類型（滿額減、折扣、贈品、免運）
    /// - 按規則類型分組，取最大折扣
    /// - 按折扣金額降序排列
    /// </summary>
    private readonly PromotionCalculationService _calculationService;

    /// <summary>
    /// 用戶權益
    /// 
    /// 用途：
    /// - 取得用戶資訊
    /// - 判斷用戶是否有權使用特定促銷活動
    /// - 取得用戶購物車資訊
    /// 
    /// 實作方式：
    /// - 見 Shared/Security/UserClaim.cs
    /// - 註冊為 Singleton（見 Shared/Security/DI.cs）
    /// 
    /// 設計特點：
    /// - 支援多種用戶資訊（如會員等級、生日、註冊時間等）
    /// - 支援多種權益（如會員優惠、生日優惠等）
    /// - 支援多種權益判斷邏輯（如會員等級判斷、生日判斷等）
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 購物車項目倉儲介面
    /// 
    /// 用途：
    /// - 存取購物車項目資料
    /// - 執行資料庫查詢
    /// - 取得用戶購物車中的商品資訊
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Carts/ICartItemRepository.cs
    /// </summary>
    private readonly ICartItemRepository _cartItemRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢促銷活動資料</param>
    /// <param name="calculationService">促銷計算服務，用於計算折扣</param>
    /// <param name="userClaim">用戶權益，用於取得用戶資訊</param>
    /// <param name="cartItemRepository">購物車項目倉儲，用於查詢購物車項目資料</param>
    public AvailableDiscountsQueryHandler(
        IPromotionRepository repository,
        PromotionCalculationService calculationService,
        IUserClaim userClaim,
        ICartItemRepository cartItemRepository)
    {
        _repository = repository;
        _calculationService = calculationService;
        _userClaim = userClaim;
        _cartItemRepository = cartItemRepository;
    }

    /// <summary>
    /// 處理可用促銷折扣查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 AvailableDiscountsQuery 請求（不帶任何參數）
    /// 2. 從用戶購物車取得商品資訊
    /// 3. 查詢可用的促銷活動
    /// 4. 使用 PromotionCalculationService 計算折扣
    /// 5. 回傳折扣集合
    /// 
    /// 查詢特性：
    /// - 自動從用戶購物車取得商品資訊
    /// - 只查詢當前時間在活動期間內的促銷
    /// - 只回傳啟用狀態的促銷活動
    /// - 只回傳未超過總使用次數限制的促銷活動
    /// 
    /// 錯誤處理：
    /// - 如果用戶未登入，會拋出例外
    /// - 如果用戶購物車為空，會返回空集合
    /// - 如果沒有可用的促銷活動，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 變更說明：
    /// - 新增從用戶購物車取得商品資訊的邏輯
    /// - 移除從請求參數取得購物車資訊的邏輯
    /// </summary>
    /// <param name="request">可用促銷折扣查詢請求物件（不帶任何參數）</param>
    /// <returns>可用折扣集合</returns>
    public async Task<IEnumerable<Discount>> HandleAsync(AvailableDiscountsQuery request)
    {
        // ========== 第一步：取得當前時間 ==========
        // 使用 UTC 時間，避免時區問題
        var now = DateTimeOffset.UtcNow;

        // ========== 第二步：查詢可用的促銷活動 ==========
        // 使用 Repository 的 GetAllAsync 方法查詢促銷活動
        // 這個方法會：
        // 1. 從資料庫查詢符合條件的促銷活動
        // 2. 包含關聯的 PromotionRule 實體
        // 3. 按建立時間排序
        // 4. 回傳促銷活動集合
        var promotions = await _repository.GetAllAsync(query =>
        {
            // ========== 篩選條件 1：活動已開始 ==========
            // 只查詢開始時間小於等於當前時間的促銷活動
            query = query.Where(p => p.StartDate <= now);
            
            // ========== 篩選條件 2：活動尚未結束 ==========
            // 只查詢結束時間大於等於當前時間的促銷活動
            query = query.Where(p => p.EndDate >= now);
            
            // ========== 篩選條件 3：活動狀態為啟用 ==========
            // 只查詢狀態為 "active" 的促銷活動
            query = query.Where(p => p.Status == "active");
            
            // ========== 篩選條件 4：未超過總使用次數限制 ==========
            // 只查詢未超過總使用次數限制的促銷活動
            // 如果 LimitTotal 為 null，表示不限制使用次數
            query = query.Where(p => !p.LimitTotal.HasValue || p.UsedCount < p.LimitTotal.Value);

            // 回傳最終組合好的 IQueryable
            return query;
        });

        // ========== 第三步：從用戶購物車取得商品資訊 ==========
        // 使用 CartItemRepository 查詢用戶購物車中的商品
        // 這個方法會：
        // 1. 從資料庫查詢用戶購物車中的商品
        // 2. 包含關聯的 Product 和 Sku 實體
        // 3. 回傳購物車項目集合
        var cartItems = await _cartItemRepository.GetAllAsync(
            query => query.Where(x => x.UserId == _userClaim.Id)
        );

        // ========== 第四步：使用 PromotionCalculationService 計算折扣 ==========
        // 這個方法會：
        // 1. 遍歷每個促銷活動
        // 2. 篩選適用範圍內的商品
        // 3. 計算符合條件的商品總金額和總數量
        // 4. 遍歷每個促銷規則，計算折扣
        // 5. 按規則類型分組，取最大折扣
        // 6. 按折扣金額降序排列
        var discounts = await _calculationService.CalculateDiscountsAsync(
            cartItems,
            promotions);

        // ========== 第五步：回傳折扣集合 ==========
        // 直接回傳折扣集合，不使用 Pagination
        return discounts;
    }
}
