using Manian.Application.Models;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Users;

/// <summary>
/// 查詢點數交易記錄的請求物件
/// 
/// 用途：
/// - 查詢指定用戶的點數交易記錄
/// - 用於用戶點數歷史頁面
/// - 支援多種篩選條件和分頁功能
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<PointTransaction>>，表示這是一個查詢請求
/// - 回傳該用戶的點數交易記錄集合（包裝於分頁模型中）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PointTransactionsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 用戶點數歷史頁面
/// - 管理員查看用戶點數記錄
/// - 點數異常排查
/// 
/// 設計特點：
/// - 支援按用戶 ID 查詢
/// - 支援按參考類型和參考 ID 篩選
/// - 支援按時間範圍篩選
/// - 支援 Keyset Pagination（游標分頁）
/// 
/// 參考實作：
/// - BrandsQuery：使用 Cursor 參數的類似實作
/// - RolesQuery：類似的查詢和分頁實作
/// - SkusQuery：類似的篩選條件設計
/// </summary>
public class PointTransactionsQuery : IRequest<Pagination<PointTransaction>>
{
    /// <summary>
    /// 用戶 ID（必填）
    /// 
    /// 用途：
    /// - 識別要查詢點數交易的用戶
    /// - 作為查詢條件過濾點數交易
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的用戶
    /// 
    /// 錯誤處理：
    /// - 如果用戶不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 參考類型（可選）
    /// 
    /// 用途：
    /// - 過濾特定業務類型的點數交易
    /// - 例如：只查詢訂單相關的點數交易
    /// 
    /// 可選值：
    /// - "order"：訂單相關
    /// - "refund"：退款相關
    /// - "promotion"：促銷活動相關
    /// - "adjustment"：手動調整
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不過濾
    /// - 如果有值，則只回傳指定參考類型的交易記錄
    /// </summary>
    public string? RefType { get; set; }

    /// <summary>
    /// 參考 ID（可選）
    /// 
    /// 用途：
    /// - 過濾特定業務 ID 的點數交易
    /// - 例如：只查詢特定訂單的點數交易
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不過濾
    /// - 如果有值，則只回傳指定參考 ID 的交易記錄
    /// - 通常與 RefType 一起使用
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單 ID 為 100 的點數交易
    /// var query = new PointTransactionsQuery 
    /// { 
    ///     UserId = 1,
    ///     RefType = "order",
    ///     RefId = "100"
    /// };
    /// </code>
    /// </summary>
    public string? RefId { get; set; }

    /// <summary>
    /// 開始時間（可選）
    /// 
    /// 用途：
    /// - 過濾指定時間之後的點數交易
    /// - 用於時間範圍查詢
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不限制開始時間
    /// - 如果有值，則只回傳此時間之後的交易記錄
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// 結束時間（可選）
    /// 
    /// 用途：
    /// - 過濾指定時間之前的點數交易
    /// - 用於時間範圍查詢
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不限制結束時間
    /// - 如果有值，則只回傳此時間之前的交易記錄
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// 每頁筆數（可選）
    /// 
    /// 用途：
    /// - 控制每頁回傳的筆數
    /// - 用於分頁功能
    /// 
    /// 驗證規則：
    /// - 預設為 20
    /// - 建議範圍：1-100
    /// </summary>
    public int? Size { get; set; } = 20;

    /// <summary>
    /// 游標字串（可選）
    /// 
    /// 用途：
    /// - 實現 Keyset Pagination（游標分頁）
    /// - 記錄上一頁最後一筆資料的時間戳記
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 OccurredAt
    /// 2. 下一頁請求時將此值傳回後端
    /// 3. 後端根據游標查詢下一批資料
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示從第一頁開始
    /// - 如果有值，則只回傳此時間之後的交易記錄
    /// 
    /// 與 BrandsQuery 的一致性：
    /// - 使用字串類型而非 DateTimeOffset
    /// - 由前端傳回 Pagination 回傳的 Cursor 值
    /// - 後端將其解析為 DateTimeOffset 進行查詢
    /// </summary>
    public string? Cursor { get; set; }
}

/// <summary>
/// 點數交易查詢處理器
/// 
/// 職責：
/// - 接收 PointTransactionsQuery 請求
/// - 根據查詢條件篩選點數交易記錄
/// - 將查詢結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PointTransactionsQuery, Pagination<PointTransaction>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IUserRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 支援多種篩選條件（用戶 ID、參考類型、參考 ID、時間範圍）
/// - 使用 Keyset Pagination（游標分頁）提升效能
/// - 統一回傳格式為 Pagination，方便前端處理
/// 
/// 參考實作：
/// - BrandsQueryHandler：使用 Cursor 參數的類似實作
/// - RolesQueryHandler：類似的查詢和分頁實作
/// - SkusQueryHandler：類似的篩選條件設計
/// </summary>
public class PointTransactionsQueryHandler : IRequestHandler<PointTransactionsQuery, Pagination<PointTransaction>>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 查詢用戶的點數交易記錄
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 擴展了 GetPointTransactionsAsync 方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於查詢點數交易記錄</param>
    public PointTransactionsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 處理點數交易查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 PointTransactionsQuery 請求
    /// 2. 建構查詢條件
    /// 3. 呼叫 Repository 的方法取得資料
    /// 4. 將資料包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 支援多種篩選條件（用戶 ID、參考類型、參考 ID、時間範圍）
    /// - 使用 Keyset Pagination（游標分頁）
    /// - 按發生時間倒序排列（最新的在前）
    /// 
    /// 錯誤處理：
    /// - 如果用戶不存在，會返回包含空集合的 Pagination 物件
    /// - 如果沒有符合條件的交易記錄，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 範例 1：查詢用戶 ID 為 1 的所有點數交易
    /// var query1 = new PointTransactionsQuery { UserId = 1 };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：查詢用戶 ID 為 1 的訂單相關點數交易
    /// var query2 = new PointTransactionsQuery 
    /// { 
    ///     UserId = 1,
    ///     RefType = "order"
    /// };
    /// var result2 = await _mediator.SendAsync(query2);
    /// 
    /// // 範例 3：查詢用戶 ID 為 1 的特定時間範圍內的點數交易
    /// var query3 = new PointTransactionsQuery 
    /// { 
    ///     UserId = 1,
    ///     StartDate = DateTimeOffset.UtcNow.AddDays(-30),
    ///     EndDate = DateTimeOffset.UtcNow
    /// };
    /// var result3 = await _mediator.SendAsync(query3);
    /// 
    /// // 範例 4：使用游標分頁
    /// // 第一次請求（無游標）
    /// var query4 = new PointTransactionsQuery { UserId = 1 };
    /// var result4 = await _mediator.SendAsync(query4);
    /// 
    /// // 第二次請求（帶游標）
    /// var query5 = new PointTransactionsQuery 
    /// { 
    ///     UserId = 1,
    ///     Cursor = result4.Cursor
    /// };
    /// var result5 = await _mediator.SendAsync(query5);
    /// </code>
    /// </summary>
    /// <param name="request">點數交易查詢請求物件，包含各種篩選條件</param>
    /// <returns>包含符合條件點數交易記錄的分頁模型</returns>
    public async Task<Pagination<PointTransaction>> HandleAsync(PointTransactionsQuery request)
    {
        // ========== 第一步：建構查詢條件 ==========
        // 使用 Lambda 運算式建構查詢條件
        // 這種設計讓 Repository 的 GetAllAsync 方法可以保持單純
        // 只需執行這個 Func 即可
        var query = BuildQuery(request);

        // ========== 第二步：執行查詢並取得資料 ==========
        // 呼叫 Repository 的 GetAllAsync 方法
        // 傳入建構好的查詢條件
        // Repository 會執行查詢並回傳結果
        var transactions = await _userRepository.GetPointTransactionsAsync(request.UserId, query);

        // ========== 第三步：將查詢結果包裝成 Pagination 物件回傳 ==========
        // 使用 Pagination 建構函式建立分頁物件
        // items：查詢結果集合
        // requestedSize：請求的每頁筆數
        // cursorSelector：選擇 OccurredAt 作為游標
        return new Pagination<PointTransaction>(
            items: transactions,
            requestedSize: request.Size,
            cursorSelector: x => x.OccurredAt.ToString("O")
        );
    }

    /// <summary>
    /// 建構點數交易查詢表達式
    /// 
    /// 職責：
    /// - 根據請求參數動態建構查詢條件
    /// - 支援多種篩選條件的組合
    /// - 實現 Keyset Pagination（游標分頁）
    /// 
    /// 設計考量：
    /// - 將查詢邏輯集中在這裡，保持 Repository 介面的簡潔
    /// - 使用 Lambda 運算式延遲執行，提升效能
    /// - 支援多種篩選條件的組合
    /// 
    /// 執行流程：
    /// 1. 建立基礎查詢（過濾用戶 ID）
    /// 2. 套用參考類型和參考 ID 篩選
    /// 3. 套用時間範圍篩選
    /// 4. 套用 Keyset Pagination（游標分頁）
    /// 5. 按發生時間倒序排列
    /// 6. 限制回傳筆數
    /// </summary>
    /// <param name="request">點數交易查詢請求物件，包含各種篩選條件</param>
    /// <returns>
    /// 回傳一個 Func<IQueryable<PointTransaction>, IQueryable<PointTransaction>>
    /// 輸入一個 IQueryable<PointTransaction>，經過 Where、OrderByDescending、Take 等操作後，輸出另一個 IQueryable<PointTransaction>
    /// </returns>
    private static Func<IQueryable<PointTransaction>, IQueryable<PointTransaction>> BuildQuery(PointTransactionsQuery request)
    {
        // 回傳一個委派，這個委派接受原始的 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：套用參考類型和參考 ID 篩選 =====
            // 如果 RefType 有值，就篩選出指定參考類型的交易記錄
            if (!string.IsNullOrEmpty(request.RefType))
            {
                query = query.Where(x => x.RefType == request.RefType);
            }

            // 如果 RefId 有值，就篩選出指定參考 ID 的交易記錄
            if (!string.IsNullOrEmpty(request.RefId))
            {
                query = query.Where(x => x.RefId == request.RefId);
            }

            // ===== 第二階段：套用時間範圍篩選 =====
            // 如果 StartDate 有值，就篩選出此時間之後的交易記錄
            if (request.StartDate.HasValue)
            {
                query = query.Where(x => x.OccurredAt >= request.StartDate.Value);
            }

            // 如果 EndDate 有值，就篩選出此時間之前的交易記錄
            if (request.EndDate.HasValue)
            {
                query = query.Where(x => x.OccurredAt <= request.EndDate.Value);
            }

            // ===== 第三階段：套用 Keyset Pagination (基於游標的分頁) =====
            // 這種分頁方式比傳統的 Skip 更穩定，尤其是資料量大時
            if (!string.IsNullOrEmpty(request.Cursor))
            {
                // 解析游標字串為 DateTimeOffset
                // 游標格式為 ISO 8601 字串（由 Pagination 回傳）
                if (DateTimeOffset.TryParse(request.Cursor, out var cursorDate))
                {
                    // 只取 OccurredAt 小於游標時間的資料（因為我們是倒序排列）
                    // 這意味著前端需要記住最後一筆資料的 OccurredAt，並在下一頁請求時傳回來
                    query = query.Where(x => x.OccurredAt < cursorDate);
                }
            }

            // ===== 第四階段：按發生時間倒序排列 =====
            // 最新的交易記錄顯示在最前面
            query = query.OrderByDescending(x => x.OccurredAt);

            // ===== 第五階段：限制回傳筆數 =====
            // 如果 Size 有指定，就只取前 Size 筆
            if (request.Size.HasValue)
            {
                query = query.Take(request.Size.Value);
            }

            // 回傳最終組合好的 IQueryable
            // 注意：此時還沒真的去資料庫執行，要到被 foreach 或 ToList() 時才會實際查詢
            return query;
        };
    }
}
