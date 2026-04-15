using Manian.Domain.Repositories.Warehouses;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Warehouses;

/// <summary>
/// 刪除儲位命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除儲位所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的儲位
/// - 清理測試資料
/// - 儲位結構重組
/// 
/// 注意事項：
/// - 刪除儲位可能會影響已關聯的庫存
/// - 建議在刪除前檢查是否有庫存使用此儲位
/// - 軟刪除可能比硬刪除更安全
/// - 如果儲位有子節點，刪除會失敗（由資料庫外鍵約束保證）
/// </summary>
public class LocationDeleteCommand : IRequest
{
    /// <summary>
    /// 儲位唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的儲位
    /// - 必須是資料庫中已存在的儲位 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的儲位
    /// 
    /// 錯誤處理：
    /// - 如果儲位不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除儲位命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 LocationDeleteCommand 命令
/// - 查詢儲位是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<LocationDeleteCommand> 介面
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
/// - 未檢查儲位是否有子節點
/// - 未檢查是否有庫存使用此儲位
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - CategoryDeleteHandler：類似的刪除邏輯
/// - BrandDeleteHandler：類似的刪除邏輯
/// </summary>
internal class LocationDeleteHandler : IRequestHandler<LocationDeleteCommand>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取儲位資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 繼承自 Repository<Location>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Warehouses/ILocationRepository.cs
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢和刪除儲位</param>
    public LocationDeleteHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除儲位命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢儲位實體
    /// 2. 驗證儲位是否存在
    /// 3. 刪除儲位
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 儲位不存在：拋出 Failure.NotFound()
    /// - 儲位有子節點：由資料庫外鍵約束拋出例外
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 考慮實作軟刪除（標記為已刪除）而非硬刪除
    /// - 建議檢查是否有子節點或庫存使用此儲位
    /// 
    /// 參考實作：
    /// - CategoryDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - BrandDeleteHandler.HandleAsync：類似的刪除邏輯
    /// </summary>
    /// <param name="request">刪除儲位命令物件，包含儲位 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(LocationDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢儲位實體 ==========
        // 使用 ILocationRepository.GetByIdAsync() 查詢儲位
        // 這個方法會從資料庫中取得完整的儲位實體
        var location = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證儲位是否存在 ==========
        // 如果找不到儲位，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 儲位 ID 不存在
        // - 儲位已被刪除（軟刪除）
        if (location == null)
            throw Failure.NotFound($"儲位不存在，ID: {request.Id}");

        // ========== 第三步：刪除儲位 ==========
        // 使用 ILocationRepository.Delete() 刪除儲位
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新儲位的狀態欄位
        // 如果儲位有子節點，資料庫外鍵約束會拋出例外
        _repository.Delete(location);

        // ========== 第四步：儲存變更 ==========
        // 使用 ILocationRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
