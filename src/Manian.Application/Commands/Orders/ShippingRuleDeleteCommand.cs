using Manian.Domain.Repositories.Orders;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 刪除運費規則命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除運費規則所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的運費規則
/// - 清理測試資料
/// - 運費規則結構重組
/// 
/// 注意事項：
/// - 刪除運費規則可能會影響已關聯的訂單
/// - 建議在刪除前檢查是否有訂單使用此規則
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class ShippingRuleDeleteCommand : IRequest
{
    /// <summary>
    /// 運費規則唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的運費規則
    /// - 必須是資料庫中已存在的運費規則 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的運費規則
    /// 
    /// 錯誤處理：
    /// - 如果運費規則不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除運費規則命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ShippingRuleDeleteCommand 命令
/// - 查詢運費規則是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShippingRuleDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IShippingRuleRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查運費規則是否有關聯的訂單
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - RuleDeleteHandler：類似的刪除邏輯
/// - CategoryDeleteHandler：類似的刪除邏輯
/// </summary>
internal class ShippingRuleDeleteHandler : IRequestHandler<ShippingRuleDeleteCommand>
{
    /// <summary>
    /// 運費規則倉儲介面
    /// 
    /// 用途：
    /// - 存取運費規則資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/ShippingRuleRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 繼承自 Repository<ShippingRule>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IShippingRuleRepository.cs
    /// </summary>
    private readonly IShippingRuleRepository _shippingRuleRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="shippingRuleRepository">運費規則倉儲，用於查詢和刪除運費規則</param>
    public ShippingRuleDeleteHandler(IShippingRuleRepository shippingRuleRepository)
    {
        _shippingRuleRepository = shippingRuleRepository;
    }

    /// <summary>
    /// 處理刪除運費規則命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢運費規則
    /// 2. 驗證運費規則是否存在
    /// 3. 刪除運費規則
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 運費規則不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 刪除運費規則可能會影響已關聯的訂單
    /// - 建議檢查是否有訂單使用此規則
    /// </summary>
    /// <param name="request">刪除運費規則命令物件，包含運費規則 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(ShippingRuleDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢運費規則 ==========
        // 使用 IShippingRuleRepository.GetByIdAsync() 查詢運費規則
        // 這個方法會從資料庫中取得完整的運費規則實體
        var rule = await _shippingRuleRepository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證運費規則是否存在 ==========
        // 如果找不到運費規則，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 運費規則 ID 不存在
        // - 運費規則已被刪除（軟刪除）
        if (rule == null)
            throw Failure.NotFound($"運費規則不存在，ID: {request.Id}");

        // ========== 第三步：刪除運費規則 ==========
        // 使用 IShippingRuleRepository.Delete() 刪除運費規則
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新運費規則的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        // 根據資料庫約束，如果運費規則有關聯的訂單，刪除會失敗
        _shippingRuleRepository.Delete(rule);

        // ========== 第四步：儲存變更 ==========
        // 使用 IShippingRuleRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _shippingRuleRepository.SaveChangeAsync();
    }
}
