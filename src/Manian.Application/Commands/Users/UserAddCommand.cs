using Manian.Application.Commands.Assets;
using Manian.Application.Models.Memberships;
using Manian.Application.Services;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Users;

/// <summary>
/// 新增用戶命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增用戶所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<User>，表示這是一個會回傳 User 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 UserAddHandler 配合使用，完成新增用戶的業務邏輯
/// 
/// 使用場景：
/// - 管理員新增用戶
/// - 使用者註冊（透過 SignupCommand）
/// - 系統批量導入用戶
/// 
/// 設計特點：
/// - 包含用戶基本資訊（姓名、Email 等）
/// - 包含密碼資訊（明文，會在 Handler 中雜湊）
/// - 支援可選屬性（如 Gender、Note 等）
/// 
/// 注意事項：
/// - Email 必須唯一（由資料庫唯一約束保證）
/// - 密碼會在 Handler 中雜湊後儲存
/// </summary>
public class UserAddCommand : IRequest<ProfileResponse>
{
    /// <summary>
    /// 用戶顯示名稱
    /// 
    /// 用途：
    /// - 顯示給其他使用者看的名稱
    /// - 用於用戶搜尋和篩選
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-50 字元
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 用戶真實姓名
    /// 
    /// 用途：
    /// - 用戶的真實姓名
    /// - 用於身份驗證和報表
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-50 字元
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 用戶電子郵件
    /// 
    /// 用途：
    /// - 用戶登入帳號
    /// - 用於接收通知和驗證碼
    /// 
    /// 驗證規則：
    /// - 必須是有效的 Email 格式
    /// - 必須唯一（由資料庫唯一約束保證）
    /// - 建議長度限制：5-100 字元
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 用戶密碼（明文）
    /// 
    /// 用途：
    /// - 用戶登入密碼
    /// - 會在 Handler 中雜湊後儲存
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：8-50 字元
    /// - 建議包含大小寫字母、數字和特殊字元
    /// </summary>
    public string Password { get; set; } = "admin123";

    /// <summary>
    /// 用戶生日
    /// 
    /// 用途：
    /// - 用於年齡驗證
    /// - 用於生日優惠活動
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 必須是有效的日期格式
    /// - 建議限制：不早於 1900 年，不晚於當前日期
    /// </summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// 用戶性別
    /// 
    /// 用途：
    /// - 用於統計分析
    /// - 用於推薦系統
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 可選值：
    /// - "male"：男性
    /// - "female"：女性
    /// - "other"：其他
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 用戶頭像 URL
    /// 
    /// 用途：
    /// - 顯示用戶頭像
    /// - 用於個人資料頁面
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 會員等級
    /// 
    /// 用途：
    /// - 標識用戶的會員等級
    /// - 用於會員權限和優惠
    /// 
    /// 預設值：
    /// - "bronze"（青銅會員）
    /// 
    /// 可選值：
    /// - "bronze"：青銅會員
    /// - "silver"：白銀會員
    /// - "gold"：黃金會員
    /// - "vip"：VIP 會員
    /// </summary>
    public string MembershipLevel { get; set; } = "bronze";

    /// <summary>
    /// 用戶狀態
    /// 
    /// 用途：
    /// - 控制用戶是否可以登入和使用系統
    /// 
    /// 預設值：
    /// - "active"（啟用）
    /// 
    /// 可選值：
    /// - "active"：啟用
    /// - "suspended"：停用
    /// - "deleted"：已刪除
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// 備註
    /// 
    /// 用途：
    /// - 管理員對用戶的備註
    /// - 用於記錄用戶特殊情況
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 建議長度限制：0-500 字元
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// 新增用戶命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 UserAddCommand 命令
/// - 驗證 Email 是否重複
/// - 雜湊密碼
/// - 建立新的 User 實體
/// - 建立對應的 PointAccount 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 User 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<UserAddCommand, User> 介面
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
/// 
/// 參考實作：
/// - CategoryAddHandler：類似的新增邏輯
/// - ProductAddHandler：類似的新增邏輯
/// </summary>
public class UserAddHandler : IRequestHandler<UserAddCommand, ProfileResponse>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<User>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 密碼服務介面
    /// 
    /// 用途：
    /// - 雜湊密碼
    /// - 驗證密碼
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/PasswordService.cs
    /// - 使用 BCrypt 或其他雜湊演算法
    /// </summary>
    private readonly IPasswordService _passwordService;

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
    /// 中介者服務
    /// 
    /// 用途：
    /// - 傳遞命令和事件
    /// - 處理跨邊界邏輯
    /// 
    /// 實作方式：
    /// - 使用 MediatR 框架（見 Infrastructure/MediatR/MediatorExtensions.cs）
    /// - 提供 SendAsync 方法傳遞命令和事件
    /// </summary>
    private readonly IMediator _mediator;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於新增用戶</param>
    /// <param name="passwordService">密碼服務，用於雜湊密碼</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生用戶 ID</param>
    /// <param name="mediator">中介者服務，用於傳遞命令和事件</param>
    public UserAddHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IUniqueIdentifier uniqueIdentifier,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _uniqueIdentifier = uniqueIdentifier;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理新增用戶命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證 Email 是否重複
    /// 2. 雜湊密碼
    /// 3. 建立新的 User 實體
    /// 4. 建立對應的 PointAccount 實體
    /// 5. 將實體加入倉儲
    /// 6. 儲存變更到資料庫
    /// 7. 更新資產庫（如果有頭像）
    /// 8. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - Email 重複：拋出 Failure.BadRequest("Email 已被使用")
    /// - 儲存失敗：拋出 Failure.BadRequest("新增用戶失敗")
    /// </summary>
    /// <param name="request">新增用戶命令物件，包含用戶的所有資訊</param>
    /// <returns>儲存後的 User 實體，包含資料庫自動生成的欄位</returns>
    public async Task<ProfileResponse> HandleAsync(UserAddCommand request)
    {
        // ========== 第一步：驗證 Email 是否重複 ==========
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw Failure.BadRequest("Email 已被使用");

        // ========== 第二步：雜湊密碼 ==========
        var passwordHash = _passwordService.Hash(request.Password);

        // ========== 第三步：建立新的 User 實體 ==========
        var user = new User
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            DisplayName = request.DisplayName,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            BirthDate = request.BirthDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Gender = request.Gender,
            Avatar = request.Avatar ?? "https://demo-po.sgp1.cdn.digitaloceanspaces.com/default_avatar.png",
            MembershipLevel = request.MembershipLevel,
            Status = request.Status,
            Note = request.Note,
            EmailVerified = true,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第四步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _userRepository.Add(user);

        // ========== 第五步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        await _userRepository.SaveChangeAsync();

        // ========== 第六步：更新資產庫 ==========
        // 將頭像圖片更新到資產庫
        if (!string.IsNullOrEmpty(request.Avatar))
        {
            await _mediator.SendAsync(new AssetUpdateCommand()
            {
                Urls = [ request.Avatar ],
                TargetType = "user",
                TargetId = user.Id
            });   
        }

        // ========== 第七步：回傳儲存後的實體 ==========
        // 這會從資料庫中重新讀取實體，包含資料庫自動生成的欄位
        var profile = await _userRepository.GetAsync<ProfileResponse>(
            q => q.Where(u => u.Id == user.Id)
        );

        // 如果找不到對應的實體，則拋出錯誤
        if (profile == null)
            throw Failure.NotFound("建立用戶失敗");

        // 返回用戶資料
        return profile;

    }
}
