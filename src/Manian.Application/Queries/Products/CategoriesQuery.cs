using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Application.Models;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢產品類別列表的請求物件
/// 
/// 用途：
/// - 取得符合條件的產品類別列表
/// - 支援多種篩選條件（父類別、層級、狀態等）
/// - 支援關鍵字搜尋
/// - 支援游標分頁（Cursor-based Pagination）
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Category>>，表示這是一個查詢請求
/// - 回傳 Pagination<Category> 包含資料列表和游標
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CategoriesQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 類別樹狀結構瀏覽
/// - 類別下拉選單
/// - 類別管理介面
/// 
/// 分頁策略：
/// - 使用游標分頁（Cursor-based Pagination）
/// - 基於 SortOrder 進行分頁
/// - 比傳統 Skip/Take 更穩定，適合大數據量場景
/// </summary>
public class CategoriesQuery : IRequest<Pagination<Category>>
{
    /// <summary>
    /// 父類別 ID（可選）
    /// 
    /// 用途：
    /// - 篩選指定父類別下的子類別
    /// - 若為 null，則查詢所有類別（不限制父類別）
    /// 
    /// 使用範例：
    /// - ParentId = 1：查詢 ID 為 1 的類別下的所有子類別
    /// - ParentId = null：查詢所有類別
    /// 
    /// 注意事項：
    /// - 與 Level 參數配合使用可精確定位特定層級的類別
    /// - 根類別的 ParentId 為 null
    /// </summary>
    public long? ParentId { get; init; }

    /// <summary>
    /// 類別層級（可選）
    /// 
    /// 用途：
    /// - 篩選特定層級的類別
    /// - 根類別為 Level 1，子類別遞增
    /// 
    /// 使用範例：
    /// - Level = 1：查詢所有根類別
    /// - Level = 2：查詢所有第二層類別
    /// 
    /// 注意事項：
    /// - 與 ParentId 配合使用可精確定位特定父類別下的特定層級
    /// - 由資料庫觸發器自動維護
    /// </summary>
    public int? Level { get; init; }

    /// <summary>
    /// 類別狀態（可選）
    /// 
    /// 用途：
    /// - 篩選特定狀態的類別
    /// 
    /// 可選值：
    /// - "active"：啟用狀態（預設）
    /// - "inactive"：停用狀態
    /// 
    /// 使用範例：
    /// - Status = "active"：只查詢啟用的類別
    /// - Status = null：不限制狀態
    /// 
    /// 注意事項：
    /// - 見 Category 實體的 Status 屬性說明
    /// - 只能接受 "active" 或 "inactive" 兩個值
    /// </summary>
    public string? Status { get; init; }
    
    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 用途：
    /// - 在類別名稱中進行模糊搜尋
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫（查詢時會轉為小寫）
    /// - 支援模糊搜尋（使用 Contains）
    /// - 會自動去除前後空白
    /// 
    /// 使用範例：
    /// - Search = "手機"：會找到名稱包含 "手機" 的類別
    /// - Search = "PHONE"：同樣會找到（不區分大小寫）
    /// 
    /// 注意事項：
    /// - 目前只在 Name 欄位搜尋
    /// - 未來可擴展到 Slug、Description 等欄位
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 是否為葉節點（可選）
    /// 
    /// 用途：
    /// - 篩選是否為葉節點（沒有子類別的類別）
    /// 
    /// 使用範例：
    /// - IsLeaf = true：只查詢葉節點（最底層類別）
    /// - IsLeaf = false：只查詢非葉節點（有子類別的類別）
    /// - IsLeaf = null：不限制
    /// 
    /// 注意事項：
    /// - 由資料庫觸發器自動維護
    /// - 葉節點通常用於顯示商品
    /// - 非葉節點通常用於分類導航
    /// </summary>
    public bool? IsLeaf { get; set; }
    
    /// <summary>
    /// 上一頁最後一筆資料的 SortOrder（游標）
    /// 
    /// 用途：
    /// - 實現游標分頁（Cursor-based Pagination）
    /// - 記錄上一頁最後一筆資料的 SortOrder
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 SortOrder
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 SortOrder 大於此值的資料
    /// 
    /// 使用範例：
    /// - 首次查詢：Cursor = null（取得第一頁）
    /// - 第二頁：Cursor = "100"（取得 SortOrder > 100 的資料）
    /// 
    /// 注意事項：
    /// - 必須配合 OrderBy(SortOrder) 使用
    /// - 與 LastCreatedAt 配合可實現更穩定的分頁
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
    /// 
    /// 注意事項：
    /// - Size = null 表示不限制筆數（不建議）
    /// - 超過 1000 時應在後端驗證並限制
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 產品類別查詢處理器
/// 
/// 職責：
/// - 接收 CategoriesQuery 請求
/// - 建構查詢條件（篩選、排序、分頁）
/// - 從資料庫取得符合條件的產品類別
/// - 回傳 Pagination<Category> 包含資料列表和游標
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoriesQuery, Pagination<Category>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICategoryRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
public class CategoriesQueryHandler : IRequestHandler<CategoriesQuery, Pagination<Category>>
{
    /// <summary>
    /// 產品類別倉儲介面
    /// 
    /// 用途：
    /// - 存取產品類別資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/CategoryRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">產品類別倉儲，用於查詢資料</param>
    public CategoriesQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理產品類別查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 將結果轉換為 Pagination<Category>
    /// 4. 設定游標為最後一筆資料的 SortOrder
    /// 5. 回傳 Pagination<Category>
    /// 
    /// 查詢特性：
    /// - 支援多條件篩選（父類別、層級、狀態等）
    /// - 支援關鍵字搜尋
    /// - 使用游標分頁
    /// - 按 SortOrder 排序
    /// </summary>
    /// <param name="request">產品類別查詢請求物件，包含篩選和分頁參數</param>
    /// <returns>包含資料列表和游標的 Pagination<Category></returns>
    public async Task<Pagination<Category>> HandleAsync(CategoriesQuery request)
    {
        // 呼叫 BuildQuery 建構查詢條件，然後傳給 Repository 執行
        var categories = await _repository.GetAllAsync(BuildQuery(request));
                
        // 建立回傳物件
        var result = new Pagination<Category>(
            items: categories,
            requestedSize: request.Size,
            cursorSelector: c => c.SortOrder.ToString()
        );
        
        return result;
    }

    /// <summary>
    /// 建構產品類別查詢表達式
    /// 
    /// 職責：
    /// - 將請求參數轉換為 LINQ 查詢表達式
    /// - 組合篩選、排序、分頁條件
    /// 
    /// 設計優勢：
    /// - 將查詢邏輯集中管理
    /// - 保持 Repository 介面簡潔
    /// - 方便單元測試（可測試查詢邏輯）
    /// 
    /// 查詢流程：
    /// 1. 應用搜尋條件（如果有 Search）
    /// 2. 應用葉節點條件（如果有 IsLeaf）
    /// 3. 應用父類別 ID 條件（如果有 ParentId）
    /// 4. 應用層級條件（如果有 Level）
    /// 5. 應用狀態條件（如果有 Status）
    /// 6. 應用分頁條件（如果有 Cursor）
    /// 7. 按 SortOrder 排序
    /// 8. 限制回傳筆數（如果有 Size）
    /// </summary>
    /// <param name="request">產品類別查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Category>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Category>
    /// </returns>
    private static Func<IQueryable<Category>, IQueryable<Category>> BuildQuery(CategoriesQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用搜尋條件 =====
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                // 1. 去除前後空白並轉為小寫
                var searchTerm = request.Search.Trim().ToLower();
                
                // 2. 在 Name 欄位中進行模糊搜尋
                //    ToLower() 確保不區分大小寫
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
            }

            // ===== 第二階段：應用葉節點條件 =====
            if (request.IsLeaf != null)
            {
                // 篩選葉節點或非葉節點
                query = query.Where(c => c.IsLeaf == request.IsLeaf.Value);
            }

            // ===== 第三階段：應用父類別 ID 條件 =====
            if (request.ParentId != null)
            {
                // 篩選指定父類別下的子類別
                query = query.Where(c => c.ParentId == request.ParentId);
            }

            // ===== 第四階段：應用層級條件 =====
            if (request.Level != null)
            {
                // 篩選特定層級的類別
                query = query.Where(c => c.Level == request.Level);
            }

            // ===== 第五階段：應用狀態條件 =====
            if (!string.IsNullOrEmpty(request.Status))
            {
                // 篩選特定狀態的類別
                query = query.Where(c => c.Status == request.Status);
            }

            // ===== 第六階段：應用分頁條件 =====
            // 注意：這裡只使用了 Cursor
            if (request.Cursor != null)
            {
                // 只取 SortOrder 大於 Cursor 的資料
                // int.Parse 可能會拋出例外，建議加入錯誤處理
                query = query.Where(x => x.SortOrder > int.Parse(request.Cursor));
            }
            
            // 按 SortOrder 升序排列
            query = query.OrderBy(c => c.SortOrder);

            // ===== 第七階段：限制回傳筆數 =====
            if (request.Size != null)
            {
                // 只取前 Size 筆資料
                query = query.Take(request.Size.Value);
            }

            // 回傳最終組合好的查詢表達式
            // 注意：此時還沒真正執行查詢，要到被 foreach 或 ToList() 時才會執行
            return query;
        };
    }
}
