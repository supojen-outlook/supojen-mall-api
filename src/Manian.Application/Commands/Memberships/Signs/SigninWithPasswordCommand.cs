using Manian.Application.Services;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Microsoft.EntityFrameworkCore;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships.Signs;

/// <summary>
/// 使用密碼登入的命令類別 (CQRS 模式中的 Command)
/// 
/// 用途：封裝使用者使用電子郵件和密碼登入所需的資訊
/// 
/// 設計模式：
/// - 實作 IRequest<User>，表示這是一個會回傳 User 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SigninWithPasswordCommandHandler 配合使用，完成登入驗證
/// 
/// 安全性考量：
/// - 密碼以明碼傳輸（應該在傳輸層使用 HTTPS 加密）
/// - 不儲存明碼密碼，只進行驗證
/// - 驗證失敗不應洩漏具體原因（帳號不存在或密碼錯誤）
/// </summary>
public class SigninWithPasswordCommand : IRequest<User>
{
    /// <summary>
    /// 使用者電子郵件地址
    /// 
    /// 用途：
    /// - 作為使用者的識別，用於查找帳號
    /// - 必須是已註冊的 Email
    /// 
    /// 驗證規則：
    /// - 必須符合 Email 格式規範
    /// - 不區分大小寫（查詢時轉為小寫）
    /// - 必須是已驗證的 Email (EmailVerified = true)
    /// 
    /// 安全性考量：
    /// - 即使用戶不存在，也不應明確告知（防止列舉攻擊）
    /// - 但目前實作會拋出「找不到指定的帳號」的錯誤
    /// - 建議改為：不論 Email 是否存在，都回傳「帳號或密碼錯誤」
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 使用者輸入的密碼
    /// 
    /// 用途：
    /// - 與資料庫中儲存的密碼雜湊值進行比對
    /// - 驗證使用者身份
    /// 
    /// 密碼規則：
    /// - 應該由前端進行基本驗證（長度、複雜度）
    /// - 後端會使用 IPasswordService.Validate() 進行驗證
    /// - 絕不儲存明碼密碼
    /// 
    /// 安全性考量：
    /// - 建議要求使用者設定強密碼（至少 8 字元，包含大小寫、數字、符號）
    /// - 應該檢查密碼是否在常見弱密碼清單中
    /// - 驗證失敗應該有延遲（防止暴力破解）
    /// </summary>
    public string Password { get; set; }
}

/// <summary>
/// 密碼登入命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SigninWithPasswordCommand 命令
/// - 驗證使用者帳號和密碼
/// - 回傳驗證通過的使用者實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SigninWithPasswordCommand, User> 介面
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
public class SigninWithPasswordCommandHandler : IRequestHandler<SigninWithPasswordCommand, User>
{
    /// <summary>
    /// 使用者倉儲介面
    /// 
    /// 用途：
    /// - 查詢 Email 是否已註冊
    /// - 存取使用者資料
    /// - 包含使用者的角色資訊
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/UserRepository.cs）
    /// - 提供資料庫操作的抽象介面
    /// </summary>
    private readonly IUserRepository _repository;

    /// <summary>
    /// 密碼服務介面
    /// 
    /// 用途：
    /// - 驗證使用者輸入的密碼是否正確
    /// - 比對明碼密碼與資料庫中的雜湊值
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
    /// <param name="repository">使用者倉儲，用於查詢使用者資料</param>
    /// <param name="passwordService">密碼服務，用於驗證密碼</param>
    public SigninWithPasswordCommandHandler(
        IUserRepository repository, 
        IPasswordService passwordService)
    {
        _repository = repository;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 處理密碼登入命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 Email 查詢使用者（包含角色資訊）
    /// 2. 驗證使用者是否存在
    /// 3. 驗證密碼是否正確
    /// 4. 回傳驗證通過的使用者實體
    /// 
    /// 錯誤處理：
    /// - 使用者不存在：拋出 Failure.NotFound("找不到指定的帳號")
    /// - 密碼錯誤：拋出 Failure.BadRequest("密碼錯誤")
    /// </summary>
    /// <param name="request">密碼登入命令物件，包含 Email 和 Password</param>
    /// <returns>驗證通過的使用者實體（包含角色資訊）</returns>
    public async Task<User> HandleAsync(SigninWithPasswordCommand request)
    {
        // ========== 第一步：根據 Email 查詢使用者 ==========
        // 使用 IUserRepository.GetAsync() 查詢使用者
        // 使用 Include 預先載入使用者的角色資訊（避免 N+1 查詢問題）
        // 使用 Where 篩選出符合 Email 的使用者
        var user = await _repository.GetAsync(query =>
        {
            // 預先載入使用者的角色集合
            // 這樣可以在一次查詢中取得使用者和其角色，避免後續額外查詢
            query = query.Include(x => x.Roles);

            // 根據 Email 篩選使用者
            // 注意：資料庫中的 Email 應該儲存為小寫，以確保不區分大小寫
            query = query.Where(x => x.Email == request.Email);
            return query;
        });
        
        // ========== 第二步：驗證使用者是否存在 ==========
        // 如果找不到使用者，拋出 404 錯誤
        // 這種情況可能發生在：
        // - Email 尚未註冊
        // - Email 已被註冊但未驗證 (EmailVerified = false)
        // - Email 已被刪除（軟刪除）
        if(user == null)
            throw Failure.NotFound(title:"找不到指定的帳號");
        
        // ========== 第三步：驗證密碼是否正確 ==========
        // 使用 IPasswordService.Validate() 驗證密碼
        // 這個方法會：
        // 1. 從資料庫的 PasswordHash 中取出鹽
        // 2. 用相同的鹽和演算法，對輸入的密碼重新計算雜湊
        // 3. 比對重新計算的結果是否與資料庫中的雜湊值相同
        // 
        // 使用 ?? "" 處理 PasswordHash 為 null 的情況
        // 雖然正常情況下不應該為 null，但為了防禦性程式設計，加上這個處理
        if(!_passwordService.Validate(request.Password, user.PasswordHash ?? ""))
            throw Failure.BadRequest(title:"密碼錯誤");

        // ========== 第四步：回傳驗證通過的使用者 ==========
        // 回傳使用者實體，包含：
        // - 基本資料（Id, DisplayName, Email 等）
        // - 角色資訊（Roles 集合）
        // - 點數資訊（PointAccount）
        // 
        // 注意：這個 User 實體會被 EF Core 追蹤
        // 如果後續需要修改使用者資料，可以直接修改並呼叫 SaveChangesAsync()
        return user;
    }
}
