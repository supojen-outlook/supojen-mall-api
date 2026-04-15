using System;
using Manian.Application.Models;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Promotions;

/// <summary>
/// 查詢促銷活動列表的請求物件
/// 
/// 用途：
/// - 取得系統中的促銷活動列表
/// - 支援搜尋、排序、分頁功能
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Promotion>>，表示這是一個查詢請求
/// - 回傳促銷活動實體集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PromotionsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 促銷活動管理頁面
/// - 促銷活動搜尋功能
/// - 促銷活動統計報表
/// 
/// 設計特點：
/// - 支援關鍵字搜尋
/// - 支援彈性排序（多個欄位）
/// - 使用游標分頁（Cursor-based Pagination）
/// 
/// 參考實作：
/// - ProductsQuery：相同的排序和分頁實作
/// - CouponsQuery：相同的查詢模式
/// </summary>
public class PromotionsQuery : IRequest<Pagination<Promotion>>
{
    // =========================================================================
    // 搜尋條件 (Search Conditions)
    // =========================================================================

    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 搜尋範圍：
    /// - 促銷活動名稱 (Name)
    /// - 促銷活動描述 (Description)
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫（查詢時會轉為小寫）
    /// - 支援模糊搜尋（使用 Contains）
    /// - 會自動去除前後空白
    /// 
    /// 使用範例：
    /// - "雙11" → 會找到名稱或描述中包含 "雙11" 的促銷活動
    /// - "雙11" → 同樣會找到（不區分大小寫）
    /// </summary>
    public string? Search { get; init; }

    // =========================================================================
    // 排序條件 (Sorting Conditions)
    // =========================================================================

    /// <summary>
    /// 排序欄位（可選）
    /// 
    /// 可選值：
    /// - "createdat"：按建立時間排序（預設）
    /// - "startdate"：按活動開始時間排序
    /// - "enddate"：按活動結束時間排序
    /// 
    /// NULL 表示使用預設排序（createdat）
    /// 
    /// 使用場景：
    /// - 按建立時間查看最新促銷活動
    /// - 按開始時間查看即將開始的促銷活動
    /// - 按結束時間查看即將結束的促銷活動
    /// 
    /// 注意事項：
    /// - 必須配合 SortDirection 使用
    /// - 欄位名稱不區分大小寫
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 排序方向（可選）
    /// 
    /// 可選值：
    /// - "asc"：升序（由小到大）
    /// - "desc"：降序（由大到小）
    /// 
    /// 預設值："desc"
    /// 
    /// 使用場景：
    /// - 查看最新促銷活動：SortBy="createdat", SortDirection="desc"
    /// - 查看即將開始的促銷活動：SortBy="startdate", SortDirection="asc"
    /// - 查看即將結束的促銷活動：SortBy="enddate", SortDirection="asc"
    /// 
    /// 注意事項：
    /// - 必須配合 SortBy 使用
    /// - 值不區分大小寫
    /// </summary>
    public string? SortDirection { get; init; } = "desc";

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
    /// - SortBy="startdate"：StartDate 的值
    /// - SortBy="enddate"：EndDate 的值
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
    public string? Cursor { get; set; }

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
/// 促銷活動查詢處理器
/// 
/// 職責：
/// - 接收 PromotionsQuery 請求
/// - 建構查詢條件（搜尋、排序、分頁）
/// - 從資料庫取得符合條件的促銷活動
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PromotionsQuery, IEnumerable<Promotion>> 介面
/// - 遵循單一職責原則（SRP）
/// - 使用依賴注入（DI）取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例（Transient）
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - ProductsQueryHandler：相同的排序和分頁邏輯
/// - CouponsQueryHandler：相同的查詢模式
/// </summary>
public class PromotionsQueryHandler : IRequestHandler<PromotionsQuery, Pagination<Promotion>>
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
    /// - 繼承自 Repository<Promotion>，獲得通用 CRUD 功能
    /// </summary>
    private readonly IPromotionRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢資料</param>
    public PromotionsQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理促銷活動查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 回傳符合條件的促銷活動集合
    /// 
    /// 查詢特性：
    /// - 支援關鍵字搜尋
    /// - 支援彈性排序（多個欄位）
    /// - 使用游標分頁
    /// </summary>
    /// <param name="request">促銷活動查詢請求物件，包含搜尋、排序、分頁參數</param>
    /// <returns>符合條件的促銷活動集合</returns>
    public async Task<Pagination<Promotion>> HandleAsync(PromotionsQuery request)
    {
        // 建構查詢條件
        var promotions = await _repository.GetAllAsync(BuildQuery(request));
    
        // 回傳分頁結果
        return new Pagination<Promotion>(
            items: promotions,
            requestedSize: request.Size,
            cursorSelector: x =>
            {
                switch (request.SortBy?.ToLower())
                {
                    case "startdate":
                        return x.StartDate.ToString();
                    case "enddate":
                        return x.EndDate.ToString();
                    default:
                        return x.CreatedAt.ToString();
                }
            }
        );
    }

    /// <summary>
    /// 建構促銷活動查詢表達式
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
    /// 1. 應用搜尋條件（SearchTerm）
    /// 2. 應用排序條件（SortBy、SortDirection）
    /// 3. 應用分頁條件（Cursor、Size）
    /// </summary>
    /// <param name="request">促銷活動查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Promotion>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Promotion>
    /// </returns>
    private static Func<IQueryable<Promotion>, IQueryable<Promotion>> BuildQuery(PromotionsQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用搜尋條件 =====
            
            // 1.1 應用 SearchTerm 搜尋
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                // 去除前後空白並轉為小寫
                var searchTerm = request.Search.Trim().ToLower();
                
                // 在 Name 和 Description 兩個欄位中進行模糊搜尋
                // 使用 OR 邏輯，只要任一欄位包含關鍵字即符合
                query = query.Where(x => 
                    x.Name.ToLower().Contains(searchTerm) || 
                    (x.Description != null && x.Description.ToLower().Contains(searchTerm)));
            }

            // ===== 第二階段：應用排序條件 =====
            
            // 2.1 決定排序欄位（預設為 CreatedAt）
            var sortBy = request.SortBy?.ToLower() ?? "createdat";
            
            // 2.2 決定排序方向（預設為降序）
            var isDescending = request.SortDirection?.ToLower() != "asc";

            // 2.3 根據 SortBy 應用排序
            switch (sortBy)
            {
                case "startdate":
                    query = isDescending 
                        ? query.OrderByDescending(x => x.StartDate)
                        : query.OrderBy(x => x.StartDate);
                    break;
                    
                case "enddate":
                    query = isDescending 
                        ? query.OrderByDescending(x => x.EndDate)
                        : query.OrderBy(x => x.EndDate);
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
                    case "startdate":
                        if (DateTimeOffset.TryParse(request.Cursor, out var startDateCursor))
                        {
                            query = isDescending
                                ? query.Where(x => x.StartDate < startDateCursor)
                                : query.Where(x => x.StartDate > startDateCursor);
                        }
                        break;
                        
                    case "enddate":
                        if (DateTimeOffset.TryParse(request.Cursor, out var endDateCursor))
                        {
                            query = isDescending
                                ? query.Where(x => x.EndDate < endDateCursor)
                                : query.Where(x => x.EndDate > endDateCursor);
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
