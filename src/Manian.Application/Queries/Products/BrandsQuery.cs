using Manian.Application.Models;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢品牌列表的請求物件
/// 
/// 用途：
/// - 查詢符合條件的品牌列表
/// - 支援多種篩選條件（父品牌、狀態、葉節點等）
/// - 支援關鍵字搜尋
/// - 支援游標分頁（Keyset Pagination）
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<Brand>>，表示這是一個查詢請求
/// - 回傳品牌集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 BrandsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 品牌管理頁面
/// - 品牌下拉選單
/// - 品牌樹狀結構顯示
/// - 品牌統計報表
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination（基於 SortOrder 的游標分頁）
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 Cursor 參數記錄上一頁最後一筆的 SortOrder
/// 
/// 注意事項：
/// - Search 只搜尋品牌名稱（不含代碼）
/// - ParentId 篩選時會排除 null 值（只查詢有父品牌的品牌）
/// - Size 預設值為 1000，最大值為 1000
/// 
/// 參考實作：
/// - CategoriesQuery：類似的實作模式，用於查詢類別列表
/// - RolesQuery：類似的實作模式，用於查詢角色列表
/// </summary>
public class BrandsQuery : IRequest<Pagination<Brand>>
{
    /// <summary>
    /// 父品牌 ID（可選）
    /// 
    /// 用途：
    /// - 篩選指定父品牌下的子品牌
    /// - 用於建立品牌層級結構
    /// 
    /// 使用場景：
    /// - 查詢某個品牌下的所有子品牌
    /// - 建立品牌樹狀結構
    /// - 麵包屑導航
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 ParentId 等於此值的品牌
    /// - 如果為 null，回傳所有品牌（不篩選父品牌）
    /// - 注意：實作中會排除 ParentId 為 null 的品牌（只查詢有父品牌的品牌）
    /// 
    /// 範例：
    /// - ParentId = 5：查詢品牌 5 下的所有子品牌
    /// - ParentId = null：不篩選父品牌
    /// </summary>
    public long? ParentId { get; init; }

    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 用途：
    /// - 在品牌名稱中搜尋包含關鍵字的品牌
    /// - 支援模糊搜尋
    /// 
    /// 搜尋範圍：
    /// - 品牌名稱（Name）
    /// - 注意：目前實作不搜尋品牌代碼（Code）
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫（查詢時會轉為小寫）
    /// - 支援模糊搜尋（使用 Contains）
    /// - 會自動去除前後空白
    /// 
    /// 使用範例：
    /// - "Nike" → 會找到名稱中包含 "Nike" 的品牌
    /// - "nike" → 同樣會找到（不區分大小寫）
    /// - " Nike " → 會自動去除前後空白後搜尋
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 品牌狀態（可選）
    /// 
    /// 用途：
    /// - 篩選指定狀態的品牌
    /// 
    /// 可選值：
    /// - "active"：啟用狀態
    /// - "inactive"：停用狀態
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 Status 等於此值的品牌
    /// - 如果為 null，回傳所有狀態的品牌（不篩選狀態）
    /// 
    /// 使用場景：
    /// - 只顯示啟用的品牌（Status = "active"）
    /// - 查詢已停用的品牌（Status = "inactive"）
    /// 
    /// 範例：
    /// - Status = "active"：只回傳啟用的品牌
    /// - Status = null：回傳所有狀態的品牌
    /// </summary>
    public string? Status { get; init; }
    
    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 實現 Keyset Pagination（游標分頁）
    /// - 記錄上一頁最後一筆資料的 SortOrder
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
    /// - 必須配合 OrderBy 使用
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
    /// 
    /// 範例：
    /// - Size = 10：每頁回傳 10 筆資料
    /// - Size = null：回傳所有符合條件的資料（不限制筆數）
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// 是否為葉節點（可選）
    /// 
    /// 用途：
    /// - 篩選是否為葉節點的品牌
    /// 
    /// 定義：
    /// - true：葉節點（沒有子品牌）
    /// - false：非葉節點（有子品牌）
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 IsLeaf 等於此值的品牌
    /// - 如果為 null，回傳所有品牌（不篩選是否為葉節點）
    /// 
    /// 使用場景：
    /// - 只顯示終端品牌（IsLeaf = true）
    /// - 只顯示分類品牌（IsLeaf = false）
    /// - 建立品牌樹狀結構
    /// 
    /// 範例：
    /// - IsLeaf = true：只回傳葉節點品牌
    /// - IsLeaf = false：只回傳非葉節點品牌
    /// - IsLeaf = null：回傳所有品牌
    /// </summary>
    public bool? IsLeaf { get; set; }


    /// <summary>
    /// 品牌層級（可選）
    /// 
    /// 用途：
    /// - 篩選指定層級的品牌
    /// 
    /// 使用場景：
    /// - 查詢指定層級的品牌
    /// - 建立品牌樹狀結構
    ///     
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 Level 等於此值的品牌
    /// - 如果為 null，回傳所有品牌（不篩選層級）
    ///     
    /// 範例：
    /// - Level = 1：只回傳第一層品牌
    /// - Level = null：回傳所有品牌
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// 品牌 ID 列表（可選）
    /// 
    /// 用途：
    /// - 篩選指定的品牌
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 ID 在此列表中的品牌
    /// - 如果為 null 或空陣列，不進行 ID 篩選
    /// 
    /// 使用場景：
    /// - 查詢多個指定的品牌
    /// - 根據品牌路徑快取查詢品牌
    /// - 批次查詢品牌資訊
    /// 
    /// 範例：
    /// - Ids = [1, 5, 8]：只回傳 ID 為 1、5、8 的品牌
    /// - Ids = null：不進行 ID 篩選
    /// - Ids = []：不進行 ID 篩選（空陣列）
    /// </summary>
    public int[]? Ids { get; set; }
}

/// <summary>
/// 品牌查詢處理器
/// 
/// 職責：
/// - 接收 BrandsQuery 請求
/// - 建構查詢條件（搜尋、篩選、分頁）
/// - 從資料庫取得符合條件的品牌
/// 
/// 設計模式：
/// - 實作 IRequestHandler<BrandsQuery, IEnumerable<Brand>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例（Transient）
/// 
/// 測試性：
/// - 可輕易 Mock IBrandRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - Search 條件重複執行兩次（程式碼重複）
/// - 未使用 Ids 參數進行篩選
/// - ParentId 篩選邏輯可能不符合預期（排除 null）
/// </summary>
public class BrandsQueryHandler : IRequestHandler<BrandsQuery, Pagination<Brand>>
{
    /// <summary>
    /// 品牌倉儲介面
    /// 
    /// 用途：
    /// - 存取品牌資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/BrandRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly IBrandRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">品牌倉儲，用於查詢資料</param>
    public BrandsQueryHandler(IBrandRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理品牌查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 回傳符合條件的品牌集合
    /// 
    /// 查詢特性：
    /// - 支援多條件組合篩選
    /// - 支援關鍵字搜尋
    /// - 使用 Keyset Pagination 分頁
    /// - 按 SortOrder 排序
    /// </summary>
    /// <param name="request">品牌查詢請求物件，包含搜尋、篩選、分頁參數</param>
    /// <returns>符合條件的品牌集合</returns>
    public async Task<Pagination<Brand>> HandleAsync(BrandsQuery request)
    {
        // 呼叫 BuildQuery 建構查詢條件，然後傳給 Repository 執行
        var brands = await _repository.GetAllAsync(BuildQuery(request));
    
        return new Pagination<Brand>(
            items: brands,
            requestedSize: request.Size,
            cursorSelector: b => b.SortOrder.ToString()
        );
    }

    /// <summary>
    /// 建構品牌查詢表達式
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
    /// 2. 應用葉節點篩選（如果有 IsLeaf）
    /// 3. 應用父品牌篩選（如果有 ParentId）
    /// 4. 應用狀態篩選（如果有 Status）
    /// 5. 應用分頁條件（如果有 Cursor）
    /// 6. 按 SortOrder 排序
    /// 7. 限制回傳筆數（如果有 Size）
    /// </summary>
    /// <param name="request">品牌查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Brand>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Brand>
    /// </returns>
    private static Func<IQueryable<Brand>, IQueryable<Brand>> BuildQuery(BrandsQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用搜尋條件 =====
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                // 1. 去除前後空白並轉為小寫
                var searchTerm = request.Search.Trim().ToLower();
                
                // 2. 在品牌名稱中進行模糊搜尋
                //    使用 Contains 進行模糊匹配
                //    ToLower() 確保不區分大小寫
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
            }

            // ===== 第二階段：應用葉節點篩選 =====
            if (request.IsLeaf != null)
            {
                // 篩選是否為葉節點
                // true：只回傳葉節點（沒有子品牌）
                // false：只回傳非葉節點（有子品牌）
                query = query.Where(c => c.IsLeaf == request.IsLeaf.Value);
            }

            // ===== 第三階段：應用層級篩選 =====
            if (request.Level != null)
            {
                // 篩選指定層級的品牌
                // Level = 1：只回傳第一層品牌
                // Level = null：不進行層級篩選
                query = query.Where(c => c.Level == request.Level);
            }

            // ===== 第四階段：應用父品牌篩選 =====
            if (request.ParentId != null)
            {
                // 篩選指定父品牌下的子品牌
                // 注意：這裡會排除 ParentId 為 null 的品牌
                //      只查詢有父品牌且父品牌 ID 等於指定值的品牌
                query = query.Where(c => c.ParentId != null && c.ParentId == request.ParentId);
            }

            // ===== 第五階段：應用狀態篩選 =====
            if (!string.IsNullOrEmpty(request.Status))
            {
                // 篩選指定狀態的品牌
                // "active"：啟用狀態
                // "inactive"：停用狀態
                query = query.Where(c => c.Status == request.Status);
            }

            // ===== 第六階段：重複應用搜尋條件（程式碼重複）=====
            // 注意：這段程式碼與第一階段完全相同
            //      這是一個程式碼重複的問題
            //      建議刪除這段程式碼
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
            }

            // ===== 第七階段：應用分頁條件 =====
            // Keyset Pagination：只取 SortOrder 大於上一頁最後一筆的資料
            if (request.Cursor != null)
            {
                // 將 Cursor 轉換為整數
                // 然後篩選 SortOrder 大於此值的品牌
                query = query.Where(x => x.SortOrder > int.Parse(request.Cursor));
            }
            
            // 按 SortOrder 排序（數字越小越前面）
            query = query.OrderBy(c => c.SortOrder);

            // ===== 第八階段：限制回傳筆數 =====
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
