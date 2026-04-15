using Manian.Application.Commands.Assets;
using Manian.Application.Models.Memberships;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Users;

/// <summary>
/// 更新用戶命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝更新用戶所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<User>，表示這是一個會回傳 User 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 UserUpdateHandler 配合使用，完成更新用戶的業務邏輯
/// 
/// 使用場景：
/// - 管理員更新用戶資料
/// - 用戶更新個人資料
/// - 系統自動更新用戶狀態
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 不包含密碼更新（使用 ResetPasswordCommand）
/// - 支援可選屬性（只更新非 null 的欄位）
/// 
/// 注意事項：
/// - Email 更新需要驗證唯一性
/// - 頭像更新會同步更新資產庫
/// </summary>
public class UserUpdateCommand : IRequest<ProfileResponse>
{
    /// <summary>
    /// 用戶唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的用戶
    /// - 必須是資料庫中已存在的用戶 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的用戶
    /// 
    /// 錯誤處理：
    /// - 如果用戶不存在，會拋出 Failure.NotFound("找不到指定的用戶")
    /// </summary>
    public int Id { get; set; }

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
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public string? DisplayName { get; set; }

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
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public string? FullName { get; set; }

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
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// - 如果有值，需要驗證是否與其他用戶重複
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 用戶生日
    /// 
    /// 用途：
    /// - 用於年齡驗證
    /// - 用於生日優惠活動
    /// 
    /// 驗證規則：
    /// - 必須是有效的日期格式
    /// - 建議限制：不早於 1900 年，不晚於當前日期
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// 用戶性別
    /// 
    /// 用途：
    /// - 用於統計分析
    /// - 用於推薦系統
    /// 
    /// 可選值：
    /// - "male"：男性
    /// - "female"：女性
    /// - "other"：其他
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 用戶頭像 URL
    /// 
    /// 用途：
    /// - 顯示用戶頭像
    /// - 用於個人資料頁面
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// - 如果有值，會同步更新資產庫
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 會員等級
    /// 
    /// 用途：
    /// - 標識用戶的會員等級
    /// - 用於會員權限和優惠
    /// 
    /// 可選值：
    /// - "bronze"：青銅會員
    /// - "silver"：白銀會員
    /// - "gold"：黃金會員
    /// - "vip"：VIP 會員
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public string? MembershipLevel { get; set; }

    /// <summary>
    /// 用戶狀態
    /// 
    /// 用途：
    /// - 控制用戶是否可以登入和使用系統
    /// 
    /// 可選值：
    /// - "active"：啟用
    /// - "suspended"：停用
    /// - "deleted"：已刪除
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 備註
    /// 
    /// 用途：
    /// - 管理員對用戶的備註
    /// - 用於記錄用戶特殊情況
    /// 
    /// 驗證規則：
    /// - 建議長度限制：0-500 字元
    /// 
    /// 注意事項：
    /// - 如果為 null，則不更新此欄位
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// 更新用戶命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 UserUpdateCommand 命令
/// - 查詢用戶是否存在
/// - 驗證 Email 是否重複（如果更新 Email）
/// - 更新用戶資訊
/// - 更新資產庫（如果更新頭像）
/// - 回傳更新後的 User 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<UserUpdateCommand, User> 介面
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
/// - CategoryUpdateHandler：類似的更新邏輯
/// - ProductUpdateHandler：類似的更新邏輯
/// </summary>
public class UserUpdateHandler : IRequestHandler<UserUpdateCommand, ProfileResponse>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 繼承自 Repository<User>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

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
    /// <param name="userRepository">用戶倉儲，用於查詢和更新用戶</param>
    /// <param name="mediator">中介者服務，用於傳遞命令和事件</param>
    public UserUpdateHandler(
        IUserRepository userRepository,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理更新用戶命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢用戶實體
    /// 2. 驗證用戶是否存在
    /// 3. 驗證 Email 是否重複（如果更新 Email）
    /// 4. 更新用戶資訊（只更新非 null 的欄位）
    /// 5. 儲存變更到資料庫
    /// 6. 更新資產庫（如果更新頭像）
    /// 7. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - 用戶不存在：拋出 Failure.NotFound("找不到指定的用戶")
    /// - Email 重複：拋出 Failure.BadRequest("Email 已被使用")
    /// - 儲存失敗：拋出 Failure.BadRequest("更新用戶失敗")
    /// </summary>
    /// <param name="request">更新用戶命令物件，包含用戶 ID 和要更新的欄位</param>
    /// <returns>更新後的 User 實體，包含資料庫自動更新的欄位</returns>
    public async Task<ProfileResponse> HandleAsync(UserUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢用戶實體 ==========
        // 使用 IUserRepository.GetByIdAsync() 查詢用戶
        // 這個方法會從資料庫中取得完整的用戶實體
        var user = await _userRepository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證用戶是否存在 ==========
        // 如果找不到用戶，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 用戶 ID 不存在
        // - 用戶已被刪除（軟刪除）
        if (user == null)
            throw Failure.NotFound(title: "找不到指定的用戶");

        // ========== 第三步：驗證 Email 是否重複（如果更新 Email） ==========
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                throw Failure.BadRequest("Email 已被使用");
        }

        // ========== 第四步：更新用戶資訊 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        if (!string.IsNullOrEmpty(request.DisplayName))
            user.DisplayName = request.DisplayName;

        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;

        if (request.BirthDate.HasValue)
            user.BirthDate = request.BirthDate.Value;

        if (!string.IsNullOrEmpty(request.Gender))
            user.Gender = request.Gender;

        if (!string.IsNullOrEmpty(request.Avatar))
            user.Avatar = request.Avatar;

        if (!string.IsNullOrEmpty(request.MembershipLevel))
            user.MembershipLevel = request.MembershipLevel;

        if (!string.IsNullOrEmpty(request.Status))
            user.Status = request.Status;

        if (request.Note != null)
            user.Note = request.Note;

        // 設定更新時間為目前 UTC 時間
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // ========== 第五步：儲存變更到資料庫 ==========
        // 使用 IUserRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
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

        // ========== 第七步：回傳更新後的實體 ==========

        // 使用 IUserRepository.GetAsync() 取得更新後的實體
        var profile = await _userRepository.GetAsync<ProfileResponse>(
            q => q.Where(u => u.Id == request.Id)
        );

        // 如果找不到用戶，拋出 400 錯誤
        if (profile == null)
            throw Failure.BadRequest("更新用戶失敗");

        // 回傳更新後的實體
        return profile;
    }
}
