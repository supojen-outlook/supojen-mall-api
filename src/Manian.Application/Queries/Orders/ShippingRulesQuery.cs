using Manian.Application.Models;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢運費規則列表的請求物件
/// 
/// 用途：
/// - 查詢所有運費規則
/// - 支援多種篩選條件
/// - 支援分頁功能
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<ShippingRule>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的運費規則集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ShippingRulesQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 運費規則管理頁面
/// - 運費規則列表顯示
/// - 運費規則篩選和搜尋
/// 
/// 設計特點：
/// - 支援多種篩選條件（是否啟用、搜尋關鍵字）
/// - 使用 Keyset Pagination 提升分頁效能
/// - 支援自訂每頁筆數
/// - 預設按優先級排序
/// 
/// 參考實作：
/// - OrdersQuery：查詢訂單列表（支援多種篩選條件和分頁）
/// - RolesQuery：查詢角色列表（支援搜尋和分頁）
/// </summary>
public class ShippingRulesQuery : IRequest<Pagination<ShippingRule>>
{
    /// <summary>
    /// 是否啟用（可選）
    /// 
    /// 用途：
    /// - 篩選啟用或停用的運費規則
    /// - 用於運費規則管理頁面的狀態篩選
    /// 
    /// 驗證規則：
    /// - 必須為布林值
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢所有啟用的運費規則
    /// var query = new ShippingRulesQuery { IsActive = true };
    /// </code>
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 用途：
    /// - 搜尋運費規則名稱或描述
    /// - 用於運費規則管理頁面的搜尋功能
    /// 
    /// 搜尋範圍：
    /// - 規則名稱（Name）
    /// - 規則描述（Description）
    /// - 不區分大小寫
    /// - 支援部分匹配（Contains）
    /// 
    /// 驗證規則：
    /// - 不能為空白字串
    /// - 會自動去除前後空白字元
    /// - 會自動轉為小寫進行比對
    /// 
    /// 使用範例：
    /// <code>
    /// // 搜尋名稱或描述包含 "免運" 的運費規則
    /// var query = new ShippingRulesQuery { Search = "免運" };
    /// </code>
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 用於 Keyset Pagination
    /// - 記錄上一頁最後一筆的 Id
    /// - 用於取得下一頁資料
    /// 
    /// Keyset Pagination 說明：
    /// - 傳統分頁使用 OFFSET/LIMIT，效能較差
    /// - Keyset Pagination 使用 WHERE id > cursor LIMIT size，效能更好
    /// - 適合資料量大的場景
    /// 
    /// 驗證規則：
    /// - 必須為有效的整數字串
    /// - 必須對應資料庫中存在的運費規則 ID
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢 ID 大於 100 的運費規則（第二頁）
    /// var query = new ShippingRulesQuery { Cursor = "100", Size = 20 };
    /// </code>
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// 每頁資料筆數（可選）
    /// 
    /// 用途：
    /// - 控制每頁回傳的運費規則數量
    /// - 用於分頁功能
    /// 
    /// 預設值：
    /// - 20（由 Repository 實作決定）
    /// 
    /// 最大值：
    /// - 100（由 Repository 實作決定）
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 不能超過最大值
    /// 
    /// 使用範例：
    /// <code>
    /// // 每頁回傳 50 筆運費規則
    /// var query = new ShippingRulesQuery { Size = 50 };
    /// </code>
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 運費規則查詢處理器
/// 
/// 職責：
/// - 接收 ShippingRulesQuery 請求
/// - 根據查詢條件建置 LINQ 查詢
/// - 呼叫 Repository 執行查詢
/// - 將查詢結果包裝成 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShippingRulesQuery, Pagination<ShippingRule>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IShippingRuleRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 使用表達式樹（Expression Tree）建置查詢
/// - 支援多種篩選條件的組合
/// - 使用 Keyset Pagination 提升效能
/// - 統一回傳格式為 Pagination，方便前端處理
/// 
/// 參考實作：
/// - OrdersQueryHandler：查詢訂單列表（支援多種篩選條件和分頁）
/// - RolesQueryHandler：查詢角色列表（支援搜尋和分頁）
/// </summary>
public class ShippingRulesQueryHandler : IRequestHandler<ShippingRulesQuery, Pagination<ShippingRule>>
{
    /// <summary>
    /// 運費規則倉儲介面
    /// 
    /// 用途：
    /// - 存取運費規則資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/ShippingRuleRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 繼承自 Repository<ShippingRule>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IShippingRuleRepository.cs
    /// </summary>
    private readonly IShippingRuleRepository _shippingRuleRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="shippingRuleRepository">運費規則倉儲，用於查詢運費規則資料</param>
    public ShippingRulesQueryHandler(IShippingRuleRepository shippingRuleRepository)
    {
        _shippingRuleRepository = shippingRuleRepository;
    }

    /// <summary>
    /// 處理運費規則查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 ShippingRulesQuery 請求
    /// 2. 呼叫 BuildQuery 方法建置 LINQ 查詢
    /// 3. 呼叫 Repository 的 GetAllAsync 方法執行查詢
    /// 4. 將查詢結果包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 支援多種篩選條件的組合
    /// - 使用 Keyset Pagination 提升效能
    /// - 按優先級排序
    /// - 支援自訂每頁筆數
    /// 
    /// 錯誤處理：
    /// - 如果查詢條件無效，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 範例 1：查詢所有啟用的運費規則
    /// var query1 = new ShippingRulesQuery { IsActive = true };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：查詢名稱或描述包含 "免運" 的運費規則，每頁 50 筆
    /// var query2 = new ShippingRulesQuery { Search = "免運", Size = 50 };
    /// var result2 = await _mediator.SendAsync(query2);
    /// 
    /// // 範例 3：使用 Keyset Pagination 查詢第二頁
    /// var query3 = new ShippingRulesQuery { Cursor = "100", Size = 20 };
    /// var result3 = await _mediator.SendAsync(query3);
    /// </code>
    /// </summary>
    /// <param name="request">運費規則查詢請求物件，包含篩選條件和分頁參數</param>
    /// <returns>包含符合條件運費規則的分頁模型</returns>
    public async Task<Pagination<ShippingRule>> HandleAsync(ShippingRulesQuery request)
    {
        // ========== 第一步：建置 LINQ 查詢 ==========
        // 呼叫 BuildQuery 方法建置 LINQ 查詢
        // 這個方法會根據請求參數建置表達式樹
        var query = BuildQuery(request);
        
        // ========== 第二步：執行查詢 ==========
        // 呼叫 Repository 的 GetAllAsync 方法執行查詢
        // 這個方法會將表達式樹轉換為 SQL 查詢
        var rules = await _shippingRuleRepository.GetAllAsync(query);
        
        // ========== 第三步：包裝成 Pagination 物件回傳 ==========
        // requestedSize：每頁資料筆數
        // cursorSelector：用於生成下一頁游標的選擇器
        // 這裡使用運費規則 ID 作為游標
        return new Pagination<ShippingRule>(
            items: rules,
            requestedSize: request.Size,
            cursorSelector: r => r.Id.ToString()
        );
    }

    /// <summary>
    /// 建置 LINQ 查詢的私有方法
    /// 
    /// 職責：
    /// - 根據請求參數建置 LINQ 查詢
    /// - 支援多種篩選條件的組合
    /// - 使用表達式樹（Expression Tree）提升效能
    /// 
    /// 設計考量：
    /// - 使用靜態方法，避免建立實例
    /// - 使用表達式樹，讓 EF Core 能轉換為 SQL
    /// - 支援多種篩選條件的組合
    /// - 使用 Keyset Pagination 提升效能
    /// 
    /// 查詢流程：
    /// 1. 篩選是否啟用（如果提供）
    /// 2. 搜尋規則名稱或描述（如果提供）
    /// 3. 應用 Keyset Pagination（如果提供游標）
    /// 4. 按優先級排序
    /// 5. 限制回傳筆數（如果提供 Size）
    /// 
    /// 效能考量：
    /// - 使用表達式樹，讓 EF Core 能轉換為 SQL
    /// - 使用 Keyset Pagination，避免 OFFSET/LIMIT 的效能問題
    /// - 只在必要時才應用篩選條件
    /// </summary>
    /// <param name="request">運費規則查詢請求物件，包含篩選條件和分頁參數</param>
    /// <returns>
    /// LINQ 查詢表達式
    /// 類型：Func<IQueryable<ShippingRule>, IQueryable<ShippingRule>>
    /// </returns>
    private static Func<IQueryable<ShippingRule>, IQueryable<ShippingRule>> BuildQuery(ShippingRulesQuery request)
    {
        // ========== 第一步：定義查詢表達式 ==========
        // 使用表達式樹（Expression Tree）建置查詢
        // 這樣 EF Core 能將其轉換為 SQL 查詢
        return query =>
        {
            // ========== 第二步：篩選是否啟用 ==========
            // 如果提供了 IsActive，則篩選啟用或停用的運費規則
            if (request.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == request.IsActive.Value);
            }

            // ========== 第三步：搜尋規則名稱或描述 ==========
            // 如果提供了 Search，則搜尋規則名稱或描述
            // 不區分大小寫，支援部分匹配
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(r => 
                    r.Name.ToLower().Contains(searchTerm) ||
                    (r.Description != null && r.Description.ToLower().Contains(searchTerm))
                );
            }

            // ========== 第四步：應用 Keyset Pagination ==========
            // 如果提供了 Cursor，則使用 Keyset Pagination
            // Keyset Pagination 使用 WHERE id > cursor LIMIT size，效能比 OFFSET/LIMIT 更好
            if (!string.IsNullOrWhiteSpace(request.Cursor) && int.TryParse(request.Cursor, out var cursorId))
            {
                query = query.Where(r => r.Id > cursorId);
            }

            // ========== 第五步：排序 ==========
            // 按優先級排序
            // 這樣優先級高的規則會顯示在最前面
            query = query.OrderBy(r => r.Priority);

            // ========== 第六步：限制回傳筆數 ==========
            // 如果提供了 Size，則限制回傳筆數
            if (request.Size.HasValue)
            {
                query = query.Take(request.Size.Value);
            }

            // ========== 第七步：回傳查詢表達式 ==========
            // 回傳建置好的查詢表達式
            // EF Core 會將其轉換為 SQL 查詢執行
            return query;
        };
    }
}
