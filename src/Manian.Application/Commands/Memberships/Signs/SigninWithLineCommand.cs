using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Manian.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships.Signs;

/// <summary>
/// Line 登入命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝 Line 登入所需的資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<User>，表示這是一個會回傳 User 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SigninWithLineHandler 配合使用，完成 Line 登入流程
/// 
/// 使用場景：
/// - 使用者通過 Line 登入
/// - Line 回調返回使用者資訊後，發送此命令進行登入處理
/// 
/// 設計特點：
/// - 包含 ProviderUid（Line 使用者唯一識別碼）
/// - 包含 DisplayName（顯示名稱）
/// - 包含 Email（電子郵箱，可選）
/// </summary>
public class SigninWithLineCommand : IRequest<User>
{
    /// <summary>
    /// Provider Unique Identifier
    /// 
    /// 用途：
    /// - Line 平台提供的唯一使用者識別碼
    /// - 用於識別使用者在 Line 平台上的身份
    /// 
    /// 特性：
    /// - 對於同一個 Line 帳號，這個值是固定的
    /// - 不同應用程式可能會獲得不同的 ProviderUid
    /// 
    /// 使用範例：
    /// - "U1234567890abcdef1234567890abcdef"
    /// </summary>
    public string ProviderUId { get; init; }

    /// <summary>
    /// 顯示名稱
    /// 
    /// 用途：
    /// - 使用者在 Line 平台上設定的顯示名稱
    /// - 用於在系統中顯示使用者名稱
    /// 
    /// 特性：
    /// - 使用者可以在 Line 平台上修改這個名稱
    /// - 系統會在首次登入時使用這個名稱
    /// - 後續登入可能會更新這個名稱（取決於業務邏輯）
    /// 
    /// 使用範例：
    /// - "張三"
    /// - "John Doe"
    /// </summary>
    public string DisplayName { get; init; }
}

/// <summary>
/// Line 登入命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SigninWithLineCommand 命令
/// - 根據 ProviderUid 查找用戶
/// - 如果用戶不存在，則創建新用戶
/// - 返回找到或創建的用戶實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SigninWithLineCommand, User> 介面
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
/// </summary>
public class SigninWithLineHandler : IRequestHandler<SigninWithLineCommand, User>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 提供查詢操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 GetAllAsync 等
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 唯一識別碼產生器服務
    /// 
    /// 用途：
    /// - 產生全域唯一的整數 ID
    /// - 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/Snowflake.cs
    /// - 註冊為單例模式 (Singleton)
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於查詢用戶資料</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生用戶 ID</param>
    public SigninWithLineHandler(IUserRepository userRepository, IUniqueIdentifier uniqueIdentifier)
    {
        _userRepository = userRepository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理 Line 登入命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ProviderUid 查詢用戶
    /// 2. 驗證用戶是否存在
    /// 3. 如果用戶不存在，創建新用戶
    /// 4. 返回找到或創建的用戶實體
    /// 
    /// 錯誤處理：
    /// - 用戶不存在且創建失敗：拋出 Failure.BadRequest("找不到指定的帳號")
    /// </summary>
    /// <param name="request">Line 登入命令物件，包含 ProviderUid、DisplayName 和 Email</param>
    /// <returns>找到或創建的用戶實體，包含角色資訊</returns>
    public async Task<User> HandleAsync(SigninWithLineCommand request)
    {
        // ========== 第一步：根據 ProviderUid 查詢用戶 ==========
        // 使用 IUserRepository.GetAsync() 查詢用戶
        // 使用 Any 方法檢查用戶的 Identities 集合中是否存在符合條件的 Identity
        // Include(x => x.Roles) 用於預先載入用戶的角色資訊
        var user = await _userRepository.GetAsync(query =>
        {
            // 預先載入使用者的角色集合
            query = query.Include(x => x.Roles);

            // 預先載入使用者的身份認證資訊集合
            // 這是關鍵，因為我們需要查詢 Identities 集合
            query = query.Include(x => x.Identities);

            // 根據 ProviderUid 篩選使用者
            // 使用 Any 方法檢查 Identities 集合中是否存在符合條件的 Identity
            // 這會被 EF Core 翻譯成 SQL 的 EXISTS 子查詢
            query = query.Where(user => user.Identities.Any(i => i.ProviderUid == request.ProviderUId));

            return query;
        });

        // ========== 第二步：驗證用戶是否存在 ==========
        // 如果找不到用戶，拋出 404 錯誤
        // 這種情況可能發生在：
        // - ProviderUid 尚未註冊
        // - 用戶已被刪除（軟刪除）
        if (user == null)
        {
            // ========== 第三步：創建新用戶 ==========
            // 產生全域唯一的整數 ID
            var id = _uniqueIdentifier.NextInt();
        
            // 創建新用戶實體
            var newUser = new User
            {
                // 產生全域唯一的整數 ID
                Id = id,
                
                // 設定基本屬性
                DisplayName = request.DisplayName,
                FullName = "",
                Email = "",
                PasswordHash = "",
                MembershipLevel = "bronze",
                Status = "active",
                EmailVerified = false,

                // 創建 Line 身份認證資訊
                Identities = new List<Identity>()
                {
                    new Identity()
                    {
                        Id = _uniqueIdentifier.NextInt(),
                        UserId = id,
                        Provider = "line",
                        ProviderUid = request.ProviderUId,
                    }
                },
                
                // 設定建立時間為目前 UTC 時間
                CreatedAt = DateTimeOffset.UtcNow
            };

            // 將新用戶加入倉儲
            _userRepository.Add(newUser);

            // 儲存變更到資料庫
            await _userRepository.SaveChangeAsync();

            // 重新查詢用戶以確保獲取完整的用戶資訊
            user = await _userRepository.GetAsync(query =>
            {
                // 預先載入使用者的角色集合
                query = query.Include(x => x.Roles);

                // 預先載入使用者的身份認證資訊集合
                query = query.Include(x => x.Identities);

                // 根據 ID 篩選使用者
                query = query.Where(u => u.Id == id);

                return query;
            });
        }

        // ========== 第四步：驗證用戶是否存在 ==========
        // 如果仍然找不到用戶，拋出錯誤
        if (user == null)
            throw Failure.BadRequest("找不到指定的帳號");

        // ========== 第五步：返回找到的用戶實體 ==========
        // 返回用戶實體，包含：
        // - 基本資料（Id, DisplayName, Email 等）
        // - 角色資訊（Roles 集合）
        // - 身份認證資訊（Identities 集合）
        return user;
    }
}
