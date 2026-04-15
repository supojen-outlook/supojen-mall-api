using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Services;
using Manian.Domain.ValueObjects;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 新增運費規則命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增運費規則所需的資訊
/// 設計模式：實作 IRequest<ShippingRule>，表示這是一個會回傳 ShippingRule 實體的命令
/// 
/// 使用場景：
/// - 管理員建立新的運費規則
/// - 系統初始化時建立預設運費規則
/// - 運費規則結構擴展
/// 
/// 設計特點：
/// - 支援按數量和按金額兩種條件類型
/// - 使用值物件確保條件的類型安全
/// - 自動設定優先級（由系統計算）
/// 
/// 注意事項：
/// - 運費規則的優先級由系統根據現有規則自動計算
/// - 建議在新增前檢查是否已存在類似規則
/// - 運費規則的條件必須有效（由 ShippingRuleCondition 實體驗證）
/// </summary>
public class ShippingRuleAddCommand : IRequest<ShippingRule>
{
    /// <summary>
    /// 規則名稱
    /// 
    /// 用途：
    /// - 識別運費規則
    /// - 顯示在管理後台
    /// 
    /// 驗證規則：
    /// - 不能為空白字串
    /// - 長度不能超過 100 個字元
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 規則描述
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
    /// 運費規則條件
    /// 
    /// 用途：
    /// - 定義運費規則的適用條件
    /// - 支援按數量和按金額兩種條件類型
    /// 
    /// 驗證規則：
    /// - 必須為有效的 ShippingRuleCondition 實體
    /// - 條件必須通過 IsValid() 驗證
    /// 
    /// 使用範例：
    /// <code>
    /// // 按數量計算的運費條件
    /// var quantityCondition = new QuantityShippingCondition
    /// {
    ///     MinQuantity = 1,
    ///     MaxQuantity = 5
    /// };
    /// 
    /// // 按金額計算的運費條件
    /// var amountCondition = new AmountShippingCondition
    /// {
    ///     MinAmount = 100,
    ///     MaxAmount = 500
    /// };
    /// </code>
    /// </summary>
    public ShippingRuleCondition? Condition { get; set; }

    /// <summary>
    /// 運費金額
    /// 
    /// 用途：
    /// - 設定符合條件的訂單的運費
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 不能為負數
    /// </summary>
    public decimal ShippingFee { get; set; }

    /// <summary>
    /// 是否啟用
    /// 
    /// 用途：
    /// - 控制運費規則是否生效
    /// 
    /// 驗證規則：
    /// - 必須為布林值
    /// - 預設值為 true
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 新增運費規則命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ShippingRuleAddCommand 命令
/// - 建立新的 ShippingRule 實體
/// - 計算並設定優先級
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 ShippingRule 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShippingRuleAddCommand, ShippingRule> 介面
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
/// - RuleAddHandler：類似的新增邏輯
/// - CategoryAddHandler：類似的新增邏輯
/// </summary>
internal class ShippingRuleAddHandler : IRequestHandler<ShippingRuleAddCommand, ShippingRule>
{
    /// <summary>
    /// 唯一識別碼產生器服務
    /// 
    /// 用途：
    /// - 產生全域唯一的整數 ID
    /// - 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/Snowflake.cs
    /// - 註冊為單例模式 (Singleton)
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 運費規則倉儲介面
    /// 
    /// 用途：
    /// - 存取運費規則資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/ShippingRuleRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<ShippingRule>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IShippingRuleRepository.cs
    /// </summary>
    private readonly IShippingRuleRepository _shippingRuleRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生規則 ID</param>
    /// <param name="shippingRuleRepository">運費規則倉儲，用於存取資料庫</param>
    public ShippingRuleAddHandler(
        IUniqueIdentifier uniqueIdentifier,
        IShippingRuleRepository shippingRuleRepository)
    {
        _uniqueIdentifier = uniqueIdentifier;
        _shippingRuleRepository = shippingRuleRepository;
    }

    /// <summary>
    /// 處理新增運費規則命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證條件是否有效
    /// 2. 查詢現有規則以計算優先級
    /// 3. 建立新的 ShippingRule 實體
    /// 4. 將實體加入倉儲並儲存
    /// 5. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 條件無效：拋出 Failure.BadRequest("運費條件無效")
    /// - 儲存失敗：拋出 Failure.BadRequest("新增運費規則失敗")
    /// 
    /// 注意事項：
    /// - 運費規則的優先級由系統根據現有規則自動計算
    /// - 新規則的優先級會設定為現有規則的最大優先級 + 1
    /// </summary>
    /// <param name="request">新增運費規則命令物件，包含規則的所有資訊</param>
    /// <returns>儲存後的 ShippingRule 實體，包含資料庫自動生成的欄位</returns>
    public async Task<ShippingRule> HandleAsync(ShippingRuleAddCommand request)
    {
        // ========== 第一步：驗證條件是否有效 ==========
        // 如果提供了條件，驗證條件是否有效
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

        // ========== 第二步：查詢現有規則以計算優先級 ==========
        // 查詢所有現有的運費規則
        var allRules = await _shippingRuleRepository.GetAllAsync();
        
        // 計算新規則的優先級
        // 新規則的優先級會設定為現有規則的最大優先級 + 1
        // 如果沒有現有規則，優先級設為 1
        int maxPriority = allRules.Any() ? allRules.Max(r => r.Priority) : 0;
        int newPriority = maxPriority + 1;

        // ========== 第三步：建立新的 ShippingRule 實體 ==========
        var rule = new ShippingRule
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            Name = request.Name,
            Description = request.Description,
            
            // 設定條件
            Condition = request.Condition,
            
            // 設定運費金額
            ShippingFee = request.ShippingFee,
            
            // 設定是否啟用
            IsActive = request.IsActive,
            
            // 設定優先級
            Priority = newPriority,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第四步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _shippingRuleRepository.Add(rule);

        // ========== 第五步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        await _shippingRuleRepository.SaveChangeAsync();

        // ========== 第六步：回傳儲存後的實體 ==========
        // 回傳儲存後的 ShippingRule 實體
        // 包含所有屬性值，包括自動生成的 ID 和計算的優先級
        return rule;
    }
}
