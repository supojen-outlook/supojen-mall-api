using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Warehouses;

/// <summary>
/// 新增庫存命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增庫存所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Inventory>，表示這是一個會回傳 Inventory 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 InventoryAddHandler 配合使用，完成新增庫存的業務邏輯
/// 
/// 使用場景：
/// - 入庫作業（採購入庫、退貨入庫）
/// - 庫存調整（盤點調整、損耗調整）
/// - 庫存初始化（新 SKU 建立初始庫存）
/// 
/// 設計特點：
/// - 支援庫存累加（同一 SKU 同一儲位多次新增）
/// - 包含儲位容量檢查機制
/// - 自動計算可銷售庫存（QuantityAvailable）
/// 
/// 注意事項：
/// - 不直接使用此命令進行庫存扣減（應使用 InventoryTransaction）
/// - 庫存變更應有完整記錄（InventoryTransaction）
/// - 建議在 UI 層加入確認對話框
/// </summary>
public class InventoryAddCommand : IRequest<Inventory>
{
    /// <summary>
    /// SKU 唯一識別碼
    /// 
    /// 用途：
    /// - 識別要新增庫存的商品規格
    /// - 與 LocationId 組合唯一識別一筆庫存記錄
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的 SKU
    /// 
    /// 錯誤處理：
    /// - 如果 SKU 不存在，會拋出 Failure.NotFound()
    /// 
    /// 設計考量：
    /// - 同一 SKU 可以放在多個儲位
    /// - 同一儲位可以存放多個 SKU
    /// - (SkuId + LocationId) 組合必須唯一
    /// </summary>
    public int SkuId { get; set; }

    /// <summary>
    /// 儲位唯一識別碼
    /// 
    /// 用途：
    /// - 識別要存放庫存的儲位
    /// - 與 SkuId 組合唯一識別一筆庫存記錄
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的儲位
    /// 
    /// 錯誤處理：
    /// - 如果儲位不存在，會拋出 Failure.NotFound()
    /// 
    /// 設計考量：
    /// - 儲位有容量限制（Capacity 屬性）
    /// - 儲位有計量單位（UnitOfMeasureId 屬性）
    /// - 儲位有狀態（Status 屬性：active/inactive/maintenance）
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// 要新增的庫存數量
    /// 
    /// 用途：
    /// - 指定要新增到儲位的庫存數量
    /// - 會累加到現有庫存中（如果已存在）
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 不能為 0 或負數
    /// - 新增後的總量不能超過儲位容量
    /// 
    /// 錯誤處理：
    /// - 如果新增後會超過儲位容量，會拋出 Failure.BadRequest()
    /// 
    /// 設計考量：
    /// - 使用儲位的計量單位（UnitOfMeasureId）
    /// - 與 Capacity 比較時使用相同的計量單位
    /// - 不區分入庫類型（採購/退貨/調整）
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// 新增庫存命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 InventoryAddCommand 命令
/// - 驗證儲位和 SKU 是否存在
/// - 檢查儲位容量是否足夠
/// - 新增或更新庫存記錄
/// - 回傳處理後的庫存實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<InventoryAddCommand, Inventory> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ILocationRepository 和 IUniqueIdentifier
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查 SKU 是否存在
/// - 未檢查儲位狀態是否為 active
/// - 未記錄庫存交易（InventoryTransaction）
/// - 建議在實際專案中加入這些檢查
/// </summary>
internal class InventoryAddHandler : IRequestHandler<InventoryAddCommand, Inventory>
{
    /// <summary>
    /// 唯一識別碼產生器
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;
    
    /// <summary>
    /// 儲位資料庫存取介面
    /// </summary>
    private readonly ILocationRepository _locationRepository;

    /// <summary>
    /// 建構子
    /// </summary>
    public InventoryAddHandler(
        IUniqueIdentifier uniqueIdentifier, 
        ILocationRepository locationRepository)
    {
        _uniqueIdentifier = uniqueIdentifier;
        _locationRepository = locationRepository;
    }

    /// <summary>
    /// 處理新增庫存命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 查詢儲位實體並驗證存在性
    /// 2. 檢查儲位容量是否足夠
    /// 3. 檢查是否已存在該 SKU 的庫存記錄
    /// 4. 新增或更新庫存記錄
    /// 5. 儲存變更到資料庫
    /// 6. 回傳處理後的庫存實體
    /// 
    /// 錯誤處理：
    /// - 儲位不存在：拋出 Failure.NotFound()
    /// - 儲位容量不足：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 庫存變更應有完整記錄（InventoryTransaction）
    /// - 建議檢查 SKU 是否存在
    /// - 建議檢查儲位狀態是否為 active
    /// </summary>
    /// <param name="request">新增庫存命令物件，包含 SkuId、LocationId 和 Quantity</param>
    /// <returns>處理後的庫存實體（包含更新後的數量）</returns>
    public async Task<Inventory> HandleAsync(InventoryAddCommand request)
    {
        // ========== 第一步：查詢儲位實體 ==========
        // 使用 ILocationRepository.GetByIdAsync() 查詢儲位
        // 這個方法會從資料庫中取得完整的儲位實體
        var location = await _locationRepository.GetByIdAsync(request.LocationId);

        // 驗證儲位是否存在
        // 如果找不到儲位，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 儲位 ID 不存在
        // - 儲位已被刪除（軟刪除）
        if (location == null)
            throw Failure.NotFound($"儲位不存在，ID: {request.LocationId}");

        // ========== 第二步：檢查儲位容量 ==========
        // 查詢該儲位當前所有庫存
        var inventories = await _locationRepository.GetInventoriesByLocationIdAsync(request.LocationId);

        // 計算當前總庫存量（QuantityOnHand 總和）
        var currentQuantity = inventories.Sum(i => i.QuantityOnHand);

        // 檢查新增後是否會超過容量
        // Capacity 是儲位的最大容量屬性（見 Location 實體）
        // 如果新增後的總量超過容量，拋出 400 錯誤
        if (currentQuantity + request.Quantity > location.MaxQuantity)
        {
            throw Failure.BadRequest(
                $"儲位容量不足，當前：{currentQuantity}，要新增：{request.Quantity}，容量：{location.MaxQuantity}");
        }

        // ========== 第三步：檢查是否已存在該 SKU 的庫存記錄 ==========
        // 從查詢到的庫存集合中找出符合 SkuId 的記錄
        var existingInventory = inventories.FirstOrDefault(i => i.SkuId == request.SkuId);
        Inventory? newInventory = null;

        if (existingInventory != null)
        {
            // 如果已存在，更新庫存量
            // 累加 QuantityOnHand
            existingInventory.QuantityOnHand += request.Quantity;
            
            // 重新計算可銷售庫存
            // QuantityAvailable = QuantityOnHand - QuantityReserved
            existingInventory.QuantityAvailable = existingInventory.QuantityOnHand - existingInventory.QuantityReserved;
        }
        else
        {
            // 如果不存在，新增庫存記錄
            newInventory = new Inventory
            {
                // 使用雪花演算法產生全域唯一 ID
                Id = _uniqueIdentifier.NextInt(),
                
                // 設定 SKU 和儲位關聯
                SkuId = request.SkuId,
                LocationId = request.LocationId,
                
                // 設定庫存數量
                QuantityOnHand = request.Quantity,
                QuantityReserved = 0,  // 初始預占庫存為 0
                QuantityAvailable = request.Quantity,  // 初始可銷售庫存等於總庫存
                
                // 設定狀態
                Status = "active",
                IsAvailable = true,
                
                // 記錄建立時間
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            // 將新庫存記錄加入倉儲追蹤
            _locationRepository.AddInventory(newInventory);
        }

        // ========== 第四步：儲存變更 ==========
        // 使用 ILocationRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        // 包括：
        // - 新增的庫存記錄（如果不存在）
        // - 更新的庫存記錄（如果已存在）
        await _locationRepository.SaveChangeAsync();

        // ========== 第五步：回傳結果 ==========
        // 如果存在，回傳更新後的庫存實體
        // 如果不存在，回傳新增的庫存實體
        if(existingInventory != null)
        {
            return existingInventory;
        }
        else
        {
            return newInventory ?? throw Failure.BadRequest("庫存新增失敗");
        }
    }
}
