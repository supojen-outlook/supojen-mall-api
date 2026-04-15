using Manian.Application.Models;
using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Warehouses;

/// <summary>
/// 查詢儲位列表的請求物件
/// 
/// 用途：
/// - 查詢倉庫中的儲位資料
/// - 支援多種篩選條件和分頁功能
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Location>>，表示這是一個查詢請求
/// - 回傳 Pagination<Location> 包含資料列表和游標
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 LocationsQueryHandler 配合使用，完成查詢
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination (基於 SortOrder 的游標分頁)
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 Cursor 參數記錄上一頁最後一筆的 SortOrder
/// 
/// 使用場景：
/// - 儲位管理介面
/// - 儲位選擇器
/// - 儲位報表
/// </summary>
public class LocationsQuery : IRequest<Pagination<Location>>
{
    /// <summary>
    /// 父位置 ID（可選）
    /// 
    /// 用途：
    /// - 篩選指定父位置下的子位置
    /// - NULL 表示查詢所有位置（不限層級）
    /// 
    /// 使用範例：
    /// - null：查詢所有儲位
    /// - 1：查詢 ID 為 1 的儲位下的所有子儲位
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// 狀態（可選）
    /// 
    /// 可選值：
    /// - "active"：啟用狀態
    /// - "inactive"：停用狀態
    /// - "maintenance"：維護中
    /// 
    /// 預設行為：
    /// - NULL 表示不篩選狀態（回傳所有狀態的儲位）
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 搜尋範圍：
    /// - 儲位名稱 (Name)
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫（查詢時會轉為小寫）
    /// - 支援模糊搜尋（使用 Contains）
    /// - 會自動去除前後空白
    /// 
    /// 使用範例：
    /// - "A區" → 會找到名稱中包含 "A區" 的儲位
    /// - "A01" → 會找到名稱中包含 "A01" 的儲位
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 位置類型（可選）
    /// 
    /// 可選值：
    /// - "ZONE"：區域（第一層級）
    /// - "BIN"：儲位（第二層級）
    /// - "INTERNAL"：虛擬位置
    /// 
    /// 預設行為：
    /// - NULL 表示不篩選位置類型
    /// </summary>
    public string? LocationType { get; init; }

    /// <summary>
    /// 區域類型（可選）
    /// 
    /// 可選值：
    /// - "RECEIVING"：收貨區
    /// - "STORAGE"：儲存區
    /// - "PICKING"：揀貨區
    /// - "PACKING"：包裝區
    /// - "SHIPPING"：出貨區
    /// - "QA"：品檢區
    /// - "RETURNING"：退貨區
    /// 
    /// 預設行為：
    /// - NULL 表示不篩選區域類型
    /// </summary>
    public string? ZoneType { get; init; }

    /// <summary>
    /// 上一頁最後一筆資料的 SortOrder（游標）
    /// 
    /// 用途：實現 Keyset Pagination（游標分頁）
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 SortOrder
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 SortOrder 大於此值的資料
    /// 
    /// 為什麼用 SortOrder 而不是 Id？
    /// - SortOrder 更有業務意義（按排序順序）
    /// - 避免暴露內部 Id 給前端
    /// - 支援自訂排序順序
    /// 
    /// 注意事項：
    /// - 首次查詢時應傳 null（取得第一頁）
    /// - 必須配合 OrderBy(SortOrder) 使用
    /// - 實作中會轉換為整數進行比較
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

    /// <summary>
    /// 樓層（可選）
    /// 
    /// 用途：
    /// - 篩選指定樓層的儲位
    /// - NULL 表示不篩選樓層
    ///     
    /// 使用範例：
    /// - 1：查詢第 1 樓的儲位
    /// - 2：查詢第 2 樓的儲位
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// 儲位 ID（可選）
    /// 
    /// 用途：
    /// - 篩選指定 ID 的儲位
    /// - NULL 表示不篩選 ID
    ///     
    /// 使用範例：
    /// - 1：查詢 ID 為 1 的儲位
    /// - 2：查詢 ID 為 2 的儲位
    /// </summary>
    public int[] Ids { get; set; }
}

/// <summary>
/// 儲位查詢處理器
/// 
/// 職責：
/// - 接收 LocationsQuery 請求
/// - 建構查詢條件（搜尋、篩選、分頁）
/// - 從資料庫取得符合條件的儲位
/// - 回傳 Pagination<Location> 包含資料列表和游標
/// 
/// 設計模式：
/// - 實作 IRequestHandler<LocationsQuery, Pagination<Location>> 介面
/// - 遵循單一職責原則（SRP）
/// - 使用依賴注入（DI）取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例
/// 
/// 測試性：
/// - 可輕易 Mock ILocationRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
internal class LocationsQueryHandler : IRequestHandler<LocationsQuery, Pagination<Location>>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取儲位資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢資料</param>
    public LocationsQueryHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理儲位查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 將結果封裝為 Pagination 物件
    /// 
    /// 查詢特性：
    /// - 支援多欄位模糊搜尋
    /// - 使用 Keyset Pagination 分頁
    /// - 按 SortOrder 升序排列
    /// </summary>
    /// <param name="request">儲位查詢請求物件，包含搜尋和分頁參數</param>
    /// <returns>包含資料列表和游標的 Pagination<Location></returns>
    public async Task<Pagination<Location>> HandleAsync(LocationsQuery request)
    {
        // 呼叫 BuildQuery 建構查詢條件，然後傳給 Repository 執行
        var locations = await _repository.GetAllAsync(BuildQuery(request));
    
        // 建立回傳物件
        // 使用建構函式自動計算 Cursor，無需手動判斷
        // cursorSelector：指定使用 SortOrder 屬性作為下一頁的游標
        return new Pagination<Location>(
            items: locations,
            requestedSize: request.Size,
            cursorSelector: l => l.SortOrder.ToString()
        );
    }

    /// <summary>
    /// 建構儲位查詢表達式
    /// 
    /// 職責：
    /// - 將請求參數轉換為 LINQ 查詢表達式
    /// - 組合搜尋、篩選、排序、分頁條件
    /// 
    /// 設計優勢：
    /// - 將查詢邏輯集中管理
    /// - 保持 Repository 介面簡潔
    /// - 方便單元測試（可測試查詢邏輯）
    /// 
    /// 查詢流程：
    /// 1. 應用搜尋條件（如果有 Search）
    /// 2. 應用位置類型條件（如果有 LocationType）
    /// 3. 應用父位置 ID 條件（如果有 ParentId）
    /// 4. 應用區域類型條件（如果有 ZoneType）
    /// 5. 應用狀態條件（如果有 Status）
    /// 6. 應用分頁條件（如果有 Cursor）
    /// 7. 按 SortOrder 升序排序
    /// 8. 限制回傳筆數（如果有 Size）
    /// </summary>
    /// <param name="request">儲位查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Location>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Location>
    /// </returns>
    private static Func<IQueryable<Location>, IQueryable<Location>> BuildQuery(LocationsQuery request)
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

            // ===== 第二階段：應用位置類型條件 =====
            if (!string.IsNullOrEmpty(request.LocationType))
            {
                query = query.Where(l => l.LocationType == request.LocationType);   
            }

            // ===== 第三階段：應用父位置 ID 條件 =====
            if (request.ParentId != null)
            {
                // 篩選指定父位置下的子位置
                query = query.Where(c => c.ParentId == request.ParentId);
            }

            // ===== 第四階段：應用區域類型條件 =====
            if (!string.IsNullOrEmpty(request.ZoneType))
            {
                query = query.Where(l => l.ZoneType == request.ZoneType);   
            }

            // ===== 第五階段：應用狀態條件 =====
            if (!string.IsNullOrEmpty(request.Status))
            {
                // 篩選特定狀態的儲位
                query = query.Where(c => c.Status == request.Status);
            }

            // ===== 第六階段：應用層級條件 =====
            if (request.Level != null)
            {
                query = query.Where(c => c.Level == request.Level);
            }

            // ===== 第七階段：應用 ID 條件 =====
            if(request.Ids != null && request.Ids.Any())
            {
                query = query.Where(c => request.Ids.Contains(c.Id));
            }

            // ===== 第八階段：應用分頁條件 =====
            // Keyset Pagination：只取 SortOrder 大於上一頁最後一筆的資料
            if (request.Cursor != null)
            {
                // 將 Cursor 轉換為整數
                // 然後篩選 SortOrder 大於此值的儲位
                // 若轉換失敗（格式錯誤），則忽略 Cursor 條件（回傳第一頁）
                if (int.TryParse(request.Cursor, out var sortOrderCursor))
                {
                    query = query.Where(x => x.SortOrder > sortOrderCursor);
                }
            }
            
            // 按 SortOrder 升序排列（數字越小越前面）
            query = query.OrderBy(c => c.SortOrder);

            // ===== 第九階段：限制回傳筆數 =====
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
