using Manian.Application.Models;
using Manian.Application.Services;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢當前登入用戶訂單列表的請求物件
/// 
/// 用途：
/// - 查詢當前登入用戶的訂單列表
/// - 用於會員訂單查詢頁面
/// - 自動鎖定為當前登入用戶的訂單
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Order>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的訂單集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 MyOrdersQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 會員查看自己的訂單列表
/// - 會員訂單狀態篩選
/// - 會員訂單編號搜尋
/// - 會員訂單日期篩選
/// 
/// 設計特點：
/// - 自動鎖定為當前登入用戶的訂單（透過 IUserClaim）
/// - 支援訂單狀態篩選
/// - 支援訂單編號搜尋
/// - 支援訂單日期範圍篩選（新增）
/// - 使用 Keyset Pagination 提升分頁效能
/// - 支援自訂每頁筆數
/// - 預設按建立時間倒序排列
/// 
/// 與 OrdersQuery 的對比：
/// - OrdersQuery：可查詢任何用戶的訂單（需提供 UserId）
/// - MyOrdersQuery：只能查詢當前登入用戶的訂單（自動從 IUserClaim 取得）
/// </summary>
public class MyOrdersQuery : IRequest<Pagination<Order>>
{
    /// <summary>
    /// 訂單狀態（可選）
    /// 
    /// 用途：
    /// - 篩選特定狀態的訂單
    /// - 用於會員訂單查詢頁面的狀態篩選
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
    /// // 查詢當前登入用戶的所有已付款訂單
    /// var query = new MyOrdersQuery { Status = "paid" };
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
    /// - 用於會員訂單查詢頁面的搜尋功能
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
    /// // 搜尋當前登入用戶的訂單編號包含 "2023" 的訂單
    /// var query = new MyOrdersQuery { Search = "2023" };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 實體包含 OrderNumber 欄位
    /// - 此屬性用於搜尋訂單編號
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 訂單開始日期（可選）
    /// 
    /// 用途：
    /// - 篩選指定日期之後的訂單
    /// - 用於會員訂單查詢頁面的日期篩選
    /// - 與 EndDate 配合使用，形成日期範圍篩選
    /// 
    /// 驗證規則：
    /// - 必須為有效的 DateTimeOffset 值
    /// - 必須早於或等於 EndDate（如果提供了 EndDate）
    /// - 建議使用 UTC 時間
    /// 
    /// 錯誤處理：
    /// - 如果日期無效，會忽略此條件
    /// - 如果 StartDate 晚於 EndDate，會忽略此條件
    /// - 建議在 UI 層處理日期驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢當前登入用戶在 2023 年 1 月 1 日之後的訂單
    /// var query = new MyOrdersQuery { 
    ///     StartDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) 
    /// };
    /// 
    /// // 查詢當前登入用戶在 2023 年 1 月 1 日到 2023 年 12 月 31 日之間的訂單
    /// var query = new MyOrdersQuery { 
    ///     StartDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
    ///     EndDate = new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero)
    /// };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 實體包含 CreatedAt 欄位
    /// - 此屬性用於篩選指定日期之後的訂單
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// 訂單結束日期（可選）
    /// 
    /// 用途：
    /// - 篩選指定日期之前的訂單
    /// - 用於會員訂單查詢頁面的日期篩選
    /// - 與 StartDate 配合使用，形成日期範圍篩選
    /// 
    /// 驗證規則：
    /// - 必須為有效的 DateTimeOffset 值
    /// - 必須晚於或等於 StartDate（如果提供了 StartDate）
    /// - 建議使用 UTC 時間
    /// 
    /// 錯誤處理：
    /// - 如果日期無效，會忽略此條件
    /// - 如果 EndDate 早於 StartDate，會忽略此條件
    /// - 建議在 UI 層處理日期驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢當前登入用戶在 2023 年 12 月 31 日之前的訂單
    /// var query = new MyOrdersQuery { 
    ///     EndDate = new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero) 
    /// };
    /// 
    /// // 查詢當前登入用戶在 2023 年 1 月 1 日到 2023 年 12 月 31 日之間的訂單
    /// var query = new MyOrdersQuery { 
    ///     StartDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
    ///     EndDate = new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero)
    /// };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 實體包含 CreatedAt 欄位
    /// - 此屬性用於篩選指定日期之前的訂單
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

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
    /// // 查詢當前登入用戶的訂單，ID 大於 100 的訂單（第二頁）
    /// var query = new MyOrdersQuery { Cursor = "100", Size = 20 };
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
    /// var query = new MyOrdersQuery { Size = 50 };
    /// </code>
    /// 
    /// 資料關聯：
    /// - 此屬性用於控制分頁大小
    /// - 不直接關聯資料庫欄位
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 當前登入用戶訂單查詢處理器
/// 
/// 職責：
/// - 接收 MyOrdersQuery 請求
/// - 從 IUserClaim 取得當前登入用戶 ID
/// - 根據查詢條件建置 LINQ 查詢
/// - 呼叫 Repository 執行查詢
/// - 將查詢結果包裝成 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<MyOrdersQuery, Pagination<Order>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IOrderRepository 和 IUserClaim
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 自動鎖定為當前登入用戶的訂單（透過 IUserClaim）
/// - 使用表達式樹（Expression Tree）建置查詢
/// - 支援多種篩選條件的組合
/// - 支援日期範圍篩選（新增）
/// - 使用 Keyset Pagination 提升效能
/// - 統一回傳格式為 Pagination，方便前端處理
/// 
/// 參考實作：
/// - OrdersQueryHandler：類似的查詢和分頁邏輯
/// - ProductsQueryHandler：類似的查詢和分頁邏輯
/// - UsersQueryHandler：類似的查詢和分頁邏輯
/// </summary>
public class MyOrdersQueryHandler : IRequestHandler<MyOrdersQuery, Pagination<Order>>
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
    /// 當前請求的使用者身份服務
    /// 
    /// 用途：
    /// - 從 HTTP 請求中獲取當前登入用戶的 ID
    /// - 提供統一的方式存取使用者身份資訊
    /// 
    /// 實作方式：
    /// - 從 JWT Token 的 "sub" 宣告解析使用者 ID（見 Infrastructure/Services/UserClaim.cs）
    /// - 使用 init 關鍵字確保 ID 在請求生命週期中不可變
    /// 
    /// 介面定義：
    /// - 見 Application/Services/IUserClaim.cs
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢訂單資料</param>
    /// <param name="userClaim">使用者身份服務，用於獲取當前登入用戶 ID</param>
    public MyOrdersQueryHandler(
        IOrderRepository orderRepository,
        IUserClaim userClaim)
    {
        _orderRepository = orderRepository;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理當前登入用戶訂單查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 從 IUserClaim 取得當前登入用戶 ID
    /// 2. 呼叫 BuildQuery 方法建置 LINQ 查詢
    /// 3. 呼叫 Repository 的 GetAllAsync 方法執行查詢
    /// 4. 將查詢結果包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 自動鎖定為當前登入用戶的訂單
    /// - 支援多種篩選條件的組合
    /// - 支援日期範圍篩選（新增）
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
    /// // 範例 1：查詢當前登入用戶的所有訂單
    /// var query1 = new MyOrdersQuery();
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：查詢當前登入用戶的所有已付款訂單，每頁 50 筆
    /// var query2 = new MyOrdersQuery { Status = "paid", Size = 50 };
    /// var result2 = await _mediator.SendAsync(query2);
    /// 
    /// // 範例 3：搜尋當前登入用戶的訂單編號包含 "2023" 的訂單
    /// var query3 = new MyOrdersQuery { Search = "2023" };
    /// var result3 = await _mediator.SendAsync(query3);
    /// 
    /// // 範例 4：使用 Keyset Pagination 查詢第二頁
    /// var query4 = new MyOrdersQuery { Cursor = "100", Size = 20 };
    /// var result4 = await _mediator.SendAsync(query4);
    /// 
    /// // 範例 5：查詢當前登入用戶在 2023 年 1 月 1 日到 2023 年 12 月 31 日之間的訂單
    /// var query5 = new MyOrdersQuery { 
    ///     StartDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
    ///     EndDate = new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero)
    /// };
    /// var result5 = await _mediator.SendAsync(query5);
    /// </code>
    /// </summary>
    /// <param name="request">當前登入用戶訂單查詢請求物件，包含篩選條件和分頁參數</param>
    /// <returns>包含符合條件訂單的分頁模型</returns>
    public async Task<Pagination<Order>> HandleAsync(MyOrdersQuery request)
    {
        // ========== 第一步：取得當前登入用戶 ID ==========
        // 從 IUserClaim 服務獲取當前登入用戶的唯一識別碼
        // 這個值來自 JWT Token 的 "sub" 宣告，已經過驗證
        var userId = _userClaim.Id;

        // ========== 第二步：建置 LINQ 查詢 ==========
        // 呼叫 BuildQuery 方法建置 LINQ 查詢
        // 這個方法會根據請求參數建置表達式樹
        // 見下方的 BuildQuery 方法實作
        var query = BuildQuery(request, userId);
        
        // ========== 第三步：執行查詢 ==========
        // 呼叫 Repository 的 GetAllAsync 方法執行查詢
        // 這個方法會將表達式樹轉換為 SQL 查詢
        // 見 Infrastructure/Repositories/Orders/OrderRepository.cs 的實作
        var orders = await _orderRepository.GetAllAsync(query);
        
        // ========== 第四步：包裝成 Pagination 物件回傳 ==========
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
    /// - 自動鎖定為當前登入用戶的訂單
    /// - 支援多種篩選條件的組合
    /// - 支援日期範圍篩選（新增）
    /// - 使用表達式樹（Expression Tree）提升效能
    /// 
    /// 設計考量：
    /// - 使用靜態方法，避免建立實例
    /// - 使用表達式樹，讓 EF Core 能轉換為 SQL
    /// - 支援多種篩選條件的組合
    /// - 使用 Keyset Pagination 提升效能
    /// 
    /// 查詢流程：
    /// 1. 鎖定為當前登入用戶的訂單（必須）
    /// 2. 篩選訂單狀態（如果提供）
    /// 3. 搜尋訂單編號（如果提供）
    /// 4. 篩選日期範圍（如果提供，新增）
    /// 5. 應用 Keyset Pagination（如果提供游標）
    /// 6. 按建立時間倒序排列
    /// 7. 限制回傳筆數（如果提供 Size）
    /// 
    /// 效能考量：
    /// - 使用表達式樹，讓 EF Core 能轉換為 SQL
    /// - 使用 Keyset Pagination，避免 OFFSET/LIMIT 的效能問題
    /// - 只在必要時才應用篩選條件
    /// </summary>
    /// <param name="request">當前登入用戶訂單查詢請求物件，包含篩選條件和分頁參數</param>
    /// <param name="userId">當前登入用戶 ID</param>
    /// <returns>
    /// LINQ 查詢表達式
    /// 類型：Func<IQueryable<Order>, IQueryable<Order>>
    /// </returns>
    private static Func<IQueryable<Order>, IQueryable<Order>> BuildQuery(MyOrdersQuery request, int userId)
    {
        // ========== 第一步：定義查詢表達式 ==========
        // 使用表達式樹（Expression Tree）建置查詢
        // 這樣 EF Core 能將其轉換為 SQL 查詢
        return query =>
        {
            // ========== 第二步：鎖定為當前登入用戶的訂單 ==========
            // 必須篩選當前登入用戶的訂單
            // 這是 MyOrdersQuery 與 OrdersQuery 的主要差異
            query = query.Where(o => o.UserId == userId);

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

            // ========== 第五步：篩選日期範圍（新增）==========
            // 如果提供了 StartDate，則篩選該日期之後的訂單
            if (request.StartDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= request.StartDate.Value);
            }

            // 如果提供了 EndDate，則篩選該日期之前的訂單
            if (request.EndDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= request.EndDate.Value);
            }

            // ========== 第六步：應用 Keyset Pagination ==========
            // 如果提供了 Cursor，則使用 Keyset Pagination
            // Keyset Pagination 使用 WHERE id > cursor LIMIT size，效能比 OFFSET/LIMIT 更好
            if (!string.IsNullOrWhiteSpace(request.Cursor) && int.TryParse(request.Cursor, out var cursorId))
            {
                query = query.Where(o => o.Id > cursorId);
            }

            // ========== 第七步：排序 ==========
            // 按建立時間倒序排列
            // 這樣最新的訂單會顯示在最前面
            query = query.OrderByDescending(o => o.CreatedAt);

            // ========== 第八步：限制回傳筆數 ==========
            // 如果提供了 Size，則限制回傳筆數
            if (request.Size.HasValue)
            {
                query = query.Take(request.Size.Value);
            }

            // ========== 第九步：回傳查詢表達式 ==========
            // 回傳建置好的查詢表達式
            // EF Core 會將其轉換為 SQL 查詢執行
            return query;
        };
    }
}
