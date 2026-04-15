using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.ValueObjects;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 更新運費規則命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新運費規則所需的資訊
/// 設計模式：實作 IRequest<ShippingRule>，表示這是一個會回傳更新後實體的命令
/// 
/// 使用場景：
/// - 管理員修改運費規則
/// - 調整運費金額
/// - 修改運費條件
/// - 啟用或停用運費規則
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 只更新提供的欄位，保持未提供欄位的原值不變
/// - 不允許更新優先級（應使用專用的 PriorityUpdateCommand）
/// 
/// 注意事項：
/// - 更新運費規則可能會影響正在進行的訂單
/// - 建議在更新前檢查是否有訂單使用此規則
/// - 運費規則的條件必須有效（由 ShippingRuleCondition 實體驗證）
/// </summary>
public class ShippingRuleUpdateCommand : IRequest<ShippingRule>
{
    /// <summary>
    /// 運費規則唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的運費規則
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

    /// <summary>
    /// 規則名稱（可選）
    /// 
    /// 用途：
    /// - 識別運費規則
    /// - 顯示在管理後台
    /// 
    /// 驗證規則：
    /// - 不能為空白字串
    /// - 長度不能超過 100 個字元
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 規則描述（可選）
    /// 
    /// 用途：
    /// - 說明運費規則的適用條件
    /// - 顯示在管理後台
    /// 
    /// 驗證規則：
    /// - 可以為 null
    /// - 長度不能超過 500 個字元
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 運費規則條件（可選）
    /// 
    /// 用途：
    /// - 定義運費規則的適用條件
    /// - 支援按數量和按金額兩種條件類型
    /// 
    /// 驗證規則：
    /// - 必須為有效的 ShippingRuleCondition 實體
    /// - 條件必須通過 IsValid() 驗證
    /// </summary>
    public ShippingRuleCondition? Condition { get; set; }

    /// <summary>
    /// 運費金額（可選）
    /// 
    /// 用途：
    /// - 設定符合條件的訂單的運費
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 不能為負數
    /// </summary>
    public decimal? ShippingFee { get; set; }

    /// <summary>
    /// 是否啟用（可選）
    /// 
    /// 用途：
    /// - 控制運費規則是否生效
    /// 
    /// 驗證規則：
    /// - 必須為布林值
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// 更新運費規則命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ShippingRuleUpdateCommand 命令
/// - 查詢運費規則是否存在
/// - 更新運費規則
/// - 回傳更新後的實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShippingRuleUpdateCommand, ShippingRule> 介面
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
/// 參考實作：
/// - RuleUpdateHandler：類似的更新邏輯
/// - CategoryUpdateHandler：類似的更新邏輯
/// </summary>
internal class ShippingRuleUpdateHandler : IRequestHandler<ShippingRuleUpdateCommand, ShippingRule>
{
    /// <summary>
    /// 運費規則倉儲介面
    /// 
    /// 用途：
    /// - 存取運費規則資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/ShippingRuleRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、UpdateAsync 等
    /// - 繼承自 Repository<ShippingRule>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IShippingRuleRepository.cs
    /// </summary>
    private readonly IShippingRuleRepository _shippingRuleRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="shippingRuleRepository">運費規則倉儲，用於查詢和更新運費規則</param>
    public ShippingRuleUpdateHandler(IShippingRuleRepository shippingRuleRepository)
    {
        _shippingRuleRepository = shippingRuleRepository;
    }

    /// <summary>
    /// 處理更新運費規則命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢運費規則
    /// 2. 驗證運費規則是否存在
    /// 3. 驗證條件是否有效（如果提供了新條件）
    /// 4. 更新運費規則
    /// 5. 儲存變更
    /// 6. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - 運費規則不存在：拋出 Failure.NotFound()
    /// - 條件無效：拋出 Failure.BadRequest("運費條件無效")
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 更新運費規則可能會影響正在進行的訂單
    /// - 建議檢查是否有訂單使用此規則
    /// </summary>
    /// <param name="request">更新運費規則命令物件，包含規則 ID 和要更新的欄位</param>
    /// <returns>更新後的運費規則實體</returns>
    public async Task<ShippingRule> HandleAsync(ShippingRuleUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢運費規則 ==========
        // 使用 IShippingRuleRepository.GetByIdAsync() 查詢運費規則
        // 這個方法會從資料庫中取得完整的運費規則實體
        var rule = await _shippingRuleRepository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證運費規則是否存在 ==========
        // 如果找不到運費規則，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 運費規則 ID 不存在
        // - 運費規則已被刪除
        if (rule == null)
            throw Failure.NotFound($"運費規則不存在，ID: {request.Id}");

        // ========== 第三步：驗證條件是否有效 ==========
        // 如果提供了新條件，驗證條件是否有效
        if (request.Condition != null)
        {
            // 根據條件類型調用相應的驗證方法
            bool isValid = request.Condition switch
            {
                QuantityShippingCondition quantityCondition => quantityCondition.IsValid(),
                AmountShippingCondition amountCondition => amountCondition.IsValid(),
                _ => false
            };

            // 如果條件無效，拋出錯誤
            if (!isValid)
                throw Failure.BadRequest("運費條件無效");
        }

        // ========== 第四步：更新運費規則 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        if (request.Name != null)
            rule.Name = request.Name;
        
        if (request.Description != null)
            rule.Description = request.Description;
        
        if (request.Condition != null)
            rule.Condition = request.Condition;
        
        if (request.ShippingFee.HasValue)
            rule.ShippingFee = request.ShippingFee.Value;
        
        if (request.IsActive.HasValue)
            rule.IsActive = request.IsActive.Value;
        
        // ========== 第五步：儲存變更 ==========
        // 使用 IShippingRuleRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _shippingRuleRepository.SaveChangeAsync();

        // ========== 第六步：回傳更新後的實體 ==========
        // 回傳更新後的 ShippingRule 實體
        // 包含所有更新後的屬性值
        return rule;
    }
}
