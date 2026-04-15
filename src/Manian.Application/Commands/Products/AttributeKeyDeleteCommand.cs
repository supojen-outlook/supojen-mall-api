using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 刪除屬性鍵命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除屬性鍵所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的屬性鍵
/// - 清理測試資料
/// - 屬性結構重組
/// 
/// 注意事項：
/// - 刪除屬性鍵可能會影響已關聯的產品
/// - 建議在刪除前檢查是否有產品使用此屬性鍵
/// - 建議在刪除前檢查是否有類別關聯此屬性鍵
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class AttributeKeyDeleteCommand : IRequest
{
    /// <summary>
    /// 屬性鍵唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的屬性鍵
    /// - 必須是資料庫中已存在的屬性鍵 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的屬性鍵
    /// 
    /// 錯誤處理：
    /// - 如果屬性鍵不存在，會拋出 Failure.BadRequest("找不到相對應的屬性鍵")
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除屬性鍵命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AttributeKeyDeleteCommand 命令
/// - 查詢屬性鍵是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeKeyDeleteCommand> 介面
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
/// - 未檢查屬性鍵是否有關聯的屬性值
/// - 未檢查是否有類別使用此屬性鍵
/// - 未檢查是否有產品使用此屬性鍵
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - BrandDeleteCommandHandler：類似的刪除邏輯
/// - CategoryDeleteCommandHandler：類似的刪除邏輯
/// </summary>
internal class AttributeKeyDeleteHandler : IRequestHandler<AttributeKeyDeleteCommand>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性鍵資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 繼承自 Repository<AttributeKey>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Products/IAttributeKeyRepository.cs
    /// - 擴展了 AddValues、GetValuesAsync、GetCategoryAttributesAsync 等方法
    /// </summary>
    private readonly IAttributeKeyRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">屬性鍵倉儲，用於查詢和刪除屬性鍵</param>
    public AttributeKeyDeleteHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除屬性鍵命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢屬性鍵實體
    /// 2. 驗證屬性鍵是否存在
    /// 3. 刪除屬性鍵
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 屬性鍵不存在：拋出 Failure.BadRequest("找不到相對應的屬性鍵")
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 考慮實作軟刪除（標記為已刪除）而非硬刪除
    /// - 建議檢查是否有屬性值、類別或產品使用此屬性鍵
    /// 
    /// 參考實作：
    /// - BrandDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - CategoryDeleteHandler.HandleAsync：類似的刪除邏輯
    /// </summary>
    /// <param name="request">刪除屬性鍵命令物件，包含屬性鍵 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(AttributeKeyDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢屬性鍵實體 ==========
        // 使用 IAttributeKeyRepository.GetByIdAsync() 查詢屬性鍵
        // 這個方法會從資料庫中取得完整的屬性鍵實體
        var attributeKey = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證屬性鍵是否存在 ==========
        // 如果找不到屬性鍵，拋出 400 錯誤
        // 這種情況可能發生在：
        // - 屬性鍵 ID 不存在
        // - 屬性鍵已被刪除（軟刪除）
        if (attributeKey == null)
            throw Failure.BadRequest(title: "找不到相對應的屬性鍵");
        
        // ========== 第三步：刪除屬性鍵 ==========
        // 使用 IAttributeKeyRepository.DeleteAsync() 刪除屬性鍵
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新屬性鍵的狀態欄位
        _repository.Delete(attributeKey);

        // ========== 第四步：儲存變更 ==========
        // 使用 IAttributeKeyRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
