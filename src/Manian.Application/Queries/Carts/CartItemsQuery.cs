using Manian.Application.Models;
using Manian.Application.Services;
using Manian.Domain.Entities.Carts;
using Manian.Domain.Repositories.Carts;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Carts;

/// <summary>
/// 查詢購物車項目列表請求物件
/// 
/// 用途：
/// - 查詢當前使用者的購物車項目列表
/// - 支援購物車和願望清單兩種類型
/// - 支援分頁查詢
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<CartItem>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的購物車項目集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CartItemsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 購物車頁面顯示購物車項目
/// - 願望清單頁面顯示願望清單項目
/// - 訂單結帳時讀取購物車項目
/// 
/// 設計特點：
/// - 支援按類型篩選（shopping/wishlist）
/// - 支援分頁查詢
/// - 預設按建立時間排序
/// - 只回傳當前使用者的購物車項目
/// </summary>
public class CartItemsQuery : IRequest<Pagination<CartItem>>
{
    /// <summary>
    /// 購物車類型
    /// 
    /// 用途：
    /// - 識別要查詢的購物車類型
    /// - 支援購物車和願望清單兩種類型
    /// 
    /// 可選值：
    /// - "shopping"：購物車（預設值）
    /// - "wishlist"：願望清單
    /// 
    /// 驗證規則：
    /// - 必須是 "shopping" 或 "wishlist"
    /// - 預設值為 "shopping"
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢購物車項目
    /// var query = new CartItemsQuery { Type = "shopping" };
    /// 
    /// // 查詢願望清單項目
    /// var query = new CartItemsQuery { Type = "wishlist" };
    /// </code>
    /// </summary>
    public string Type { get; set; } = "shopping";

    /// <summary>
    /// 頁面大小（每頁資料筆數）
    /// 
    /// 用途：
    /// - 控制每次查詢回傳的資料筆數
    /// - 用於分頁功能
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 預設值為 20
    /// 
    /// 使用範例：
    /// <code>
    /// // 每頁 10 筆資料
    /// var query = new CartItemsQuery { Size = 10 };
    /// </code>
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// 游標（用於分頁）
    /// 
    /// 用途：
    /// - 標記分頁位置
    /// - 用於游標分頁（Cursor-based Pagination）
    /// 
    /// 說明：
    /// - 首次查詢時不傳入此參數
    /// - 後續查詢使用上一次回傳的 Cursor 值
    /// - 由 Pagination 模型提供
    /// 
    /// 使用範例：
    /// <code>
    /// // 首次查詢
    /// var query1 = new CartItemsQuery { Size = 10 };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 下一頁查詢
    /// var query2 = new CartItemsQuery 
    /// { 
    ///     Size = 10, 
    ///     Cursor = result1.Cursor 
    /// };
    /// var result2 = await _mediator.SendAsync(query2);
    /// </code>
    /// </summary>
    public string? Cursor { get; set; }
}

/// <summary>
/// 購物車項目查詢處理器
/// 
/// 職責：
/// - 接收 CartItemsQuery 請求
/// - 根據使用者 ID 和類型查詢購物車項目
/// - 將查詢結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CartItemsQuery, Pagination<CartItem>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICartItemRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 只查詢當前使用者的購物車項目
/// - 支援按類型篩選（shopping/wishlist）
/// - 支援游標分頁
/// - 按建立時間排序
/// 
/// 參考實作：
/// - SkusQueryHandler：查詢 SKU 列表的類似實作
/// - TagsQueryHandler：查詢標籤列表的類似實作
/// </summary>
public class CartItemsQueryHandler : IRequestHandler<CartItemsQuery, Pagination<CartItem>>
{
    /// <summary>
    /// 購物車倉儲介面
    /// 
    /// 用途：
    /// - 存取購物車項目資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 繼承自 Repository<CartItem>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Carts/ICartItemRepository.cs
    /// </summary>
    private readonly ICartItemRepository _repository;

    /// <summary>
    /// 使用者宣告介面
    /// 
    /// 用途：
    /// - 取得當前登入使用者的 ID
    /// - 取得當前使用者的 Session ID
    /// 
    /// 實作方式：
    /// - 見 Application/Services/UserClaim.cs
    /// - 從 HTTP Context 中取得使用者資訊
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">購物車倉儲，用於查詢購物車項目</param>
    /// <param name="userClaim">使用者宣告，用於取得使用者 ID</param>
    public CartItemsQueryHandler(
        ICartItemRepository repository,
        IUserClaim userClaim)
    {
        _repository = repository;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理購物車項目查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證購物車類型
    /// 2. 取得當前使用者 ID
    /// 3. 呼叫 Repository 查詢購物車項目
    /// 4. 將查詢結果包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 只查詢當前使用者的購物車項目
    /// - 支援按類型篩選（shopping/wishlist）
    /// - 支援游標分頁
    /// - 按建立時間降序排序（最新的在前）
    /// 
    /// 錯誤處理：
    /// - 購物車類型無效：拋出 ArgumentException
    /// - 如果購物車沒有項目，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 範例 1：查詢購物車項目（預設每頁 20 筆）
    /// var query1 = new CartItemsQuery { Type = "shopping" };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：查詢願望清單項目（每頁 10 筆）
    /// var query2 = new CartItemsQuery 
    /// { 
    ///     Type = "wishlist", 
    ///     Size = 10 
    /// };
    /// var result2 = await _mediator.SendAsync(query2);
    /// 
    /// // 範例 3：使用游標分頁
    /// var query3 = new CartItemsQuery 
    /// { 
    ///     Type = "shopping", 
    ///     Size = 10, 
    ///     Cursor = result1.Cursor 
    /// };
    /// var result3 = await _mediator.SendAsync(query3);
    /// </code>
    /// </summary>
    /// <param name="request">購物車項目查詢請求物件，包含 Type、Size、Cursor</param>
    /// <returns>包含符合條件購物車項目的分頁模型</returns>
    public async Task<Pagination<CartItem>> HandleAsync(CartItemsQuery request)
    {
        // ========== 第一步：驗證購物車類型 ==========
        if (request.Type != "shopping" && request.Type != "wishlist")
            throw new ArgumentException("購物車類型必須是 'shopping' 或 'wishlist'");

        // ========== 第二步：取得當前使用者 ID ==========
        var userId = _userClaim.Id;

        // ========== 第三步：查詢購物車項目 ==========
        // 呼叫 Repository 的 GetAllAsync 方法查詢購物車項目
        // 使用 Func<IQueryable<CartItem>, IQueryable<CartItem>> 參數定義查詢邏輯
        var cartItems = await _repository.GetAllAsync(q => 
            q.Where(x => 
                x.UserId == userId && 
                x.CartType == request.Type)
            .OrderByDescending(x => x.CreatedAt));

        // ========== 第四步：將查詢結果包裝成 Pagination 物件回傳 ==========
        // 使用游標分頁邏輯
        // cursorSelector: 使用 CreatedAt 作為游標欄位
        // requestedSize: 使用 request.Size 作為每頁大小
        // cursor: 使用 request.Cursor 作為游標值
        return new Pagination<CartItem>(
            items: cartItems,
            requestedSize: request.Size,
            cursorSelector: x => x.CreatedAt.ToString()
        );
    }
}
