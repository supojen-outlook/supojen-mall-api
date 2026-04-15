using Manian.Application.Models;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢訂單列表的請求物件
/// 
/// 用途：
/// - 查詢訂單列表，支援多種篩選條件
/// - 用於訂單管理頁面
/// - 用於會員訂單查詢頁面
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Order>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的訂單集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 OrdersQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 管理員查看所有訂單
/// - 會員查看自己的訂單
/// - 訂單狀態篩選
/// - 訂單編號搜尋
/// 
/// 設計特點：
/// - 支援多種篩選條件（會員 ID、狀態、搜尋關鍵字）
/// - 使用 Keyset Pagination 提升分頁效能
/// - 支援自訂每頁筆數
/// - 預設按建立時間倒序排列
/// 
/// 與其他查詢的對比：
/// - PaymentQuery：查詢單一訂單的付款記錄（需要 OrderId）
/// - ShipmentQuery：查詢單一訂單的物流記錄（需要 OrderId）
/// - OrdersQuery：查詢訂單列表（支援多種篩選條件和分頁）
/// </summary>
public class OrdersQuery : IRequest<Pagination<Order>>
{
    /// <summary>
    /// 會員 ID（可選）
    /// 
    /// 用途：
    /// - 篩選特定會員的訂單
    /// - 用於會員訂單查詢頁面
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的會員
    /// 
    /// 錯誤處理：
    /// - 如果會員不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢會員 ID 為 100 的所有訂單
    /// var query = new OrdersQuery { UserId = 100 };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 與 User 是多對一關係
    /// - 一個會員可以有多筆訂單
    /// - 此屬性用於篩選特定會員的訂單
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// 訂單狀態（可選）
    /// 
    /// 用途：
    /// - 篩選特定狀態的訂單
    /// - 用於訂單管理頁面的狀態篩選
    /// 
    /// 可選值：
    /// - pending：待處理
    /// - paid：已付款
    /// - shipped：已出貨
    /// - completed：已完成
    /// - cancelled：已取消
    /// 
    /// 驗證規則：
    /// - 必須為上述五個值之一
    /// - 不區分大小寫（由 Repository 實作決定）
    /// 
    /// 錯誤處理：
    /// - 如果狀態值無效，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢所有已付款的訂單
    /// var query = new OrdersQuery { Status = "paid" };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 實體包含 Status 欄位
    /// - 此屬性用於篩選特定狀態的訂單
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 用途：
    /// - 搜尋訂單編號
    /// - 用於訂單管理頁面的搜尋功能
    /// 
    /// 搜尋範圍：
    /// - 訂單編號（OrderNumber）
    /// - 不區分大小寫
    /// - 支援部分匹配（Contains）
    /// 
    /// 驗證規則：
    /// - 不能為空白字串
    /// - 會自動去除前後空白字元
    /// - 會自動轉為小寫進行比對
    /// 
    /// 錯誤處理：
    /// - 如果找不到符合條件的訂單，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 搜尋訂單編號包含 "2023" 的訂單
    /// var query = new OrdersQuery { Search = "2023" };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 實體包含 OrderNumber 欄位
    /// - 此屬性用於搜尋訂單編號
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
    /// - 必須對應資料庫中存在的訂單 ID
    /// 
    /// 錯誤處理：
    /// - 如果游標無效，會返回第一頁資料
    /// - 建議在 UI 層處理游標驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢 ID 大於 100 的訂單（第二頁）
    /// var query = new OrdersQuery { Cursor = "100", Size = 20 };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 實體包含 Id 欄位（主鍵）
    /// - 此屬性用於 Keyset Pagination
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// 每頁資料筆數（可選）
    /// 
    /// 用途：
    /// - 控制每頁回傳的訂單數量
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
    /// 錯誤處理：
    /// - 如果值無效，會使用預設值
    /// - 建議在 UI 層處理數值驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 每頁回傳 50 筆訂單
    /// var query = new OrdersQuery { Size = 50 };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 此屬性用於控制分頁大小
    /// - 不直接關聯資料庫欄位
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 訂單查詢處理器
/// 
/// 職責：
/// - 接收 OrdersQuery 請求
/// - 根據查詢條件建置 LINQ 查詢
/// - 呼叫 Repository 執行查詢
/// - 將查詢結果包裝成 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<OrdersQuery, Pagination<Order>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IOrderRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 使用表達式樹（Expression Tree）建置查詢
/// - 支援多種篩選條件的組合
/// - 使用 Keyset Pagination 提升效能
/// - 統一回傳格式為 Pagination，方便前端處理
/// 
/// 參考實作：
/// - ProductsQueryHandler：類似的查詢和分頁邏輯
/// - UsersQueryHandler：類似的查詢和分頁邏輯
/// </summary>
public class OrdersQueryHandler : IRequestHandler<OrdersQuery, Pagination<Order>>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 繼承自 Repository<Order>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢訂單資料</param>
    public OrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// 處理訂單查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 OrdersQuery 請求
    /// 2. 呼叫 BuildQuery 方法建置 LINQ 查詢
    /// 3. 呼叫 Repository 的 GetAllAsync 方法執行查詢
    /// 4. 將查詢結果包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 支援多種篩選條件的組合
    /// - 使用 Keyset Pagination 提升效能
    /// - 按建立時間倒序排列
    /// - 支援自訂每頁筆數
    /// 
    /// 錯誤處理：
    /// - 如果查詢條件無效，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 範例 1：查詢會員 ID 為 100 的所有訂單
    /// var query1 = new OrdersQuery { UserId = 100 };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：查詢所有已付款的訂單，每頁 50 筆
    /// var query2 = new OrdersQuery { Status = "paid", Size = 50 };
    /// var result2 = await _mediator.SendAsync(query2);
    /// 
    /// // 範例 3：搜尋訂單編號包含 "2023" 的訂單
    /// var query3 = new OrdersQuery { Search = "2023" };
    /// var result3 = await _mediator.SendAsync(query3);
    /// 
    /// // 範例 4：使用 Keyset Pagination 查詢第二頁
    /// var query4 = new OrdersQuery { Cursor = "100", Size = 20 };
    /// var result4 = await _mediator.SendAsync(query4);
    /// </code>
    /// </summary>
    /// <param name="request">訂單查詢請求物件，包含篩選條件和分頁參數</param>
    /// <returns>包含符合條件訂單的分頁模型</returns>
    public async Task<Pagination<Order>> HandleAsync(OrdersQuery request)
    {
        // ========== 第一步：建置 LINQ 查詢 ==========
        // 呼叫 BuildQuery 方法建置 LINQ 查詢
        // 這個方法會根據請求參數建置表達式樹
        // 見下方的 BuildQuery 方法實作
        var query = BuildQuery(request);
        
        // ========== 第二步：執行查詢 ==========
        // 呼叫 Repository 的 GetAllAsync 方法執行查詢
        // 這個方法會將表達式樹轉換為 SQL 查詢
        // 見 Infrastructure/Repositories/Orders/OrderRepository.cs 的實作
        var orders = await _orderRepository.GetAllAsync(query);
        
        // ========== 第三步：包裝成 Pagination 物件回傳 ==========
        // requestedSize：每頁資料筆數
        // cursorSelector：用於生成下一頁游標的選擇器
        // 這裡使用訂單 ID 作為游標
        return new Pagination<Order>(
            items: orders,
            requestedSize: request.Size,
            cursorSelector: o => o.Id.ToString()
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
    /// 1. 篩選會員 ID（如果提供）
    /// 2. 篩選訂單狀態（如果提供）
    /// 3. 搜尋訂單編號（如果提供）
    /// 4. 應用 Keyset Pagination（如果提供游標）
    /// 5. 按建立時間倒序排列
    /// 6. 限制回傳筆數（如果提供 Size）
    /// 
    /// 效能考量：
    /// - 使用表達式樹，讓 EF Core 能轉換為 SQL
    /// - 使用 Keyset Pagination，避免 OFFSET/LIMIT 的效能問題
    /// - 只在必要時才應用篩選條件
    /// </summary>
    /// <param name="request">訂單查詢請求物件，包含篩選條件和分頁參數</param>
    /// <returns>
    /// LINQ 查詢表達式
    /// 類型：Func<IQueryable<Order>, IQueryable<Order>>
    /// </returns>
    private static Func<IQueryable<Order>, IQueryable<Order>> BuildQuery(OrdersQuery request)
    {
        // ========== 第一步：定義查詢表達式 ==========
        // 使用表達式樹（Expression Tree）建置查詢
        // 這樣 EF Core 能將其轉換為 SQL 查詢
        return query =>
        {
            // ========== 第二步：篩選會員 ID ==========
            // 如果提供了 UserId，則篩選該會員的訂單
            if (request.UserId.HasValue)
            {
                query = query.Where(o => o.UserId == request.UserId.Value);
            }

            // ========== 第三步：篩選訂單狀態 ==========
            // 如果提供了 Status，則篩選該狀態的訂單
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(o => o.Status == request.Status);
            }

            // ========== 第四步：搜尋訂單編號 ==========
            // 如果提供了 Search，則搜尋訂單編號
            // 不區分大小寫，支援部分匹配
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(o => o.OrderNumber.ToLower().Contains(searchTerm));
            }

            // ========== 第五步：應用 Keyset Pagination ==========
            // 如果提供了 Cursor，則使用 Keyset Pagination
            // Keyset Pagination 使用 WHERE id > cursor LIMIT size，效能比 OFFSET/LIMIT 更好
            if (!string.IsNullOrWhiteSpace(request.Cursor) && int.TryParse(request.Cursor, out var cursorId))
            {
                query = query.Where(o => o.Id > cursorId);
            }

            // ========== 第六步：排序 ==========
            // 按建立時間倒序排列
            // 這樣最新的訂單會顯示在最前面
            query = query.OrderByDescending(o => o.CreatedAt);

            // ========== 第七步：限制回傳筆數 ==========
            // 如果提供了 Size，則限制回傳筆數
            if (request.Size.HasValue)
            {
                query = query.Take(request.Size.Value);
            }

            // ========== 第八步：回傳查詢表達式 ==========
            // 回傳建置好的查詢表達式
            // EF Core 會將其轉換為 SQL 查詢執行
            return query;
        };
    }
}
