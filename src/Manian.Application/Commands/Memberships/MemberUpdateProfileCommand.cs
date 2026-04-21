using Manian.Application.Commands.Assets;
using Manian.Application.Models.Memberships;
using Manian.Application.Services;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships;

/// <summary>
/// 更新會員個人資料命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝會員更新個人資料所需的資訊
/// 
/// 設計模式：
/// - 實作 IRequest<ProfileResponse>，表示這是一個會回傳更新後資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 MemberUpdateProfileCommandHandler 配合使用，完成個人資料更新
/// 
/// 使用場景：
/// - 會員在個人資料頁面修改資料
/// - 會員更新基本資訊（姓名、生日、性別等）
/// - 會員更新頭像
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 只更新非 null 的欄位，保持 null 欄位的原值不變
/// - 不允許更新 Email（需要單獨的驗證流程）
/// - 不允許更新密碼（使用 ResetPasswordConfirmedCommand）
/// - 不允許更新點數（通過 PointTransaction 處理）
/// 
/// 注意事項：
/// - 更新操作不可逆，建議在 UI 層加入確認對話框
/// - 建議在更新前驗證資料格式
/// - 頭像 URL 應該是已上傳的資產 URL
/// 
/// 與其他命令的對比：
/// - UserUpdateCommand：管理員更新用戶資料（可更新 Email、Status 等）
/// - MemberUpdateProfileCommand：會員更新自己的資料（限制可更新欄位）
/// - ResetPasswordConfirmedCommand：更新密碼
/// </summary>
public class MemberUpdateProfileCommand : IRequest<ProfileResponse>
{
    /// <summary>
    /// 顯示名稱
    /// 
    /// 用途：
    /// - 用戶在系統中顯示的名稱
    /// - 不同於真實姓名，可以是暱稱
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，不能為空字串
    /// - 建議長度限制：1-50 字元
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new MemberUpdateProfileCommand { 
    ///     DisplayName = "小明" 
    /// };
    /// </code>
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 用戶真實姓名
    /// 
    /// 用途：
    /// - 用戶的真實姓名
    /// - 用於配送、發票等正式文件
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，不能為空字串
    /// - 建議長度限制：1-50 字元
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new MemberUpdateProfileCommand { 
    ///     FullName = "王小明" 
    /// };
    /// </code>
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// 用戶生日
    /// 
    /// 用途：
    /// - 用戶的出生日期
    /// - 用於生日優惠、年齡驗證等
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，必須是有效的日期
    /// - 不能是未來的日期
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new MemberUpdateProfileCommand { 
    ///     BirthDate = new DateOnly(1990, 1, 1) 
    /// };
    /// </code>
    /// </summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// 用戶性別
    /// 
    /// 用途：
    /// - 用戶的性別
    /// - 用於統計、推薦等
    /// 
    /// 可選值：
    /// - "male"：男性
    /// - "female"：女性
    /// - "other"：其他
    /// - null：不更新此欄位
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，必須是上述三個值之一
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new MemberUpdateProfileCommand { 
    ///     Gender = "male" 
    /// };
    /// </code>
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 用戶頭像 URL
    /// 
    /// 用途：
    /// - 用戶的頭像圖片 URL
    /// - 顯示在個人資料頁面、評論區等
    /// 
    /// 驗證規則：
    /// - 可以為 null（表示不更新此欄位）
    /// - 如果提供，應該是已上傳的資產 URL
    /// - 建議格式：/assets/{id}.{ext}
    /// 
    /// 使用範例：
    /// <code>
    /// var command = new MemberUpdateProfileCommand { 
    ///     Avatar = "/assets/123.jpg" 
    /// };
    /// </code>
    /// 
    /// 注意事項：
    /// - 頭像應該先通過 AssetAddCommand 上傳
    /// - 建議限制頭像圖片大小和格式
    /// </summary>
    public string? Avatar { get; set; }
}

/// <summary>
/// 更新會員個人資料命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 MemberUpdateProfileCommand 命令
/// - 獲取當前登入用戶的 ID
/// - 查詢用戶資料
/// - 更新用戶資料
/// - 回傳更新後的資料
/// 
/// 設計模式：
/// - 實作 IRequestHandler<MemberUpdateProfileCommand, ProfileResponse> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IUserRepository 和 IUserClaim
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查頭像 URL 是否有效
/// - 未檢查生日是否合理
/// - 建議在實際專案中加入這些檢查
/// 
/// 參考實作：
/// - ProfileQueryHandler：類似的用戶資料查詢邏輯
/// - UserUpdateHandler：類似的更新邏輯
/// - ResetPasswordConfirmedHandler：類似的驗證邏輯
/// </summary>
public class MemberUpdateProfileCommandHandler : IRequestHandler<MemberUpdateProfileCommand, ProfileResponse>
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
    /// - 擴展了 GetByEmailAsync、GetPointTransactionsAsync 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

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
    /// 
    /// 參數：
    /// - userRepository：用戶倉儲，用於查詢和更新用戶資料
    /// - userClaim：使用者身份服務，用於獲取當前登入用戶 ID
    /// - mediator：中介者，用於發送其他命令
    /// </summary>
    private readonly IMediator _mediator;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於查詢和更新用戶資料</param>
    /// <param name="userClaim">使用者身份服務，用於獲取當前登入用戶 ID</param>
    /// <param name="mediator">中介者，用於發送其他命令</param>
    public MemberUpdateProfileCommandHandler(
        IUserRepository userRepository,
        IUserClaim userClaim,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _userClaim = userClaim;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理更新會員個人資料命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 獲取當前登入用戶的 ID
    /// 2. 查詢用戶資料
    /// 3. 驗證用戶是否存在
    /// 4. 更新非 null 的欄位
    /// 5. 儲存變更
    /// 6. 映射並回傳更新後的資料
    /// 
    /// 錯誤處理：
    /// - 用戶不存在：拋出 Failure.NotFound("找不到指定的用戶")
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 不允許更新 Email、密碼、點數等敏感欄位
    /// 
    /// 資料映射：
    /// - 使用 Mapster 將 User 實體映射為 ProfileResponse（見 Mappers/Memberships/UserMap.cs）
    /// - ProfileResponse 繼承自 UserBase，額外包含 Points 屬性
    /// - Points 來自 User.PointAccount.Balance
    /// </summary>
    /// <param name="request">更新會員個人資料命令物件，包含要更新的欄位</param>
    /// <returns>更新後的用戶資料 DTO（ProfileResponse）</returns>
    public async Task<ProfileResponse> HandleAsync(MemberUpdateProfileCommand request)
    {
        // ========== 第一步：獲取當前登入用戶 ID ==========
        // 從 IUserClaim 服務獲取當前登入用戶的唯一識別碼
        // 這個值來自 JWT Token 的 "sub" 宣告，已經過驗證
        var userId = _userClaim.Id;

        // ========== 第二步：查詢用戶資料 ==========
        // 使用泛型方法 GetByIdAsync<User> 查詢用戶資料
        // 包含 PointAccount 導航屬性，以便後續映射
        var user = await _userRepository.GetByIdAsync(userId);

        // ========== 第三步：驗證用戶是否存在 ==========
        // 如果找不到用戶資料，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 用戶已被刪除（軟刪除）
        // - 資料庫中的資料不一致
        // - Token 中的 ID 與資料庫不匹配
        if (user == null)
            throw Failure.NotFound("找不到指定的用戶");

        // ========== 第四步：更新非 null 的欄位 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        if (!string.IsNullOrEmpty(request.DisplayName))
            user.DisplayName = request.DisplayName;
        
        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;
        
        if (request.BirthDate.HasValue)
            user.BirthDate = request.BirthDate.Value;
        
        if (!string.IsNullOrEmpty(request.Gender))
            user.Gender = request.Gender;
        
        if (!string.IsNullOrEmpty(request.Avatar))
        {
            await _mediator.SendAsync(new AssetUpdateCommand()
            {
                Urls = [ request.Avatar ],
                TargetType = "user",
                TargetId = user.Id
            });

            if (!string.IsNullOrEmpty(user.Avatar))
            {
                await _mediator.SendAsync(new AssetDeleteCommand()
                {
                    Urls = [ user.Avatar ]
                });    
            }  

            user.Avatar = request.Avatar;
        }

        // ========== 第五步：儲存變更 ==========
        // 使用 IUserRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _userRepository.SaveChangeAsync();

        // ========== 第六步：映射並回傳更新後的資料 ==========
        // 使用泛型方法 GetByIdAsync<ProfileResponse> 查詢並映射用戶資料
        // Repository 會自動將 User 實體映射為 ProfileResponse DTO
        // 映射規則在 UserMap.cs 中定義
        var profile = await _userRepository.GetByIdAsync<ProfileResponse>(userId);

        if(profile == null)
            throw Failure.BadRequest("更新資料失敗");

        // 回傳 ProfileResponse 對象，包含：
        // - 基本資料（來自 UserBase）：Id, DisplayName, Email 等
        // - 點數資訊（來自 PointAccount.Balance）
        return profile;
    }
}
