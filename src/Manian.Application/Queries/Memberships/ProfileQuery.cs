using Manian.Application.Models.Memberships;
using Manian.Application.Services;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Memberships;

/// <summary>
/// 查詢當前登入用戶的個人資料請求
/// 
/// 設計模式：
/// - 實作 IRequest<ProfileResponse>，表示這是一個查詢請求
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ProfileQueryHandler 配合使用，完成用戶資料查詢
/// 
/// 使用場景：
/// - 用戶登入後查看個人資料頁面
/// - API 端點：GET /api/profile
/// - 需要身份認證（通過 Cookie 或 JWT Token）
/// </summary>
public class ProfileQuery : IRequest<ProfileResponse>;

/// <summary>
/// 用戶資料查詢處理器
/// 
/// 職責：
/// - 接收 ProfileQuery 請求
/// - 從資料庫查詢當前登入用戶的資料
/// - 將 User 實體映射為 ProfileResponse DTO
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ProfileQuery, ProfileResponse> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// </summary>
public class ProfileQueryHandler : IRequestHandler<ProfileQuery, ProfileResponse>
{
    /// <summary>
    /// 使用者倉儲介面
    /// 
    /// 用途：
    /// - 查詢使用者資料
    /// - 提供資料庫操作的抽象介面
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/UserRepository.cs）
    /// - 提供泛型方法 GetByIdAsync<T>，可將實體映射為指定類型
    /// </summary>
    private readonly IUserRepository _accountRepository;

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
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="accountRepository">使用者倉儲，用於查詢使用者資料</param>
    /// <param name="userClaim">使用者身份服務，用於獲取當前登入用戶 ID</param>
    public ProfileQueryHandler(
        IUserRepository accountRepository, 
        IUserClaim userClaim)
    {
        _accountRepository = accountRepository;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理用戶資料查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 獲取當前登入用戶的 ID
    /// 2. 從資料庫查詢用戶資料並映射為 ProfileResponse
    /// 3. 驗證資料存在性
    /// 4. 返回用戶資料
    /// 
    /// 資料映射：
    /// - 使用 Mapster 將 User 實體映射為 ProfileResponse（見 Mappers/Memberships/UserMap.cs）
    /// - ProfileResponse 繼承自 UserBase，額外包含 Points 屬性
    /// - Points 來自 User.PointAccount.Balance
    /// 
    /// 錯誤處理：
    /// - 用戶不存在：拋出 Failure.NotFound("找不到指定的用戶")
    /// </summary>
    /// <param name="request">用戶資料查詢請求物件（不包含任何屬性）</param>
    /// <returns>用戶資料 DTO（ProfileResponse），包含基本資料和點數資訊</returns>
    public async Task<ProfileResponse> HandleAsync(ProfileQuery request)
    {
        // ========== 第一步：獲取當前登入用戶 ID ==========
        // 從 IUserClaim 服務獲取當前登入用戶的唯一識別碼
        // 這個值來自 JWT Token 的 "sub" 宣告，已經過驗證
        var userId = _userClaim.Id;

        // ========== 第二步：查詢用戶資料並映射 ==========
        // 使用泛型方法 GetByIdAsync<ProfileResponse> 查詢用戶資料
        // Repository 會自動將 User 實體映射為 ProfileResponse DTO
        // 映射規則在 UserMap.cs 中定義
        var profile = await _accountRepository.GetByIdAsync<ProfileResponse>(userId);

        // ========== 第三步：驗證資料存在性 ==========
        // 如果找不到用戶資料，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 用戶已被刪除（軟刪除）
        // - 資料庫中的資料不一致
        // - Token 中的 ID 與資料庫不匹配
        if(profile == null)
            throw Failure.NotFound("找不到指定的用戶");

        // ========== 第四步：返回用戶資料 ==========
        // 返回 ProfileResponse 對象，包含：
        // - 基本資料（來自 UserBase）：Id, DisplayName, Email 等
        // - 點數資訊（來自 PointAccount.Balance）
        return profile;
    }
}
