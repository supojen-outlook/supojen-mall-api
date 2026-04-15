using Manian.Domain.Repositories.Memberships;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Users;

/// <summary>
/// 刪除身份認證資訊命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除身份認證資訊所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 用戶解除綁定第三方登入
/// - 用戶切換登入方式
/// - 系統清理過期的身份認證資訊
/// 
/// 注意事項：
/// - 如果用戶只有一種登入方式，不應刪除
/// - 建議在刪除前檢查用戶是否還有其他登入方式
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class IdentityDeleteCommand : IRequest
{
    /// <summary>
    /// 身份認證資訊唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的身份認證資訊
    /// - 必須是資料庫中已存在的身份認證資訊 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的身份認證資訊
    /// 
    /// 錯誤處理：
    /// - 如果身份認證資訊不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除身份認證資訊命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 IdentityDeleteCommand 命令
/// - 查詢身份認證資訊是否存在
/// - 檢查用戶是否還有其他登入方式
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<IdentityDeleteCommand> 介面
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
/// - AttributeValueDeleteHandler：類似的刪除邏輯
/// - BrandDeleteHandler：類似的刪除邏輯
/// </summary>
public class IdentityDeleteHandler : IRequestHandler<IdentityDeleteCommand>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 擴展了 AddIdentity、GetIdentitiesAsync、DeleteIdentity 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於查詢和刪除身份認證資訊</param>
    public IdentityDeleteHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 處理刪除身份認證資訊命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢身份認證資訊
    /// 2. 驗證身份認證資訊是否存在
    /// 3. 查詢用戶的所有身份認證資訊
    /// 4. 檢查用戶是否還有其他登入方式
    /// 5. 刪除身份認證資訊
    /// 6. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 身份認證資訊不存在：拋出 Failure.NotFound()
    /// - 用戶只有一種登入方式：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 如果用戶只有一種登入方式，不應刪除
    /// - 建議在刪除前檢查用戶是否還有其他登入方式
    /// 
    /// 參考實作：
    /// - AttributeValueDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - BrandDeleteHandler.HandleAsync：類似的刪除邏輯
    /// </summary>
    /// <param name="request">刪除身份認證資訊命令物件，包含身份認證資訊 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(IdentityDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢身份認證資訊 ==========
        // 使用 IUserRepository.GetIdentityAsync() 查詢身份認證資訊
        // 這個方法會從資料庫中取得完整的身份認證資訊實體
        var identity = await _userRepository.GetIdentityAsync(request.Id);
        
        // ========== 第二步：驗證身份認證資訊是否存在 ==========
        // 如果找不到身份認證資訊，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 身份認證資訊 ID 不存在
        // - 身份認證資訊已被刪除（軟刪除）
        if (identity == null)
            throw Failure.NotFound($"身份認證資訊不存在，ID: {request.Id}");

        // ========== 第三步：查詢用戶的所有身份認證資訊 ==========
        // 使用 IUserRepository.GetIdentitiesAsync() 查詢用戶的所有身份認證資訊
        var allIdentities = await _userRepository.GetIdentitiesAsync(
            identity.UserId,
            query => query // 不需要額外的篩選條件
        );

        // ========== 第四步：檢查用戶是否還有其他登入方式 ==========
        // 如果用戶只有一種登入方式，不允許刪除
        if (allIdentities.Count() == 1)
        {
            throw Failure.BadRequest("用戶只有一種登入方式，無法刪除");
        }

        // ========== 第五步：刪除身份認證資訊 ==========
        // 使用 IUserRepository.DeleteIdentity() 刪除身份認證資訊
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新身份認證資訊的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        _userRepository.DeleteIdentity(identity);

        // ========== 第六步：儲存變更 ==========
        // 使用 IUserRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _userRepository.SaveChangeAsync();
    }
}
