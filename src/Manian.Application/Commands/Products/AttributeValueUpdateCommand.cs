using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 更新屬性值命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新屬性值所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員修改屬性值資訊
/// - 屬性值資料維護
/// - 屬性結構調整
/// 
/// 設計特點：
/// - 與 BrandUpdateCommand 不同，所有屬性皆為必填（非可空）
/// - 遵循 HTTP PUT 語意（完整更新）
/// - 未提供部分更新功能（PATCH）
/// 
/// 注意事項：
/// - 更新屬性值可能會影響已關聯的產品
/// - 建議在更新前檢查是否有產品使用此屬性值
/// </summary>
public class AttributeValueUpdateCommand : IRequest
{
    /// <summary>
    /// 屬性值唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的屬性值
    /// - 必須是資料庫中已存在的屬性值 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的屬性值
    /// 
    /// 錯誤處理：
    /// - 如果屬性值不存在，會拋出 Failure.BadRequest("屬性值不存在")
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 屬性值內容
    /// 
    /// 用途：
    /// - 屬性的具體可選值
    /// - 顯示給使用者看的文字
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 同一屬性鍵下不能重複（由資料庫唯一約束保證）
    /// 
    /// 範例：
    /// - 「紅色」、「藍色」、「黑色」：顏色屬性的值
    /// - 「S」、「M」、「L」、「XL」：尺寸屬性的值
    /// - 「棉」、「聚酯纖維」：材質屬性的值
    /// 
    /// 注意事項：
    /// - 建議長度限制：1-50 字元
    /// - 不應包含特殊字元（除非必要）
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 排序順序
    /// 
    /// 用途：
    /// - 控制屬性值在列表中的顯示順序
    /// - 數字越小越前面
    /// 
    /// 預設值：
    /// - 0
    /// 
    /// 使用範例：
    /// - 0：顯示在最前面
    /// - 10：顯示在後面
    /// 
    /// 注意事項：
    /// - 建議使用 10、20、30 等間隔值，方便後續插入新值
    /// - 不應為負數
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 屬性值描述
    /// 
    /// 用途：
    /// - 提供屬性值的詳細說明
    /// - 可用於提示或輔助文字
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 說明屬性值的具體含義
    /// - 提供額外資訊（如材質成分）
    /// - SEO 優化
    /// 
    /// 注意事項：
    /// - 建議長度限制：0-200 字元
    /// - 可為 null，表示無描述
    /// </summary>
    public string Description { get; set; }
}

/// <summary>
/// 更新屬性值命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AttributeValueUpdateCommand 命令
/// - 查詢屬性值是否存在
/// - 更新屬性值資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeValueUpdateCommand> 介面
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
/// - 未檢查 Value 是否與同屬性鍵下的其他值重複
/// - 建議考慮使用樂觀鎖（Optimistic Concurrency）防止並發更新衝突
/// 
/// 參考實作：
/// - AttributeValueDeleteHandler：類似的刪除邏輯
/// - BrandUpdateHandler：類似的更新邏輯
/// </summary>
internal class AttributeValueUpdateHandler : IRequestHandler<AttributeValueUpdateCommand>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性值資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs）
    /// - 提供泛型方法 GetValueAsync、SaveChangeAsync 等
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
    /// <param name="repository">屬性鍵倉儲，用於查詢和更新屬性值</param>
    public AttributeValueUpdateHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新屬性值命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢屬性值實體
    /// 2. 驗證屬性值是否存在
    /// 3. 更新屬性值屬性
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 屬性值不存在：拋出 Failure.BadRequest("屬性值不存在")
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否有產品使用此屬性值
    /// - 建議檢查 Value 是否與同屬性鍵下的其他值重複
    /// 
    /// 參考實作：
    /// - AttributeValueDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - BrandUpdateHandler.HandleAsync：類似的更新邏輯
    /// </summary>
    /// <param name="request">更新屬性值命令物件，包含屬性值 ID 和要更新的欄位</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(AttributeValueUpdateCommand request)
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
        if(attributeValue == null) 
            throw Failure.BadRequest("屬性值不存在");

        // ========== 第三步：更新屬性值屬性 ==========
        // 直接更新實體屬性
        // EF Core 會自動追蹤這些變更
        // 注意：這會更新所有欄位，即使值沒有變化
        // 這種設計遵循 HTTP PUT 語意（完整更新）
        // 如果需要部分更新（PATCH 語意），應該檢查每個欄位是否為 null
        attributeValue.Value = request.Value;
        attributeValue.SortOrder = request.SortOrder;
        attributeValue.Description = request.Description;

        // ========== 第四步：儲存變更 ==========
        // 使用 IAttributeKeyRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        // EF Core 會自動產生 UPDATE SQL 語句
        await _repository.SaveChangeAsync();
    }
}
