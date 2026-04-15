using Manian.Application.Services;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships.Signs;

/// <summary>
/// 密碼重設確認命令類別 (CQRS 模式中的 Command)
/// 
/// 用途：封裝使用者完成密碼重設流程所需的資訊
/// 
/// 設計模式：
/// - 實作 IRequest 介面，表示這是一個不回傳資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ResetPasswordCommand 配合使用，完成兩階段密碼重設流程
/// 
/// 密碼重設流程：
/// 1. 第一階段：使用者輸入 Email → 系統發送驗證碼 (ResetPasswordCommand)
/// 2. 第二階段：使用者輸入 Email、新密碼、驗證碼 → 完成密碼重設 (ResetPasswordConfirmedCommand)
/// 
/// 為什麼要分兩階段？
/// - 驗證 Email 的真實性（防止使用假 Email）
/// - 防止惡意重設他人密碼（需要真實 Email 才能收到驗證碼）
/// - 增加安全性（即使 Email 被駭客入侵，還需要驗證碼）
/// </summary>
public class ResetPasswordConfirmedCommand : IRequest
{
    /// <summary>
    /// 使用者電子郵件地址
    /// 
    /// 用途：
    /// - 作為使用者的識別，用於查找帳號
    /// - 與驗證碼綁定，確保驗證碼只能用於該 Email
    /// 
    /// 驗證規則：
    /// - 必須符合 Email 格式規範
    /// - 必須是已註冊的 Email
    /// - 必須是可接收郵件的真實 Email
    /// 
    /// 安全性考量：
    /// - 即使用戶不存在，也不應明確告知（防止列舉攻擊）
    /// - 但目前實作會拋出「用戶不存在」的錯誤
    /// - 建議改為：不論 Email 是否存在，都回傳「驗證碼已發送」
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 使用者設定的新密碼
    /// 
    /// 用途：
    /// - 取代舊密碼，作為新的登入憑證
    /// 
    /// 密碼規則：
    /// - 應該由前端進行基本驗證（長度、複雜度）
    /// - 後端會使用 IPasswordService.Hash() 進行加鹽雜湊
    /// - 絕不儲存明碼密碼
    /// 
    /// 安全性考量：
    /// - 建議要求使用者設定強密碼（至少 8 字元，包含大小寫、數字、符號）
    /// - 應該檢查新密碼是否與舊密碼相同（防止重複使用舊密碼）
    /// - 應該檢查新密碼是否在常見弱密碼清單中
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 使用者再次輸入的確認密碼
    /// 
    /// 用途：
    /// - 驗證兩次輸入的密碼是否一致
    /// - 防止使用者因打字錯誤而設定錯誤的密碼
    /// 
    /// 驗證邏輯：
    /// - 應該在前端先進行基本驗證（即時回饋）
    /// - 後端應該再次驗證（防止繞過前端驗證）
    /// - 如果不一致，應該拋出例外並提示使用者
    /// 
    /// 注意事項：
    /// - 目前實作中沒有看到驗證 Password 和 PasswordComfirmed 是否一致的程式碼
    /// - 建議在 HandleAsync 方法中加入驗證邏輯
    /// </summary>
    public string PasswordComfirmed { get; set; }

    /// <summary>
    /// 發送到使用者 Email 的驗證碼
    /// 
    /// 用途：
    /// - 驗證使用者是否擁有該 Email 的存取權
    /// - 防止惡意重設他人密碼
    /// 
    /// 驗證碼特性：
    /// - 由 IValidationCodeService.GenerateCode() 產生
    /// - 有效期 5 分鐘（見 ValidationCodeService）
    /// - 驗證後自動失效（一次性使用）
    /// 
    /// 安全性考量：
    /// - 驗證碼應該是 6 位數字（不易混淆）
    /// - 驗證碼應該在記憶體快取中儲存（不寫入資料庫）
    /// - 驗證失敗次數過多應該鎖定該 Email（防止暴力破解）
    /// </summary>
    public string Code { get; set; }
}

/// <summary>
/// 密碼重設確認命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ResetPasswordConfirmedCommand 命令
/// - 驗證驗證碼是否正確
/// - 更新使用者密碼
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ResetPasswordConfirmedCommand> 介面
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
public class ResetPasswordConfirmedHandler : IRequestHandler<ResetPasswordConfirmedCommand>
{
    /// <summary>
    /// 使用者倉儲介面
    /// 
    /// 用途：
    /// - 查詢 Email 是否已註冊
    /// - 存取使用者資料
    /// - 儲存密碼更新
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/UserRepository.cs）
    /// - 提供資料庫操作的抽象介面
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 驗證碼服務介面
    /// 
    /// 用途：
    /// - 驗證使用者輸入的驗證碼是否正確
    /// - 檢查驗證碼是否在有效期限內
    /// 
    /// 實作方式：
    /// - 使用 IMemoryCache 儲存驗證碼（見 Infrastructure/Services/ValidationCodeService.cs）
    /// - 驗證碼有效期 5 分鐘
    /// - 驗證後自動失效
    /// </summary>
    private readonly IValidationCodeService _validationCodeService;

    /// <summary>
    /// 密碼服務介面
    /// 
    /// 用途：
    /// - 將明碼密碼進行加鹽雜湊
    /// - 產生安全的密文儲存到資料庫
    /// 
    /// 實作方式：
    /// - 使用 PBKDF2 金鑰衍生函數（見 Infrastructure/Services/PasswordService.cs）
    /// - 每個密碼使用獨立的隨機鹽
    /// - 迭代 1000 次（可調整）
    /// </summary>
    private readonly IPasswordService _passwordService;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">使用者倉儲，用於查詢和更新使用者資料</param>
    /// <param name="validationCodeService">驗證碼服務，用於驗證驗證碼</param>
    /// <param name="passwordService">密碼服務，用於雜湊新密碼</param>
    public ResetPasswordConfirmedHandler(
        IUserRepository userRepository, 
        IValidationCodeService validationCodeService, 
        IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _validationCodeService = validationCodeService;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 處理密碼重設確認命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證驗證碼是否正確
    /// 2. 查詢使用者是否存在
    /// 3. 更新使用者密碼
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 驗證碼錯誤：拋出 Failure.BadRequest("驗證碼錯誤！")
    /// - 使用者不存在：拋出 Failure.NotFound("用戶不存在！")
    /// </summary>
    /// <param name="request">密碼重設確認命令物件，包含 Email、新密碼、確認密碼和驗證碼</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(ResetPasswordConfirmedCommand request)
    {
        // ========== 第一步：驗證驗證碼是否正確 ==========
        // 使用 IValidationCodeService.ValidateCode() 驗證
        // 如果驗證失敗，拋出例外
        if(!_validationCodeService.ValidateCode(request.Email, request.Code))
            throw Failure.BadRequest("驗證碼錯誤！");

        // ========== 第二步：查詢使用者是否存在 ==========
        // 使用 IUserRepository.GetByEmailAsync() 查詢
        // 如果找不到使用者，拋出例外
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if(user == null)
            throw Failure.NotFound("用戶不存在！");

        // ========== 第三步：更新使用者密碼 ==========
        // 使用 IPasswordService.Hash() 將明碼密碼進行加鹽雜湊
        // 將雜湊結果儲存到使用者的 PasswordHash 屬性
        user.PasswordHash = _passwordService.Hash(request.Password);
        
        // ========== 第四步：儲存變更 ==========
        // 使用 IUserRepository.SaveChangeAsync() 將變更寫入資料庫
        await _userRepository.SaveChangeAsync();
    }
}
