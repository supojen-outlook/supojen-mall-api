using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增屬性鍵命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增商品屬性鍵所需的資訊
/// 設計模式：實作 IRequest<AttributeKey>，表示這是一個會回傳 AttributeKey 實體的命令
/// 
/// 使用場景：
/// - 管理員建立新的商品屬性（如顏色、尺寸、材質等）
/// - 定義屬性的輸入方式（下拉選單、文字框、數字輸入等）
/// - 設定屬性是否為必填或銷售屬性
/// 
/// 設計特點：
/// - 支援一次性新增屬性鍵及其屬性值
/// - 屬性值列表為可選，可後續新增
/// - 使用唯一識別碼服務生成 ID
/// </summary>
public class AttributeKeyAddCommand : IRequest<AttributeKey>
{
    /// <summary>
    /// 屬性名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的屬性名稱
    /// - 用於屬性列表和詳細頁面
    /// 
    /// 驗證規則：
    /// - 必填欄位
    /// - 建議長度限制：2-50 字元
    /// - 不應為空白或僅包含空白字元
    /// 
    /// 範例：
    /// - "顏色"、"尺寸"、"材質"、"品牌"
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 屬性描述
    /// 
    /// 用途：
    /// - 提供屬性的詳細資訊
    /// - 可用於 SEO 描述
    /// - 說明屬性的使用場景或限制
    /// 
    /// 驗證規則：
    /// - 選填欄位
    /// - 建議長度限制：0-500 字元
    /// 
    /// 範例：
    /// - "用於區分產品的顏色變體"
    /// - "產品的尺寸規格，如 S、M、L、XL"
    /// </summary>
    public string Description { get; set; }

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
    /// 驗證規則：
    /// - 必填欄位
    /// - 只能接受上述四個值之一
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用場景：
    /// - "select"：用於有固定選項的屬性（如顏色、尺寸）
    /// - "text"：用於自由輸入的屬性（如備註）
    /// - "number"：用於數值屬性（如重量、長度）
    /// - "checkbox"：用於多選屬性（如功能特點）
    /// </summary>
    public string InputType { get; set; }

    /// <summary>
    /// 是否為必填屬性
    /// 
    /// 用途：
    /// - 控制該屬性在商品發布時是否必須填寫
    /// - 影響前端驗證邏輯
    /// 
    /// 使用場景：
    /// - true：關鍵屬性（如顏色、尺寸）
    /// - false：可選屬性（如備註、特殊規格）
    /// 
    /// 注意事項：
    /// - 銷售屬性通常設為必填
    /// - 設為必填後，所有使用此屬性的商品都必須提供值
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 是否為銷售屬性
    /// 
    /// 用途：
    /// - 決定屬性是否用於生成 SKU（庫存單位）
    /// - 影響商品變體的生成邏輯
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
    public bool ForSales { get; set; }

    /// <summary>
    /// 屬性狀態
    /// 
    /// 可選值：
    /// - "active"：啟用狀態，屬性可被使用
    /// - "inactive"：停用狀態，屬性不可被使用
    /// 
    /// 使用場景：
    /// - "active"：正常使用的屬性
    /// - "inactive"：暫停使用但保留資料的屬性
    /// 
    /// 注意事項：
    /// - 停用屬性不會影響已使用該屬性的商品
    /// - 建議使用軟刪除（設為 inactive）而非硬刪除
    /// </summary>
    public string Status { get; set; }
}

/// <summary>
/// 新增屬性鍵命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AttributeKeyAddCommand 命令
/// - 建立新的 AttributeKey 實體
/// - 新增屬性值（如果提供）
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 AttributeKey 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeKeyAddCommand, AttributeKey> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IAttributeKeyRepository 和 IUniqueIdentifier
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查屬性名稱是否重複
/// - 未驗證 Status 是否為有效值（active/inactive）
/// - 未驗證 InputType 是否為有效值（select/text/number/checkbox）
/// - 未檢查屬性值是否重複
/// </summary>
internal class AttributeKeyAddHandler : IRequestHandler<AttributeKeyAddCommand, AttributeKey>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性鍵相關資料
    /// - 提供新增、查詢、更新、刪除等操作
    /// - 提供新增屬性值的方法 (AddValues)
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 提供特定方法 AddValues、GetValuesAsync、GetCategoryAttributesAsync
    /// </summary>
    private readonly IAttributeKeyRepository _keyRepository;

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
    /// 
    /// 使用場景：
    /// - 生成 AttributeKey 實體的 Id
    /// - 生成 AttributeValue 實體的 Id
    /// - 生成 SortOrder 排序值
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="keyRepository">屬性鍵倉儲，用於存取屬性鍵資料</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於生成 ID</param>
    public AttributeKeyAddHandler(IAttributeKeyRepository keyRepository, IUniqueIdentifier uniqueIdentifier)
    {
        _keyRepository = keyRepository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增屬性鍵命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 建立新的 AttributeKey 實體
    /// 2. 設定實體屬性
    /// 3. 如果有提供屬性值，新增屬性值
    /// 4. 將實體加入倉儲
    /// 5. 儲存變更到資料庫
    /// 6. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 目前未實作明確的錯誤處理
    /// - 建議加入屬性名稱重複檢查
    /// - 建議加入 Status 和 InputType 驗證
    /// </summary>
    /// <param name="request">新增屬性鍵命令物件，包含屬性的所有資訊</param>
    /// <returns>儲存後的 AttributeKey 實體，包含資料庫自動生成的欄位</returns>
    public async Task<AttributeKey> HandleAsync(AttributeKeyAddCommand request)
    {
        // ========== 第一步：建立新的 AttributeKey 實體 ==========
        var key = new AttributeKey
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            Name = request.Name,
            Description = request.Description,
            InputType = request.InputType,
            IsRequired = request.IsRequired,
            Status = request.Status,
            ForSales = request.ForSales,
            
            // 產生隨機排序值（用於屬性列表的排序）
            SortOrder = _uniqueIdentifier.NextInt(),
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow,
        };
        
        // ========== 第二步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _keyRepository.Add(key);
        
        // ========== 第三步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 包括 AttributeKey 實體和所有新增的 AttributeValue 實體
        await _keyRepository.SaveChangeAsync();

        // ========== 第四步：回傳儲存後的實體 ==========
        // 回傳的實體包含所有屬性，包括自動生成的 Id 和 CreatedAt
        // 注意：這個實體會被 EF Core 追蹤
        // 如果後續需要修改屬性，可以直接修改並呼叫 SaveChangesAsync()
        return key;
    }
}
