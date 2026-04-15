using System;
using Manian.Application.Models;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Promotions;

/// <summary>
/// 查詢優惠券列表的請求物件
/// 
/// 用途：
/// - 取得系統中的優惠券列表
/// - 支援多種搜尋條件（UserId、ScopeType、ScopeId）
/// - 支援彈性排序和游標分頁
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Coupon>>，表示這是一個查詢請求
/// - 回傳優惠券實體集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CouponsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 優惠券管理頁面
/// - 用戶優惠券列表
/// - 優惠券搜尋功能
/// 
/// 設計特點：
/// - 支援用戶 ID 篩選（查看特定用戶的優惠券）
/// - 支援範圍類型篩選（all/product/category/brand）
/// - 支援彈性排序（可選多個欄位）
/// - 使用游標分頁（Cursor-based Pagination）
/// 
/// 參考實作：
/// - ProductsQuery：相同的排序和分頁實作
/// - PromotionsQuery：相同的查詢模式
/// </summary>
public class CouponsQuery : IRequest<Pagination<Coupon>>
{
    // =========================================================================
    // 搜尋條件 (Search Conditions)
    // =========================================================================

    /// <summary>
    /// 用戶 ID（可選）
    /// 
    /// 用途：
    /// - 查詢特定用戶的優惠券
    /// - NULL 表示不限制用戶（查詢所有優惠券）
    /// 
    /// 使用場景：
    /// - 用戶優惠券頁面（只顯示該用戶的優惠券）
    /// - 管理員查看特定用戶的優惠券
    /// 
    /// 注意事項：
    /// - 與 ScopeType、ScopeId 可以同時使用
    /// - 當 UserId 有值時，只查詢該用戶的優惠券
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// 適用範圍類型（可選）
    /// 
    /// 可選值：
    /// - "all"：全部商品
    /// - "product"：指定商品
    /// - "category"：指定類別
    /// - "brand"：指定品牌
    /// 
    /// NULL 表示不限制範圍類型
    /// 
    /// 使用場景：
    /// - 查詢特定範圍類型的優惠券
    /// - 篩選適用於特定商品/類別/品牌的優惠券
    /// 
    /// 注意事項：
    /// - 當 ScopeType 為 "all" 時，ScopeId 必須為 NULL
    /// - 當 ScopeType 不為 "all" 時，ScopeId 必須有值
    /// - 與 UserId 可以同時使用
    /// </summary>
    public string? ScopeType { get; init; }

    /// <summary>
    /// 適用範圍 ID（可選）
    /// 
    /// 用途：
    /// - 根據 ScopeType 對應到不同表的 ID
    /// - ScopeType = "product"：對應 Product.Id
    /// - ScopeType = "category"：對應 Category.Id
    /// - ScopeType = "brand"：對應 Brand.Id
    /// 
    /// NULL 表示不限制範圍 ID
    /// 
    /// 使用場景：
    /// - 查詢適用於特定商品的優惠券
    /// - 查詢適用於特定類別的優惠券
    /// - 查詢適用於特定品牌的優惠券
    /// 
    /// 注意事項：
    /// - 當 ScopeType = "all" 時，ScopeId 必須為 NULL
    /// - 當 ScopeType ≠ "all" 時，ScopeId 必須有值
    /// - 與 ScopeType 必須搭配使用才有意義
    /// </summary>
    public int? ScopeId { get; init; }

    // =========================================================================
    // 排序條件 (Sorting Conditions)
    // =========================================================================

    /// <summary>
    /// 排序欄位（可選）
    /// 
    /// 可選值：
    /// - "createdat"：按建立時間排序（預設）
    /// - "validfrom"：按有效開始時間排序
    /// - "validuntil"：按有效截止時間排序
    /// - "discountrate"：按折扣率排序
    /// 
    /// NULL 表示使用預設排序（createdat）
    /// 
    /// 使用場景：
    /// - 按建立時間查看最新優惠券
    /// - 按有效期查看即將過期的優惠券
    /// - 按折扣率查看折扣最大的優惠券
    /// 
    /// 注意事項：
    /// - 必須配合 SortDirection 使用
    /// - 欄位名稱不區分大小寫
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// 排序方向（可選）
    /// 
    /// 可選值：
    /// - "asc"：升序（由小到大）
    /// - "desc"：降序（由大到小）
    /// 
    /// NULL 表示使用預設排序方向（desc）
    /// 
    /// 使用場景：
    /// - 查看最新優惠券：SortBy="createdat", SortDirection="desc"
    /// - 查看即將過期的優惠券：SortBy="validuntil", SortDirection="asc"
    /// - 查看折扣最大的優惠券：SortBy="discountrate", SortDirection="desc"
    /// 
    /// 注意事項：
    /// - 必須配合 SortBy 使用
    /// - 值不區分大小寫
    /// </summary>
    public string? SortDirection { get; init; }

    // =========================================================================
    // 分頁條件 (Pagination Conditions)
    // =========================================================================

    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：實現游標分頁（Cursor-based Pagination）
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的游標值
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取游標值之後的資料
    /// 
    /// 游標值格式：
    /// - 根據 SortBy 欄位決定
    /// - SortBy="createdat"：CreatedAt 的值
    /// - SortBy="validfrom"：ValidFrom 的值
    /// - SortBy="validuntil"：ValidUntil 的值
    /// - SortBy="discountrate"：DiscountRate 的值
    /// 
    /// NULL 表示取得第一頁資料
    /// 
    /// 使用場景：
    /// - 無限滾動（Infinite Scroll）
    /// - 大數據量場景
    /// - 避免傳統分頁的性能問題
    /// 
    /// 注意事項：
    /// - 必須配合 SortBy 使用
    /// - 游標值必須來自上一頁最後一筆資料
    /// - 不支援跳頁（只能順序瀏覽）
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>
    /// 每頁資料筆數（可選）
    /// 
    /// 預設值：1000
    /// 最大值：1000
    /// 
    /// 設計考量：
    /// - 設定較大的預設值減少請求次數
    /// - 限制最大值防止一次性載入過多資料
    /// - 適合前端實作無限滾動（Infinite Scroll）
    /// 
    /// 使用建議：
    /// - 一般列表：使用預設值 1000
    /// - 行動裝置：可考慮降低至 50-100
    /// - 匯出功能：不應使用此參數
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 優惠券查詢處理器
/// 
/// 職責：
/// - 接收 CouponsQuery 請求
/// - 建構查詢條件（UserId、ScopeType、ScopeId）
/// - 建構排序條件（SortBy、SortDirection）
/// - 建構分頁條件（Cursor、Size）
/// - 從資料庫取得符合條件的優惠券
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CouponsQuery, IEnumerable<Coupon>> 介面
/// - 遵循單一職責原則（SRP）
/// - 使用依賴注入（DI）取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例（Transient）
/// 
/// 測試性：
/// - 可輕易 Mock ICouponRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - ProductsQueryHandler：相同的排序和分頁邏輯
/// - PromotionsQueryHandler：相同的查詢模式
/// </summary>
public class CouponsQueryHandler : IRequestHandler<CouponsQuery, Pagination<Coupon>>
{
    /// <summary>
    /// 優惠券倉儲介面
    /// 
    /// 用途：
    /// - 存取優惠券資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/CouponRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 繼承自 Repository<Coupon>，獲得通用 CRUD 功能
    /// </summary>
    private readonly ICouponRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">優惠券倉儲，用於查詢資料</param>
    public CouponsQueryHandler(ICouponRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理優惠券查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 回傳符合條件的優惠券集合
    /// 
    /// 查詢特性：
    /// - 支援用戶 ID 篩選
    /// - 支援範圍類型和範圍 ID 篩選
    /// - 支援彈性排序（多個欄位）
    /// - 使用游標分頁
    /// </summary>
    /// <param name="request">優惠券查詢請求物件，包含搜尋、排序、分頁參數</param>
    /// <returns>符合條件的優惠券集合</returns>
    public async Task<Pagination<Coupon>> HandleAsync(CouponsQuery request)
    {
        // 建構查詢條件
        var coupons = await _repository.GetAllAsync(BuildQuery(request));
    
        // 建構分頁結果
        return new Pagination<Coupon>(
            items: coupons,
            requestedSize: request.Size,
            cursorSelector: x =>
            {
                switch (request.SortBy?.ToLower())
                {
                    case "createdat":
                        return x.CreatedAt.ToString();
                    case "validfrom":
                        return x.ValidFrom.ToString();
                    case "validuntil":
                        return x.ValidUntil.ToString();
                    case "discountrate":
                        return x.DiscountAmount.ToString();
                    default:
                        return x.CreatedAt.ToString();
                }
            }
        );
    }

    /// <summary>
    /// 建構優惠券查詢表達式
    /// 
    /// 職責：
    /// - 將請求參數轉換為 LINQ 查詢表達式
    /// - 組合搜尋、排序、分頁條件
    /// 
    /// 設計優勢：
    /// - 將查詢邏輯集中管理
    /// - 保持 Repository 介面簡潔
    /// - 方便單元測試（可測試查詢邏輯）
    /// 
    /// 查詢流程：
    /// 1. 應用搜尋條件（UserId、ScopeType、ScopeId）
    /// 2. 應用排序條件（SortBy、SortDirection）
    /// 3. 應用分頁條件（Cursor、Size）
    /// </summary>
    /// <param name="request">優惠券查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Coupon>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Coupon>
    /// </returns>
    private static Func<IQueryable<Coupon>, IQueryable<Coupon>> BuildQuery(CouponsQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用搜尋條件 =====
            
            // 1.1 應用 UserId 篩選
            if (request.UserId != null)
            {
                // 只查詢指定用戶的優惠券
                query = query.Where(x => x.UserId == request.UserId);
            }

            // 1.2 應用 ScopeType 和 ScopeId 篩選
            if (!string.IsNullOrWhiteSpace(request.ScopeType))
            {
                // 根據 ScopeType 篩選優惠券
                query = query.Where(x => x.ScopeType == request.ScopeType.ToLower());

                // 當 ScopeType 不為 "all" 時，必須提供 ScopeId
                if (request.ScopeType.ToLower() != "all")
                {
                    if (request.ScopeId == null)
                        throw new ArgumentException("當 ScopeType 不為 'all' 時，必須提供 ScopeId");
                    
                    // 根據 ScopeId 篩選優惠券
                    query = query.Where(x => x.ScopeId == request.ScopeId);
                }
                else
                {
                    // 當 ScopeType 為 "all" 時，ScopeId 必須為 NULL
                    if (request.ScopeId != null)
                        throw new ArgumentException("當 ScopeType 為 'all' 時，ScopeId 必須為 NULL");
                    
                    // 只查詢 ScopeType 為 "all" 的優惠券
                    query = query.Where(x => x.ScopeId == null);
                }
            }

            // ===== 第二階段：應用排序條件 =====
            
            // 2.1 決定排序欄位（預設為 CreatedAt）
            var sortBy = request.SortBy?.ToLower() ?? "createdat";
            
            // 2.2 決定排序方向（預設為降序）
            var isDescending = request.SortDirection?.ToLower() != "asc";

            // 2.3 根據 SortBy 應用排序
            switch (sortBy)
            {
                case "validfrom":
                    query = isDescending 
                        ? query.OrderByDescending(x => x.ValidFrom)
                        : query.OrderBy(x => x.ValidFrom);
                    break;
                    
                case "validuntil":
                    query = isDescending 
                        ? query.OrderByDescending(x => x.ValidUntil)
                        : query.OrderBy(x => x.ValidUntil);
                    break;
                    
                case "discountrate":
                    query = isDescending 
                        ? query.OrderByDescending(x => x.DiscountAmount)
                        : query.OrderBy(x => x.DiscountAmount);
                    break;
                    
                case "createdat":
                default:
                    query = isDescending 
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt);
                    break;
            }

            // ===== 第三階段：應用分頁條件 =====
            
            // 3.1 應用游標分頁
            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                // 根據 SortBy 決定游標欄位
                switch (sortBy)
                {
                    case "validfrom":
                        if (DateTimeOffset.TryParse(request.Cursor, out var validFromCursor))
                        {
                            query = isDescending
                                ? query.Where(x => x.ValidFrom < validFromCursor)
                                : query.Where(x => x.ValidFrom > validFromCursor);
                        }
                        break;
                        
                    case "validuntil":
                        if (DateTimeOffset.TryParse(request.Cursor, out var validUntilCursor))
                        {
                            query = isDescending
                                ? query.Where(x => x.ValidUntil < validUntilCursor)
                                : query.Where(x => x.ValidUntil > validUntilCursor);
                        }
                        break;
                        
                    case "discountrate":
                        if (decimal.TryParse(request.Cursor, out var discountRateCursor))
                        {
                            query = isDescending
                                ? query.Where(x => x.DiscountAmount < discountRateCursor)
                                : query.Where(x => x.DiscountAmount > discountRateCursor);
                        }
                        break;
                        
                    case "createdat":
                    default:
                        if (DateTimeOffset.TryParse(request.Cursor, out var createdAtCursor))
                        {
                            query = isDescending
                                ? query.Where(x => x.CreatedAt < createdAtCursor)
                                : query.Where(x => x.CreatedAt > createdAtCursor);
                        }
                        break;
                }
            }

            // 3.2 限制回傳筆數
            if (request.Size != null)
            {
                query = query.Take(request.Size.Value);
            }

            // 回傳最終組合好的查詢表達式
            // 注意：此時還沒真正執行查詢，要到被 foreach 或 ToList() 時才會執行
            return query;
        };
    }
}
