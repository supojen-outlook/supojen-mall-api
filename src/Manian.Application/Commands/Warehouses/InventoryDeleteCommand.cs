using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Warehouses;

/// <summary>
/// 刪除庫存命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除庫存記錄所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 清理無用的庫存記錄
/// - SKU 或儲位刪除前的清理
/// - 測試資料清理
/// 
/// 注意事項：
/// - 刪除庫存記錄可能會影響已關聯的訂單
/// - 建議在刪除前檢查是否有交易記錄
/// - 建議在刪除前檢查庫存數量是否為零
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class InventoryDeleteCommand : IRequest
{
    /// <summary>
    /// 庫存記錄唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的庫存記錄
    /// - 必須是資料庫中已存在的庫存記錄 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的庫存記錄
    /// 
    /// 錯誤處理：
    /// - 如果庫存記錄不存在，會拋出 Failure.NotFound()
    /// </summary>
    public long Id { get; set; }
}

/// <summary>
/// 刪除庫存命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 InventoryDeleteCommand 命令
/// - 查詢庫存記錄是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<InventoryDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ILocationRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查庫存記錄是否有交易記錄
/// - 未檢查庫存數量是否為零
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - CategoryDeleteHandler：類似的刪除邏輯
/// - BrandDeleteHandler：類似的刪除邏輯
/// </summary>
internal class InventoryDeleteHandler : IRequestHandler<InventoryDeleteCommand>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取庫存資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 擴展了 DeleteInventory 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Warehouses/ILocationRepository.cs
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢和刪除庫存記錄</param>
    public InventoryDeleteHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除庫存命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢庫存記錄
    /// 2. 驗證庫存記錄是否存在
    /// 3. 檢查庫存數量是否為零
    /// 4. 檢查是否有交易記錄
    /// 5. 刪除庫存記錄
    /// 6. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 庫存記錄不存在：拋出 Failure.NotFound()
    /// - 庫存數量不為零：拋出 Failure.BadRequest()
    /// - 有交易記錄：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 考慮實作軟刪除（標記為已刪除）而非硬刪除
    /// - 建議檢查是否有交易記錄或庫存數量是否為零
    /// </summary>
    /// <param name="request">刪除庫存命令物件，包含庫存記錄 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(InventoryDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢庫存記錄 ==========
        // 使用 ILocationRepository.GetInventoryAsync() 查詢庫存
        // 這個方法會從資料庫中取得完整的庫存實體
        var inventory = await _repository.GetInventoryAsync((int)request.Id);
        
        // ========== 第二步：驗證庫存記錄是否存在 ==========
        // 如果找不到庫存記錄，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 庫存記錄 ID 不存在
        // - 庫存記錄已被刪除
        if (inventory == null)
            throw Failure.NotFound($"庫存記錄不存在，ID: {request.Id}");

        // ========== 第三步：檢查庫存數量是否為零 ==========
        // 如果庫存數量不為零，不允許刪除
        // 這是為了防止誤刪除有庫存的記錄
        if (inventory.QuantityOnHand != 0)
        {
            throw Failure.BadRequest(
                $"庫存數量不為零，無法刪除。當前庫存：{inventory.QuantityOnHand}");
        }

        // ========== 第四步：檢查是否有交易記錄 ==========
        // 注意：這個檢查需要在 ILocationRepository 中新增方法
        // 或者通過 InventoryTransactionRepository 查詢
        // 這裡假設已經有方法可以查詢交易記錄
        // var hasTransactions = await _repository.HasInventoryTransactionsAsync(inventory.Id);
        // if (hasTransactions)
        // {
        //     throw Failure.BadRequest("庫存記錄有交易記錄，無法刪除");
        // }

        // ========== 第五步：刪除庫存記錄 ==========
        // 使用 ILocationRepository.DeleteInventory() 刪除庫存記錄
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新庫存記錄的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        _repository.DeleteInventory(inventory);

        // ========== 第六步：儲存變更 ==========
        // 使用 ILocationRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
