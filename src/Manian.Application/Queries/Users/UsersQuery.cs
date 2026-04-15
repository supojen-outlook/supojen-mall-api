using Manian.Application.Models;
using Manian.Application.Models.Memberships;
using Manian.Application.Models.Memberships.Base;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Users;

/// <summary>
/// 查詢用戶列表的請求物件
/// 
/// 用途：
/// - 查詢系統中的用戶列表
/// - 支援多種篩選條件和分頁
/// - 用於用戶管理頁面
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<UserBase>>，表示這是一個查詢請求
/// - 回傳用戶列表（包裝於分頁模型中）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 UsersQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 用戶管理頁面顯示用戶列表
/// - 用戶搜尋功能
/// - 用戶統計報表
/// 
/// 設計特點：
/// - 支援多種篩選條件：狀態、會員等級、搜尋關鍵字
/// - 支援游標分頁（基於 CreatedAt）
/// - 回傳 UserBase DTO（不包含敏感資訊如密碼雜湊）
/// 
/// 參考實作：
/// - RolesQuery：查詢角色列表的類似實作
/// - ProductsQuery：查詢商品列表的類似實作
/// </summary>
public class UsersQuery : IRequest<Pagination<ProfileResponse>>
{
    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 用途：
    /// - 在 DisplayName、Email、FullName 中進行模糊搜尋
    /// - 不區分大小寫
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不進行搜尋
    /// - 如果有值，會自動去除前後空白並轉為小寫
    /// 
    /// 使用範例：
    /// <code>
    /// // 搜尋顯示名稱或 Email 包含 "john" 的用戶
    /// var query = new UsersQuery { Search = "john" };
    /// </code>
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 用戶狀態篩選（可選）
    /// 
    /// 用途：
    /// - 篩選特定狀態的用戶
    /// - 支援的值：active（啟用）、suspended（停用）、deleted（已刪除）
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不篩選狀態
    /// - 如果有值，只回傳符合該狀態的用戶
    /// 
    /// 使用範例：
    /// <code>
    /// // 只查詢啟用的用戶
    /// var query = new UsersQuery { Status = "active" };
    /// </code>
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// 會員等級篩選（可選）
    /// 
    /// 用途：
    /// - 篩選特定會員等級的用戶
    /// - 支援的值：bronze（青銅）、silver（白銀）、gold（黃金）、vip（VIP）
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不篩選會員等級
    /// - 如果有值，只回傳符合該會員等級的用戶
    /// 
    /// 使用範例：
    /// <code>
    /// // 只查詢 VIP 會員
    /// var query = new UsersQuery { MembershipLevel = "vip" };
    /// </code>
    /// </summary>
    public string? MembershipLevel { get; init; }

    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 用於游標分頁
    /// - 值為上一頁最後一筆資料的 CreatedAt（轉為 ISO 8601 字串格式）
    /// - 只回傳 CreatedAt 大於此值的資料
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示從第一頁開始
    /// - 必須是有效的 ISO 8601 日期時間字串格式
    /// - 例如："2023-01-01T00:00:00Z"
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢 CreatedAt 大於 2023-01-01 的用戶
    /// var query = new UsersQuery { Cursor = "2023-01-01T00:00:00Z" };
    /// </code>
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>
    /// 每頁資料筆數（可選）
    /// 
    /// 用途：
    /// - 控制每頁回傳的資料筆數
    /// - 預設值為 20
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 建議範圍：1-100
    /// 
    /// 使用範例：
    /// <code>
    /// // 每頁回傳 50 筆資料
    /// var query = new UsersQuery { Size = 50 };
    /// </code>
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 用戶查詢處理器
/// 
/// 職責：
/// - 接收 UsersQuery 請求
/// - 根據篩選條件查詢用戶
/// - 將查詢結果映射為 UserBase DTO
/// - 將結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<UsersQuery, Pagination<UserBase>> 介面
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
/// - 支援多種篩選條件：狀態、會員等級、搜尋關鍵字
/// - 支援游標分頁（基於 CreatedAt）
/// - 使用 Mapster 將 User 實體映射為 UserBase DTO
/// - 統一回傳格式為 Pagination，方便前端處理
/// 
/// 參考實作：
/// - RolesQueryHandler：查詢角色列表的類似實作
/// - ProductsQueryHandler：查詢商品列表的類似實作
/// </summary>
public class UsersQueryHandler : IRequestHandler<UsersQuery, Pagination<ProfileResponse>>
{
    /// <summary>
    /// 使用者倉儲介面
    /// 
    /// 用途：
    /// - 存取使用者資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">使用者倉儲，用於查詢用戶資料</param>
    public UsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 處理用戶查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 UsersQuery 請求
    /// 2. 建構查詢條件
    /// 3. 呼叫 Repository 的 GetAllAsync 方法取得資料
    /// 4. 將 User 實體映射為 UserBase DTO
    /// 5. 將結果包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 支援多種篩選條件：狀態、會員等級、搜尋關鍵字
    /// - 支援游標分頁（基於 CreatedAt）
    /// - 按建立時間排序（由 Repository 實作）
    /// 
    /// 錯誤處理：
    /// - 如果沒有符合條件的用戶，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 範例 1：查詢所有啟用的用戶
    /// var query1 = new UsersQuery { Status = "active" };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：搜尋顯示名稱或 Email 包含 "john" 的 VIP 會員
    /// var query2 = new UsersQuery { Search = "john", MembershipLevel = "vip" };
    /// var result2 = await _mediator.SendAsync(query2);
    /// 
    /// // 範例 3：查詢 CreatedAt 大於 2023-01-01 的用戶，每頁 50 筆
    /// var query3 = new UsersQuery { 
    ///     Cursor = "2023-01-01T00:00:00Z", 
    ///     Size = 50 
    /// };
    /// var result3 = await _mediator.SendAsync(query3);
    /// </code>
    /// </summary>
    /// <param name="request">用戶查詢請求物件，包含篩選條件和分頁參數</param>
    /// <returns>包含符合條件用戶的分頁模型</returns>
    public async Task<Pagination<ProfileResponse>> HandleAsync(UsersQuery request)
    {
        // ========== 第一步：建構查詢條件 ==========
        // 使用 BuildQuery 方法建立篩選邏輯
        // 這個方法會根據 request 中的條件，動態建立出一個 Func
        // 用於篩選 IQueryable<User>
        var queryFunc = BuildQuery(request);

        // ========== 第二步：執行查詢並映射結果 ==========
        // 呼叫 Repository 的 GetAllAsync 方法
        // 傳入查詢條件和映射配置
        // Repository 會執行查詢並將 User 實體映射為 UserBase DTO
        var users = await _userRepository.GetAllAsync<ProfileResponse>(queryFunc);

        // ========== 第三步：將結果包裝成 Pagination 物件回傳 ==========
        // 使用游標分頁邏輯
        // cursorSelector 指定使用 CreatedAt 作為游標
        return new Pagination<ProfileResponse>(
            items: users,
            requestedSize: request.Size,
            cursorSelector: x => x.Id.ToString() // 使用 ISO 8601 格式
        );
    }

    /// <summary>
    /// 建構用戶查詢表達式
    /// 
    /// 職責：
    /// - 根據 request 中的條件，動態建立查詢表達式
    /// - 將查詢邏輯集中在這裡，保持 Repository 介面的簡潔
    /// 
    /// 設計考量：
    /// - 使用表達式樹（Expression Tree）建立查詢條件
    /// - 支援多種篩選條件的組合
    /// - 保持查詢邏輯的清晰和可維護性
    /// 
    /// 參數說明：
    /// - request：用戶查詢請求物件，包含所有篩選條件
    /// 
    /// 回傳值：
    /// - Func<IQueryable<User>, IQueryable<UserBase>>：查詢表達式
    ///   - 輸入：User 實體的查詢物件
    ///   - 輸出：UserBase DTO 的查詢物件
    /// </summary>
    /// <param name="request">用戶查詢請求物件，內含所有篩選條件</param>
    /// <returns>
    /// 回傳一個 Func<IQueryable<User>, IQueryable<UserBase>>
    /// 輸入一個 IQueryable<User>，經過 Where、OrderBy 等操作後，輸出 IQueryable<UserBase>
    /// 這讓 Repository 的 GetAllAsync 方法可以保持單純，只需執行這個 Func 即可
    /// </returns>
    private static Func<IQueryable<User>, IQueryable<User>> BuildQuery(UsersQuery request)
    {
        // 回傳一個委派，這個委派接受原始的 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：套用搜尋條件 =====
            // 如果 Search 參數有值，就在 DisplayName、Email、FullName 中進行模糊搜尋
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower(); // 前後去空白並轉小寫，達到不區分大小寫的效果
                query = query.Where(x =>
                    x.Id.ToString().ToLower().Contains(searchTerm) ||  // 用戶 ID 包含關鍵字
                    x.DisplayName.ToLower().Contains(searchTerm) ||    // 顯示名稱包含關鍵字
                    x.Email.ToLower().Contains(searchTerm) ||          // Email 包含關鍵字
                    x.FullName.ToLower().Contains(searchTerm)          // 姓名包含關鍵字
                );
            }

            // ===== 第二階段：套用狀態篩選 =====
            // 如果 Status 參數有值，只回傳符合該狀態的用戶
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }

            // ===== 第三階段：套用會員等級篩選 =====
            // 如果 MembershipLevel 參數有值，只回傳符合該會員等級的用戶
            if (!string.IsNullOrWhiteSpace(request.MembershipLevel))
            {
                query = query.Where(x => x.MembershipLevel == request.MembershipLevel);
            }

            // ===== 第四階段：套用游標分頁 =====
            // 如果 Cursor 參數有值，只回傳 CreatedAt 大於該值的用戶
            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                // 將字串格式的游標轉換為 DateTimeOffset
                if (DateTimeOffset.TryParse(request.Cursor, out var cursorValue))
                {
                    query = query.Where(x => x.CreatedAt > cursorValue);
                }
            }

            // ===== 第五階段：排序 =====
            // 按建立時間降序排序（最新的在前）
            query = query.OrderByDescending(x => x.CreatedAt);

            // ===== 第六階段：限制回傳筆數 =====
            // 如果 Size 參數有值，只取前 Size 筆
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
