using Manian.Application.Models;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 計量單位查詢請求類別
/// 
/// 用途：
/// - 查詢系統中的計量單位列表
/// - 支援多欄位模糊搜尋
/// - 支援基於時間的游標分頁
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<UnitOfMeasure>>，表示這是一個查詢請求
/// - 回傳 Pagination<UnitOfMeasure> 包含資料列表
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 UnitOfMeasuresQueryHandler 配合使用，完成查詢
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination (基於 CreatedAt 的游標分頁)
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 LastCreatedAt 參數記錄上一頁最後一筆的時間
/// </summary>
public class UnitOfMeasuresQuery : IRequest<Pagination<UnitOfMeasure>>
{
    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 搜尋範圍：
    /// - 單位代碼
    /// - 單位名稱
    /// - 單位描述
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫（查詢時會轉為小寫）
    /// - 支援模糊搜尋（使用 Contains）
    /// - 會自動去除前後空白
    /// 
    /// 使用範例：
    /// - "box" → 會找到代碼、名稱或描述中包含 "box" 的單位
    /// - "BOX" → 同樣會找到（不區分大小寫）
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 上一頁最後一筆資料的建立時間 (游標)
    /// 
    /// 用途：實現 Keyset Pagination（游標分頁）
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 CreatedAt
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 CreatedAt 小於此值的資料 (因為是降序排列)
    /// 
    /// 為什麼用 CreatedAt 而不是 Id？
    /// - CreatedAt 更有業務意義（按時間排序）
    /// - 避免暴露內部 Id 給前端
    /// - 支援時間軸式的瀏覽體驗
    /// 
    /// 注意事項：
    /// - 首次查詢時應傳 null（取得最新資料）
    /// - 必須配合 OrderByDescending(CreatedAt) 使用
    /// </summary>
    public DateTime? LastCreatedAt { get; set; }
    
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
/// 計量單位查詢處理器
/// 
/// 職責：
/// - 接收 UnitOfMeasuresQuery 請求
/// - 建構查詢條件（搜尋、分頁）
/// - 從資料庫取得符合條件的計量單位
/// - 封裝分頁結果
/// 
/// 設計模式：
/// - 實作 IRequestHandler<UnitOfMeasuresQuery, Pagination<UnitOfMeasure>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例
/// 
/// 測試性：
/// - 可輕易 Mock IUnitOfMeasureRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
public class UnitOfMeasuresQueryHandler : IRequestHandler<UnitOfMeasuresQuery, Pagination<UnitOfMeasure>>
{
    /// <summary>
    /// 計量單位倉儲介面
    /// 
    /// 用途：
    /// - 存取計量單位資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly IUnitOfMeasureRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">計量單位倉儲，用於查詢資料</param>
    public UnitOfMeasuresQueryHandler(IUnitOfMeasureRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理計量單位查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 將結果封裝為 Pagination 物件
    /// 
    /// 查詢特性：
    /// - 支援多欄位模糊搜尋
    /// - 使用 Keyset Pagination 分頁
    /// - 按建立時間降序排列
    /// </summary>
    /// <param name="request">計量單位查詢請求物件，包含搜尋和分頁參數</param>
    /// <returns>包含資料列表的 Pagination<UnitOfMeasure> 物件</returns>
    public async Task<Pagination<UnitOfMeasure>> HandleAsync(UnitOfMeasuresQuery request)
    {
        // 呼叫 BuildQuery 建構查詢條件，然後傳給 Repository 執行
        var unitOfMeasures = await _repository.GetAllAsync(BuildQuery(request));
    
        // 建立回傳物件
        // 注意：此處 requestedSize 傳 null，表示不依賴建構函式自動計算 Cursor
        // cursorSelector 傳 null，表示不在此處生成 Cursor
        // 這意味著此查詢可能是一次性加載所有數據，或者由前端/上層邏輯處理分頁狀態
        return new Pagination<UnitOfMeasure>(
            items: unitOfMeasures,
            requestedSize: null,
            cursorSelector: null  
        );
    }

    /// <summary>
    /// 建構計量單位查詢表達式
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
    /// 1. 應用搜尋條件（如果有 Search）
    /// 2. 應用分頁條件（如果有 LastCreatedAt）
    /// 3. 按建立時間降序排序
    /// 4. 限制回傳筆數（如果有 Size）
    /// </summary>
    /// <param name="request">計量單位查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<UnitOfMeasure>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<UnitOfMeasure>
    /// </returns>
    private static Func<IQueryable<UnitOfMeasure>, IQueryable<UnitOfMeasure>> BuildQuery(UnitOfMeasuresQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用搜尋條件 =====
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                // 1. 去除前後空白並轉為小寫
                var searchTerm = request.Search.Trim().ToLower();
                
                // 2. 在 Code、Name、Description 三個欄位中進行模糊搜尋
                //    使用 OR 邏輯，只要任一欄位包含關鍵字即符合
                //    ToLower() 確保不區分大小寫
                query = query.Where(x => 
                    x.Code.ToLower().Contains(searchTerm) || 
                    x.Name.ToLower().Contains(searchTerm) || 
                    x.Description.ToLower().Contains(searchTerm));
            }

            // ===== 第二階段：應用分頁條件 =====
            // Keyset Pagination：只取 CreatedAt 小於上一頁最後一筆的資料
            // 注意：這裡使用小於 (<) 是因為預設是降序排列 (最新的在前)
            if (request.LastCreatedAt != null)
            {
                query = query.Where(x => x.CreatedAt < request.LastCreatedAt);
            }
            
            // 按建立時間降序排列（最新的在前）
            // 必須在分頁條件之後執行，確保游標比較的邏輯正確
            query = query.OrderByDescending(l => l.CreatedAt);

            // ===== 第三階段：限制回傳筆數 =====
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
