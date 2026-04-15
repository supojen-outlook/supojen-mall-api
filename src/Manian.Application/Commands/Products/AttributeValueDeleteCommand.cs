using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 刪除屬性值命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除屬性值所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的屬性值
/// - 清理測試資料
/// - 屬性結構重組
/// 
/// 注意事項：
/// - 刪除屬性值可能會影響已關聯的產品
/// - 建議在刪除前檢查是否有產品使用此屬性值
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class AttributeValueDeleteCommand : IRequest
{
    /// <summary>
    /// 屬性值唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的屬性值
    /// - 必須是資料庫中已存在的屬性值 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的屬性值
    /// 
    /// 錯誤處理：
    /// - 如果屬性值不存在，會拋出 Failure.BadRequest("属性值不存在")
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除屬性值命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AttributeValueDeleteCommand 命令
/// - 查詢屬性值是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeValueDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IAttributeKeyRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查屬性值是否有關聯的產品
/// - 未檢查是否有產品使用此屬性值
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// </summary>
internal class AttributeDeleteHandler : IRequestHandler<AttributeValueDeleteCommand>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性值資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs）
    /// - 提供泛型方法 GetValueAsync、Delete 等
    /// - 繼承自 Repository<AttributeKey>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Products/IAttributeKeyRepository.cs
    /// - 擴展了 GetValueAsync、Delete 等方法
    /// </summary>
    private readonly IAttributeKeyRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">屬性鍵倉儲，用於查詢和刪除屬性值</param>
    public AttributeDeleteHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除屬性值命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢屬性值實體
    /// 2. 驗證屬性值是否存在
    /// 3. 刪除屬性值
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 屬性值不存在：拋出 Failure.BadRequest("属性值不存在")
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 考慮實作軟刪除（標記為已刪除）而非硬刪除
    /// - 建議檢查是否有產品使用此屬性值
    /// 
    /// 參考實作：
    /// - AttributeKeyDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - BrandDeleteHandler.HandleAsync：類似的刪除邏輯
    /// </summary>
    /// <param name="request">刪除屬性值命令物件，包含屬性值 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(AttributeValueDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢屬性值實體 ==========
        // 使用 IAttributeKeyRepository.GetValueAsync() 查詢屬性值
        // 這個方法會從資料庫中取得完整的屬性值實體
        var attributeValue = await _repository.GetValueAsync(request.Id);
        
        // ========== 第二步：驗證屬性值是否存在 ==========
        // 如果找不到屬性值，拋出 400 錯誤
        // 這種情況可能發生在：
        // - 屬性值 ID 不存在
        // - 屬性值已被刪除（軟刪除）
        if(attributeValue is null) 
            throw Failure.BadRequest("属性值不存在");
        
        // ========== 第三步：刪除屬性值 ==========
        // 使用 IAttributeKeyRepository.Delete() 刪除屬性值
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新屬性值的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        _repository.Delete(attributeValue);

        // ========== 第四步：儲存變更 ==========
        // 使用 IAttributeKeyRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
