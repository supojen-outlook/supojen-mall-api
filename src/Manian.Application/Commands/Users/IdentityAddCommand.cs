using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Users;

/// <summary>
/// 新增身份認證資訊命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增身份認證資訊所需的資訊
/// 設計模式：實作 IRequest<Identity>，表示這是一個會回傳新增實體的命令
/// 
/// 使用場景：
/// - 用戶使用第三方登入（Google、Line、Microsoft、Facebook）
/// - 用戶綁定新的登入方式
/// - 系統遷移用戶資料
/// 
/// 設計特點：
/// - 回傳型別為 Identity，讓呼叫者可以取得新增後的實體（包含自動生成的 ID）
/// - 與 IdentityDeleteCommand 不同，後者不回傳資料（IRequest）
/// 
/// 注意事項：
/// - (UserId + Provider + ProviderUid) 必須唯一（由資料庫唯一約束保證）
/// - Provider 必須是有效的認證廠商（google、line、microsoft、facebook）
/// </summary>
public class IdentityAddCommand : IRequest<Identity>
{
    /// <summary>
    /// 用戶 ID
    /// 
    /// 用途：
    /// - 識別要新增身份認證的目標用戶
    /// - 必須是資料庫中已存在的用戶 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的用戶
    /// 
    /// 範例：
    /// - UserId = 1：為 ID 為 1 的用戶新增身份認證
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 認證廠商
    /// 
    /// 用途：
    /// - 識別第三方登入的提供者
    /// - 用於決定使用哪個 OAuth 流程
    /// 
    /// 可選值：
    /// - google：Google 登入
    /// - line：Line 登入
    /// - microsoft：Microsoft 登入
    /// - facebook：Facebook 登入
    /// 
    /// 驗證規則：
    /// - 必填
    /// - 必須是有效的認證廠商
    /// - 不區分大小寫
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// 認證廠商的唯一識別碼
    /// 
    /// 用途：
    /// - 識別第三方平台上的用戶
    /// - 用於比對用戶身份
    /// 
    /// 範例：
    /// - Google：用戶的 Google ID
    /// - Line：用戶的 Line User ID
    /// - Microsoft：用戶的 Microsoft ID
    /// - Facebook：用戶的 Facebook ID
    /// 
    /// 驗證規則：
    /// - 必填
    /// - (UserId + Provider + ProviderUid) 必須唯一
    /// </summary>
    public string ProviderUid { get; set; }
}

/// <summary>
/// 新增身份認證資訊命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 IdentityAddCommand 命令
/// - 驗證用戶是否存在
/// - 建立新的 Identity 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Identity 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<IdentityAddCommand, Identity> 介面
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
/// 參考實作：
/// - AttributeValueAddHandler：類似的新增邏輯
/// - BrandAddHandler：類似的新增邏輯
/// </summary>
public class IdentityAddHandler : IRequestHandler<IdentityAddCommand, Identity>
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
    /// - 擴展了 AddIdentity、GetIdentitiesAsync 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於新增身份認證資訊</param>
    public IdentityAddHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 處理新增身份認證資訊命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證用戶是否存在
    /// 2. 建立新的 Identity 實體
    /// 3. 設定實體屬性
    /// 4. 將實體加入倉儲
    /// 5. 儲存變更到資料庫
    /// 6. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 用戶不存在：拋出 Failure.NotFound()
    /// - 身份認證資訊重複：由資料庫唯一約束處理
    /// 
    /// 注意事項：
    /// - 新增後的實體會包含自動生成的 ID
    /// - 建議在 UI 層顯示新增成功的訊息
    /// 
    /// 參考實作：
    /// - AttributeValueAddHandler.HandleAsync：類似的新增邏輯
    /// - BrandAddHandler.HandleAsync：類似的新增邏輯
    /// </summary>
    /// <param name="request">新增身份認證資訊命令物件，包含用戶 ID 和身份認證資訊</param>
    /// <returns>儲存後的 Identity 實體，包含自動生成的 ID</returns>
    public async Task<Identity> HandleAsync(IdentityAddCommand request)
    {
        // ========== 第一步：驗證用戶是否存在 ==========
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw Failure.NotFound($"用戶不存在，ID: {request.UserId}");

        // ========== 第二步：建立新的 Identity 實體 ==========
        var identity = new Identity
        {
            UserId = request.UserId,
            Provider = request.Provider.ToLower(), // 統一轉為小寫，避免大小寫不一致
            ProviderUid = request.ProviderUid
        };

        // ========== 第三步：將實體加入倉儲 ==========
        // 使用 IUserRepository.AddIdentity() 新增身份認證資訊
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _userRepository.AddIdentity(identity);

        // ========== 第四步：儲存變更到資料庫 ==========
        // 使用 IUserRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會執行 INSERT SQL 語句，並自動生成 ID
        await _userRepository.SaveChangeAsync();

        // ========== 第五步：回傳儲存後的實體 ==========
        return identity;
    }
}
