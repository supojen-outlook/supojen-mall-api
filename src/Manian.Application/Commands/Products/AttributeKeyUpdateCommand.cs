using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 更新屬性鍵命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新屬性鍵所需的資訊
/// 設計模式：實作 IRequest<AttributeKey>，表示這是一個會回傳 AttributeKey 實體的命令
/// 
/// 使用場景：
/// - 管理員修改屬性鍵資訊
/// - 屬性鍵資料維護
/// - 屬性結構調整
/// 
/// 設計特點：
/// - 所有屬性皆為可空（nullable），支援部分更新
/// - 未提供的欄位保持原值不變
/// - 遵循 HTTP PATCH 語意（部分更新）
/// 
/// 注意事項：
/// - NewCode 屬性在 Handler 中未被使用，可能是設計遺留或未完成功能
/// - InputType 和 Status 的驗證在 AttributeKey 實體中進行
/// </summary>
public class AttributeKeyUpdateCommand : IRequest<AttributeKey>
{
    /// <summary>
    /// 屬性鍵唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的屬性鍵
    /// - 必須是資料庫中已存在的屬性鍵 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的屬性鍵
    /// 
    /// 錯誤處理：
    /// - 如果屬性鍵不存在，會拋出 Failure.NotFound("找不到該屬性鍵")
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 屬性名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的屬性名稱
    /// - 用於屬性列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 建議長度限制：2-50 字元
    /// - 不應為空白或僅包含空白字元
    /// 
    /// 範例：
    /// - "顏色"、"尺寸"、"材質"、"品牌"
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 屬性鍵的新代碼（未使用）
    /// 
    /// 用途：
    /// - 預期用於更新屬性鍵的代碼
    /// 
    /// 注意事項：
    /// - 此屬性在 Handler 中未被使用
    /// - 可能是設計遺留或未完成功能
    /// - 建議確認是否需要實作或移除此屬性
    /// </summary>
    public string? NewCode { get; set; }

    /// <summary>
    /// 屬性描述
    /// 
    /// 用途：
    /// - 提供屬性的詳細資訊
    /// - 可用於 SEO 描述
    /// - 說明屬性的使用場景或限制
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 建議長度限制：0-500 字元
    /// 
    /// 範例：
    /// - "用於區分產品的顏色變體"
    /// - "產品的尺寸規格，如 S、M、L、XL"
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 輸入類型
    /// 
    /// 用途：
    /// - 決定前端顯示的輸入元件類型
    /// - 影響使用者輸入的方式和驗證規則
    /// 
    /// 可選值：
    /// - "select"：下拉選單（預設值）
    /// - "text"：文字輸入框
    /// - "number"：數字輸入框
    /// - "checkbox"：複選框
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 只能接受上述四個值之一
    /// - 設定其他值會拋出 ArgumentException（在 AttributeKey 實體中驗證）
    /// 
    /// 使用場景：
    /// - "select"：用於有固定選項的屬性（如顏色、尺寸）
    /// - "text"：用於自由輸入的屬性（如備註）
    /// - "number"：用於數值屬性（如重量、長度）
    /// - "checkbox"：用於多選屬性（如功能特點）
    /// </summary>
    public string? InputType { get; set; }

    /// <summary>
    /// 是否為必填屬性
    /// 
    /// 用途：
    /// - 控制該屬性在商品發布時是否必須填寫
    /// - 影響前端驗證邏輯
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - true：關鍵屬性（如顏色、尺寸）
    /// - false：可選屬性（如備註、特殊規格）
    /// 
    /// 注意事項：
    /// - 銷售屬性通常設為必填
    /// - 設為必填後，所有使用此屬性的商品都必須提供值
    /// </summary>
    public bool? IsRequired { get; set; }

    /// <summary>
    /// 屬性狀態
    /// 
    /// 可選值：
    /// - "active"：啟用狀態，屬性可被使用
    /// - "inactive"：停用狀態，屬性不可被使用
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - "active"：正常使用的屬性
    /// - "inactive"：暫停使用但保留資料的屬性
    /// 
    /// 注意事項：
    /// - 停用屬性不會影響已使用該屬性的商品
    /// - 建議使用軟刪除（設為 inactive）而非硬刪除
    /// 
    /// 驗證規則：
    /// - 只能接受 "active" 或 "inactive" 兩個值
    /// - 設定其他值會拋出 ArgumentException（在 AttributeKey 實體中驗證）
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 是否為銷售屬性
    /// 
    /// 用途：
    /// - 決定屬性是否用於生成 SKU（庫存單位）
    /// - 影響商品變體的生成邏輯
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - true：用於生成 SKU（如顏色、尺寸）
    /// - false：僅為描述性屬性（如保固期、材質）
    /// 
    /// 注意事項：
    /// - 銷售屬性會影響庫存管理
    /// - 建議銷售屬性使用 "select" 輸入類型
    /// - 銷售屬性的值通常需要預先定義
    /// </summary>
    public bool? ForSales { get; set; }

    /// <summary>
    /// 屬性單位
    /// 
    /// 用途：
    /// - 指定屬性值的單位
    /// - 用於數值型屬性
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - 重量屬性："g"（公克）、"kg"（公斤）
    /// - 長度屬性："cm"（公分）、"m"（公尺）
    /// - 溫度屬性："°C"（攝氏度）
    /// 
    /// 注意事項：
    /// - 單位應該符合國際標準或行業慣例
    /// - 單位應該與輸入類型（InputType）匹配
    /// - 單位通常只對數值型屬性有意義
    /// </summary>
    public string? Unit { get; set; }
}

/// <summary>
/// 更新屬性鍵命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AttributeKeyUpdateCommand 命令
/// - 查詢屬性鍵是否存在
/// - 更新屬性鍵資訊
/// - 回傳更新後的屬性鍵實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeKeyUpdateCommand, AttributeKey> 介面
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
/// - 未檢查 Status 是否為有效值（active/inactive）
/// - 未檢查 InputType 是否為有效值（select/text/number/checkbox）
/// - 未檢查是否會影響已關聯的類別或產品
/// - NewCode 屬性未被使用
/// 
/// 參考實作：
/// - BrandUpdateHandler：類似的更新邏輯
/// - CategoryUpdateHandler：類似的更新邏輯
/// </summary>
internal class AttributeKeyUpdateHandler : IRequestHandler<AttributeKeyUpdateCommand, AttributeKey>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性鍵資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
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
    /// <param name="repository">屬性鍵倉儲，用於查詢和更新屬性鍵</param>
    public AttributeKeyUpdateHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新屬性鍵命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢屬性鍵實體
    /// 2. 驗證屬性鍵是否存在
    /// 3. 更新屬性鍵屬性（只更新非 null 的欄位）
    /// 4. 儲存變更
    /// 5. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - 屬性鍵不存在：拋出 Failure.NotFound("找不到該屬性鍵")
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查是否會影響已關聯的類別或產品
    /// - NewCode 屬性在 Handler 中未被使用
    /// 
    /// 參考實作：
    /// - BrandUpdateHandler.HandleAsync：類似的更新邏輯
    /// - CategoryUpdateHandler.HandleAsync：類似的更新邏輯
    /// </summary>
    /// <param name="request">更新屬性鍵命令物件，包含屬性鍵 ID 和要更新的欄位</param>
    /// <returns>更新後的 AttributeKey 實體，包含所有屬性</returns>
    public async Task<AttributeKey> HandleAsync(AttributeKeyUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢屬性鍵實體 ==========
        // 使用 IAttributeKeyRepository.GetByIdAsync() 查詢屬性鍵
        // 這個方法會從資料庫中取得完整的屬性鍵實體
        var attributeKey = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證屬性鍵是否存在 ==========
        // 如果找不到屬性鍵，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 屬性鍵 ID 不存在
        // - 屬性鍵已被刪除（軟刪除）
        if (attributeKey == null)
            throw Failure.NotFound("找不到該屬性鍵");
        
        // ========== 第三步：更新屬性鍵屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新基本屬性
        if (request.Name != null) attributeKey.Name = request.Name;
        if (request.Description != null) attributeKey.Description = request.Description;
        if (request.InputType != null) attributeKey.InputType = request.InputType;
        if (request.Status != null) attributeKey.Status = request.Status;
        
        // 更新布爾屬性
        if (request.IsRequired != null) attributeKey.IsRequired = request.IsRequired.Value;
        if (request.ForSales != null) attributeKey.ForSales = request.ForSales.Value;
        
        // 更新單位
        if (request.Unit != null) attributeKey.Unit = request.Unit;
        
        // 注意：NewCode 屬性在 Handler 中未被使用
        // 可能是設計遺留或未完成功能
        
        // ========== 第四步：儲存變更 ==========
        // 使用 IAttributeKeyRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第五步：回傳更新後的實體 ==========
        // 回傳更新後的屬性鍵實體，包含所有屬性
        // 注意：這個實體會被 EF Core 追蹤
        // 如果後續需要修改屬性，可以直接修改並呼叫 SaveChangesAsync()
        return attributeKey;
    }
}
