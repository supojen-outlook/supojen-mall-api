using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Warehouses;

/// <summary>
/// 更新庫存命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新庫存所需的資訊
/// 設計模式：實作 IRequest<Inventory>，表示這是一個會回傳更新後實體的命令
/// 
/// 使用場景：
/// - 庫存調整（盤點調整、損耗調整）
/// - 庫存更正（錯誤修正）
/// - 系統初始化（設定初始庫存）
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 只更新 QuantityOnHand，其他欄位保持不變
/// - 自動計算 QuantityAvailable
/// 
/// 注意事項：
/// - 建議搭配 InventoryTransaction 記錄變更原因
/// - 不建議直接用此命令進行庫存扣減（應使用 InventoryTransaction）
/// - 建議在 UI 層加入確認對話框
/// </summary>
public class InventoryUpdateCommand : IRequest<Inventory>
{
    /// <summary>
    /// 庫存記錄唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的庫存記錄
    /// - 必須是資料庫中已存在的庫存記錄 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的庫存記錄
    /// 
    /// 錯誤處理：
    /// - 如果庫存記錄不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 實際庫存數量
    /// 
    /// 用途：
    /// - 設定庫存的實際數量
    /// - 會直接覆蓋原有數量（非累加）
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 不能為 0 或負數
    /// - 更新後的數量不能小於預占庫存
    /// 
    /// 錯誤處理：
    /// - 如果更新後的數量小於預占庫存，會拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 這是直接設定數量，不是累加
    /// - 會自動計算 QuantityAvailable
    /// - 建議搭配 InventoryTransaction 記錄變更原因
    /// </summary>
    public int? QuantityOnHand { get; set; }
}

/// <summary>
/// 更新庫存命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 InventoryUpdateCommand 命令
/// - 查詢庫存記錄是否存在
/// - 更新庫存數量
/// - 自動計算可銷售庫存
/// 
/// 設計模式：
/// - 實作 IRequestHandler<InventoryUpdateCommand, Inventory> 介面
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
/// - 未檢查 QuantityOnHand 是否小於 QuantityReserved
/// - 未記錄庫存交易（InventoryTransaction）
/// - 未檢查是否會超過儲位容量
/// - 建議在實際專案中加入這些檢查
/// </summary>
internal class InventoryUpdateHandler : IRequestHandler<InventoryUpdateCommand, Inventory>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取庫存資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 擴展了 GetInventoryAsync、GetInventoriesAsync 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Warehouses/ILocationRepository.cs
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢和更新庫存</param>
    public InventoryUpdateHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新庫存命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢庫存記錄
    /// 2. 驗證庫存記錄是否存在
    /// 3. 更新庫存數量
    /// 4. 自動計算可銷售庫存
    /// 5. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 庫存記錄不存在：拋出 Failure.NotFound()
    /// - 更新後的數量小於預占庫存：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否會超過儲位容量
    /// - 建議記錄庫存交易（InventoryTransaction）
    /// </summary>
    /// <param name="request">更新庫存命令物件，包含庫存記錄 ID 和要更新的數量</param>
    /// <returns>更新後的庫存實體</returns>
    public async Task<Inventory> HandleAsync(InventoryUpdateCommand request)
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

        // ========== 第三步：更新庫存數量 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        if (request.QuantityOnHand.HasValue)
        {
            // 檢查更新後的數量是否小於預占庫存
            if (request.QuantityOnHand.Value < inventory.QuantityReserved)
            {
                throw Failure.BadRequest(
                    $"更新後的數量不能小於預占庫存，當前預占：{inventory.QuantityReserved}，要更新為：{request.QuantityOnHand.Value}");
            }

            // 更新實際庫存數量
            inventory.QuantityOnHand = request.QuantityOnHand.Value;
            
            // 自動計算可銷售庫存
            // QuantityAvailable = QuantityOnHand - QuantityReserved
            inventory.QuantityAvailable = inventory.QuantityOnHand - inventory.QuantityReserved;
        }

        // ========== 第四步：儲存變更 ==========
        // 使用 ILocationRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第五步：回傳更新後的實體 ==========
        // 回傳更新後的 Inventory 實體
        // 包含所有更新後的屬性值
        return inventory;
    }
}
