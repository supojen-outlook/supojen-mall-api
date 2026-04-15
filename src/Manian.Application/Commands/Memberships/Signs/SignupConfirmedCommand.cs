using Manian.Application.Services;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships.Signs;

/// <summary>
/// 註冊確認命令類別 (CQRS 模式中的 Command)
/// 
/// 用途：封裝使用者完成註冊流程所需的所有資訊
/// 
/// 設計模式：
/// - 實作 IRequest 介面，表示這是一個不回傳資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SignupWithEmailCommand 配合使用，完成兩階段註冊流程
/// 
/// 註冊流程：
/// 1. 第一階段：使用者輸入 Email → 系統發送驗證碼 (SignupWithEmailCommand)
/// 2. 第二階段：使用者輸入 Email、密碼、驗證碼 → 完成註冊 (SignupConfirmedCommand)
/// 
/// 為什麼要分兩階段？
/// - 驗證 Email 的真實性（防止使用假 Email）
/// - 防止惡意註冊（需要真實 Email 才能收到驗證碼）
/// - 減少垃圾帳號（增加註冊成本）
/// </summary>
public class SignupConfirmedCommand : IRequest
{
    /// <summary>
    /// 使用者電子郵件地址
    /// 
    /// 用途：
    /// - 作為使用者的登入帳號
    /// - 接收系統通知和驗證碼
    /// - 作為使用者的唯一識別之一
    /// 
    /// 驗證規則：
    /// - 必須符合 Email 格式規範
    /// - 必須是唯一且未註冊過的 Email
    /// - 必須已通過驗證碼驗證
    /// 
    /// 儲存位置：
    /// - 存儲在 User.Email 欄位
    /// - 註冊時會將 EmailVerified 設為 true
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 使用者密碼（明碼）
    /// 
    /// 安全性處理：
    /// - 絕不會以明碼形式儲存於資料庫
    /// - 會透過 IPasswordService.Hash() 進行加鹽雜湊
    /// - 雜湊結果儲存在 User.PasswordHash 欄位
    /// 
    /// 密碼要求：
    /// - 建議長度至少 8 個字元
    /// - 建議包含大小寫字母、數字和特殊符號
    /// - 必須與 PasswordComfirmed 完全一致
    /// 
    /// 為什麼要雜湊？
    /// - 即使資料庫外洩，攻擊者也無法還原出原始密碼
    /// - 每個密碼使用獨立的隨機鹽，防止彩虹表攻擊
    /// - 使用 PBKDF2 等金鑰衍生函數，增加暴力破解成本
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 密碼確認欄位
    /// 
    /// 用途：
    /// - 確保使用者兩次輸入的密碼完全一致
    /// - 防止使用者因輸入錯誤而設定了自己不知道的密碼
    /// 
    /// 驗證邏輯：
    /// - 在 Handler 中會比對 Password 和 PasswordComfirmed
    /// - 如果不一致，會拋出 Failure.BadRequest 例外
    /// - 錯誤訊息：「兩次輸入的密碼不一致！」
    /// 
    /// 注意事項：
    /// - 這個欄位不會儲存到資料庫
    /// - 只在註冊時用於前端驗證
    /// - 驗證通過後即可忽略
    /// </summary>
    public string PasswordComfirmed { get; set; }

    /// <summary>
    /// Email 驗證碼
    /// 
    /// 用途：
    /// - 驗證 Email 的真實性和所有權
    /// - 確保註冊者可以存取該 Email 信箱
    /// 
    /// 驗證流程：
    /// 1. 使用者先呼叫 SignupWithEmailCommand，系統發送驗證碼到 Email
    /// 2. 使用者輸入收到的驗證碼
    /// 3. 系統透過 IValidationCodeService.ValidateCode() 驗證
    /// 
    /// 驗證碼特性：
    /// - 6 位數字（由 ValidationCodeService 產生）
    /// - 有效期 5 分鐘
    /// - 使用後即失效（一次性使用）
    /// - 儲存在記憶體快取中（IMemoryCache）
    /// 
    /// 錯誤處理：
    /// - 驗證碼錯誤：拋出 Failure.BadRequest("驗證碼錯誤！")
    /// - 驗證碼過期：系統自動從快取移除，驗證會失敗
    /// </summary>
    public string Code { get; set; }
}

/// <summary>
/// 註冊確認命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SignupConfirmedCommand 命令
/// - 驗證所有註冊資訊的正確性
/// - 建立新的使用者帳號和相關資料
/// - 將使用者資料儲存到資料庫
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SignupConfirmedCommand> 介面
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
public class SignupConfirmedHandler : IRequestHandler<SignupConfirmedCommand>
{
    /// <summary>
    /// 唯一識別碼產生器服務
    /// 
    /// 用途：
    /// - 為新使用者產生唯一的 ID
    /// - 確保在分散式環境中 ID 也不會重複
    /// 
    /// 實作方式：
    /// - 預設使用 Snowflake 演算法（見 Infrastructure/Services/Snowflake.cs）
    /// - 產生 32 位元整數 ID (int)
    /// 
    /// 為什麼需要這個服務？
    /// - 資料庫自增 ID 在分散式環境中不適用
    /// - GUID 太長且無排序性
    /// - Snowflake 兼具唯一性、排序性和效能
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 驗證碼服務
    /// 
    /// 用途：
    /// - 驗證使用者輸入的驗證碼是否正確
    /// - 確保 Email 的真實性
    /// 
    /// 實作方式：
    /// - 使用記憶體快取儲存驗證碼（見 Infrastructure/Services/ValidationCodeService.cs）
    /// - 驗證碼有效期 5 分鐘
    /// - 驗證後自動失效
    /// </summary>
    private readonly IValidationCodeService _validationCodeService;

    /// <summary>
    /// 使用者倉儲介面
    /// 
    /// 用途：
    /// - 存取使用者資料
    /// - 查詢 Email 是否已註冊
    /// - 儲存新使用者資料
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/UserRepository.cs）
    /// - 提供資料庫操作的抽象介面
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 密碼服務
    /// 
    /// 用途：
    /// - 將明碼密碼雜湊為密文
    /// - 確保密碼安全儲存
    /// 
    /// 實作方式：
    /// - 使用 PBKDF2 演算法（見 Infrastructure/Services/PasswordService.cs）
    /// - 每個密碼使用獨立的隨機鹽
    /// - 迭代 1000 次增加暴力破解成本
    /// </summary>
    private readonly IPasswordService _passwordService;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="uniqueIdentifier">唯一識別碼產生器</param>
    /// <param name="validationCodeService">驗證碼服務</param>
    /// <param name="userRepository">使用者倉儲</param>
    /// <param name="passwordService">密碼服務</param>
    public SignupConfirmedHandler(
        IUniqueIdentifier uniqueIdentifier, 
        IValidationCodeService validationCodeService, 
        IUserRepository userRepository, 
        IPasswordService passwordService)
    {
        _uniqueIdentifier = uniqueIdentifier;
        _validationCodeService = validationCodeService;
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 處理註冊確認命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證驗證碼
    /// 2. 檢查 Email 是否已註冊
    /// 3. 驗證兩次密碼是否一致
    /// 4. 建立新使用者實體
    /// 5. 儲存到資料庫
    /// 
    /// 錯誤處理：
    /// - 驗證碼錯誤：拋出 Failure.BadRequest("驗證碼錯誤！")
    /// - Email 已註冊：拋出 Failure.BadRequest("該郵箱已註冊！")
    /// - 密碼不一致：拋出 Failure.BadRequest("兩次輸入的密碼不一致！")
    /// - 建立失敗：拋出 Failure.BadRequest("用戶創建失敗")
    /// </summary>
    /// <param name="request">註冊確認命令物件，包含 Email、密碼、驗證碼等資訊</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(SignupConfirmedCommand request)
    {
        // ========== 第一步：驗證驗證碼 ==========
        // 使用 IValidationCodeService.ValidateCode() 驗證
        // 參數：Email（作為 key）、驗證碼
        // 如果驗證失敗（驗證碼錯誤或過期），拋出例外
        if(!_validationCodeService.ValidateCode(request.Email, request.Code))
            throw Failure.BadRequest("驗證碼錯誤！");

        // ========== 第二步：檢查 Email 是否已註冊 ==========
        // 使用 IUserRepository.GetByEmailAsync() 查詢
        // 如果找到使用者，表示 Email 已被註冊，拋出例外
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if(user != null)
            throw Failure.BadRequest("該郵箱已註冊！");

        // ========== 第三步：驗證兩次密碼是否一致 ==========
        // 直接比對 Password 和 PasswordComfirmed
        // 如果不一致，拋出例外
        if(request.Password != request.PasswordComfirmed)
            throw Failure.BadRequest("兩次輸入的密碼不一致！");

        // ========== 第四步：建立新使用者實體 ==========
        user = new User
        {
            // 使用 Snowflake 演算法產生唯一 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 使用 PBKDF2 演算法將密碼雜湊後儲存
            // 原始密碼不會被儲存
            PasswordHash = _passwordService.Hash(request.Password),
            
            // 設定使用者狀態為活躍
            // 其他可能的值：suspended（暫停）、deleted（刪除）
            Status = "active",
            
            // 設定建立時間為目前 UTC 時間
            // 使用 UTC 時間可以避免時區問題
            CreatedAt = DateTimeOffset.UtcNow,
            
            // 更新時間初始為 null
            // 表示使用者尚未修改過任何資料
            UpdatedAt = null,
            
            // 設定 Email 並標記為已驗證
            // EmailVerified = true 表示 Email 已通過驗證碼驗證
            Email = request.Email,
            EmailVerified = true,
            
            // 預設顯示名稱為 Email
            // 使用者可以在註冊後自行修改
            DisplayName = request.Email,
            
            // 全名初始為空字串
            // 使用者可以在註冊後自行填寫
            FullName = "",
            
            // 使用預設頭像
            // 這是一個公開的 CDN 連結
            Avatar = "https://demo-po.sgp1.cdn.digitaloceanspaces.com/default_avatar.png",
            
            // 設定會員等級為青銅
            // 其他可能的等級：silver（銀）、gold（金）、vip（VIP）
            MembershipLevel = "bronze"
        };   

        // ========== 第五步：將新使用者加入資料庫 ==========
        // AddAsync 只會將實體加入追蹤，不會立即寫入資料庫
        _userRepository.Add(user);

        // ========== 第六步：儲存變更到資料庫 ==========
        // SaveChangeAsync 會將所有被追蹤的實體變更寫入資料庫
        // 這是工作單元模式的提交點
        await _userRepository.SaveChangeAsync();
    }
}
