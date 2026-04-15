using Manian.Application.Models;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Users;

/// <summary>
/// 查詢用戶身份認證資訊的請求物件
/// 
/// 用途：
/// - 查詢指定用戶的所有身份認證資訊
/// - 支援游標分頁（Keyset Pagination）
/// - 用於用戶管理頁面查看用戶的登入方式
/// - 用於用戶資料編輯功能
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Identity>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的 Identity 實體集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 IdentitiesQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 用戶管理頁面查看用戶的登入方式
/// - 用戶資料編輯表單
/// - 用戶身份認證資訊確認
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination（基於 Id 的游標分頁）
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 Cursor 參數記錄上一頁最後一筆的 Id
/// 
/// 與 UserQuery 的區別：
/// - UserQuery：查詢用戶基本資料和點數
/// - IdentitiesQuery：查詢用戶的身份認證資訊（Google、Line、Microsoft、Facebook 等）
/// 
/// 錯誤處理：
/// - 用戶不存在：拋出 Failure.NotFound("找不到指定的用戶")
/// </summary>
public class IdentitiesQuery : IRequest<Pagination<Identity>>
{
    /// <summary>
    /// 用戶唯一識別碼
    /// 
    /// 用途：
    /// - 用於查詢指定的用戶
    /// - 必須是資料庫中已存在的用戶 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的用戶
    /// 
    /// 錯誤處理：
    /// - 如果用戶不存在，會拋出 Failure.NotFound("找不到指定的用戶")
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 實現 Keyset Pagination（游標分頁）
    /// - 記錄上一頁最後一筆資料的 Id
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 Id
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 Id 大於此值的資料
    /// 
    /// 為什麼用 Id？
    /// - Id 是唯一識別碼，適合作為游標
    /// - Id 通常按插入順序遞增
    /// - 避免暴露內部業務欄位給前端
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
    public int? Size { get; set; }
}

/// <summary>
/// 用戶身份認證資訊查詢處理器
/// 
/// 職責：
/// - 接收 IdentitiesQuery 請求
/// - 從資料庫取得指定用戶的所有身份認證資訊
/// - 支援游標分頁
/// - 回傳包裝在 Pagination 模型中的 Identity 實體集合
/// 
/// 設計模式：
/// - 實作 IRequestHandler<IdentitiesQuery, Pagination<Identity>> 介面
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
/// - 使用 Repository 的泛型 GetAllAsync 方法
/// - 支援游標分頁（Keyset Pagination）
/// - 統一回傳格式為 Pagination，方便前端處理
/// - 包含用戶的所有身份認證資訊
/// 
/// 參考實作：
/// - UserQueryHandler：查詢用戶基本資料的類似實作
/// - RolesQueryHandler：查詢角色列表的類似實作
/// </summary>
public class IdentitiesQueryHandler : IRequestHandler<IdentitiesQuery, Pagination<Identity>>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 查詢用戶的身份認證資訊
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 繼承自 Repository<User>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於查詢用戶資料</param>
    public IdentitiesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 處理用戶身份認證資訊查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證用戶是否存在
    /// 2. 建構查詢條件（包含分頁邏輯）
    /// 3. 呼叫 Repository 的 GetAllAsync 方法取得資料
    /// 4. 將資料包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 支援游標分頁（Keyset Pagination）
    /// - 按 Id 排序（由查詢邏輯決定）
    /// - 包含用戶的所有身份認證資訊
    /// 
    /// 資料來源：
    /// - Identity 實體包含：Id、UserId、Provider、ProviderUid
    /// - Provider 表示認證廠商：google、line、microsoft、facebook
    /// - ProviderUid 表示認證廠商的唯一識別碼
    /// 
    /// 錯誤處理：
    /// - 用戶不存在：拋出 Failure.NotFound("找不到指定的用戶")
    /// </summary>
    /// <param name="request">用戶身份認證資訊查詢請求物件，包含用戶 ID、游標和每頁筆數</param>
    /// <returns>包含用戶身份認證資訊的分頁模型</returns>
    public async Task<Pagination<Identity>> HandleAsync(IdentitiesQuery request)
    {
        // ========== 第一步：驗證用戶是否存在 ==========
        // 使用 IUserRepository.GetByIdAsync() 查詢用戶
        // 如果找不到用戶，拋出 404 錯誤
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw Failure.NotFound(title: "找不到指定的用戶");

        // ========== 第二步：建構查詢條件 ==========
        // 使用 BuildQuery 方法建立篩選邏輯，包含分頁
        // 這個方法會回傳一個 Func，用來篩選 IQueryable<Identity>
        var query = BuildQuery(request);

        // ========== 第三步：查詢用戶的身份認證資訊 ==========
        // 使用 IUserRepository.GetAllAsync() 查詢用戶的身份認證資訊
        // 傳入剛建立的查詢條件
        var identities = await _userRepository.GetIdentitiesAsync(request.UserId,query);

        // ========== 第四步：將查詢結果包裝成 Pagination 物件回傳 ==========
        // 使用 Id 作為游標選擇器
        // 這樣前端可以記錄最後一筆資料的 Id，用於下一頁查詢
        return new Pagination<Identity>(
            items: identities,
            requestedSize: request.Size,
            cursorSelector: x => x.Id.ToString()
        );
    }

    /// <summary>
    /// 建構身份認證資訊查詢表達式
    /// 
    /// 根據 request 中的條件，動態建立出一個 Func，用來篩選 IQueryable<Identity>
    /// 這種寫法可以將查詢邏輯集中在這裡，保持 Repository 介面的簡潔
    /// </summary>
    /// <param name="request">身份認證資訊查詢請求物件，內含所有篩選條件</param>
    /// <returns>
    /// 回傳一個 Func<IQueryable<Identity>, IQueryable<Identity>>
    /// 輸入一個 IQueryable<Identity>，經過 Where、OrderBy、Take 等操作後，輸出另一個 IQueryable<Identity>
    /// </returns>
    private static Func<IQueryable<Identity>, IQueryable<Identity>> BuildQuery(IdentitiesQuery request)
    {
        // 回傳一個委派，這個委派接受原始的 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：套用 Keyset Pagination (基於 Id 的游標分頁) =====
            // 這種分頁方式比傳統的 Skip 更穩定，尤其是資料量大時
            if (!string.IsNullOrEmpty(request.Cursor) && int.TryParse(request.Cursor, out var cursorId))
            {
                // 只取 Id 大於 Cursor 的資料（假設 Id 是遞增的）
                // 這意味著前端需要記住最後一筆資料的 Id，並在下一頁請求時傳回來
                query = query.Where(x => x.Id > cursorId);
            }

            // ===== 第二階段：按 Id 排序 =====
            // 確保資料按 Id 順序排列，這樣游標分頁才能正常運作
            query = query.OrderBy(x => x.Id);

            // ===== 第三階段：限制回傳筆數 =====
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
