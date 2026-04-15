using Manian.Domain.Repositories.Promotions;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 刪除促銷範圍命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除促銷範圍所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的促銷範圍
/// - 清理測試資料
/// - 促銷範圍結構重組
/// 
/// 注意事項：
/// - 刪除範圍可能會影響已關聯的訂單
/// - 建議在刪除前檢查是否有訂單使用此範圍
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class ScopeDeleteCommand : IRequest
{
    /// <summary>
    /// 範圍唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的範圍
    /// - 必須是資料庫中已存在的範圍 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的範圍
    /// 
    /// 錯誤處理：
    /// - 如果範圍不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int ScopeId { get; set; }
}

/// <summary>
/// 刪除促銷範圍命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ScopeDeleteCommand 命令
/// - 查詢範圍是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ScopeDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查範圍是否有關聯的訂單
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - RuleDeleteHandler：類似的刪除邏輯
/// - BrandDeleteHandler：類似的刪除邏輯
/// </summary>
internal class ScopeDeleteHandler : IRequestHandler<ScopeDeleteCommand>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷範圍資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 擴展了 GetScopeAsync、DeleteScope 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢和刪除範圍</param>
    public ScopeDeleteHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除促銷範圍命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢範圍實體
    /// 2. 驗證範圍是否存在
    /// 3. 刪除範圍
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 範圍不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 刪除範圍可能會影響已關聯的訂單
    /// - 建議檢查是否有訂單使用此範圍
    /// 
    /// 參考實作：
    /// - RuleDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - BrandDeleteHandler.HandleAsync：類似的刪除邏輯
    /// </summary>
    /// <param name="request">刪除促銷範圍命令物件，包含範圍 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(ScopeDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢範圍實體 ==========
        // 使用 IPromotionRepository.GetScopeAsync() 查詢範圍
        // 這個方法會從資料庫中取得完整的範圍實體
        var scope = await _repository.GetScopeAsync(request.ScopeId);
        
        // ========== 第二步：驗證範圍是否存在 ==========
        // 如果找不到範圍，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 範圍 ID 不存在
        // - 範圍已被刪除（軟刪除）
        if (scope == null)
            throw Failure.NotFound($"範圍不存在，ID: {request.ScopeId}");

        // ========== 第三步：刪除範圍 ==========
        // 使用 IPromotionRepository.DeleteScope() 刪除範圍
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新範圍的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        // 根據資料庫約束，如果範圍有關聯的訂單，刪除會失敗
        _repository.DeleteScope(scope);

        // ========== 第四步：儲存變更 ==========
        // 使用 IPromotionRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
