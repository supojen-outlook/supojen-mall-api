using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增屬性值命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增屬性值所需的資訊
/// 設計模式：實作 IRequest<AttributeValue>，表示這是一個會回傳新增實體的命令
/// 
/// 使用場景：
/// - 管理員為屬性鍵新增可選值（如為「顏色」屬性新增「紅色」）
/// - 商品屬性初始化
/// - 屬性結構擴充
/// 
/// 設計特點：
/// - 回傳型別為 AttributeValue，讓呼叫者可以取得新增後的實體（包含自動生成的 ID）
/// - 與 AttributeValueDeleteCommand 不同，後者不回傳資料（IRequest）
/// 
/// 注意事項：
/// - 同一屬性鍵下不能有重複的值（由資料庫唯一約束保證）
/// - 建議在新增前檢查屬性鍵是否存在
/// </summary>
public class AttributeValueAddCommand : IRequest<AttributeValue>
{
    /// <summary>
    /// 屬性鍵 ID
    /// 
    /// 用途：
    /// - 識別要新增屬性值的目標屬性鍵
    /// - 必須是資料庫中已存在的屬性鍵 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的屬性鍵
    /// 
    /// 範例：
    /// - 1：為「顏色」屬性新增值
    /// - 2：為「尺寸」屬性新增值
    /// 
    /// 錯誤處理：
    /// - 如果屬性鍵不存在，會拋出 Failure.BadRequest()
    /// </summary>
    public int KeyId { get; set; }

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
/// 新增屬性值命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AttributeValueAddCommand 命令
/// - 呼叫 Repository 新增屬性值
/// - 回傳新增後的實體（包含自動生成的 ID）
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeValueAddCommand, AttributeValue> 介面
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
/// 參考實作：
/// - AttributeValueDeleteHandler：類似的刪除邏輯
/// - BrandAddHandler：類似的新增邏輯
/// </summary>
internal class AttributeValueAddHandler : IRequestHandler<AttributeValueAddCommand, AttributeValue>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性值資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs）
    /// - 提供泛型方法 AddValue、SaveChangeAsync 等
    /// - 繼承自 Repository<AttributeKey>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Products/IAttributeKeyRepository.cs
    /// - 擴展了 AddValue、GetValuesAsync 等方法
    /// </summary>
    private readonly IAttributeKeyRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">屬性鍵倉儲，用於新增屬性值</param>
    public AttributeValueAddHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理新增屬性值命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 呼叫 Repository 的 AddValue 方法新增屬性值
    /// 2. 呼叫 SaveChangeAsync 將變更寫入資料庫
    /// 3. 回傳新增後的實體（包含自動生成的 ID）
    /// 
    /// 返回值：
    /// - AttributeValue：新增後的實體，包含自動生成的 ID
    /// 
    /// 錯誤處理：
    /// - 屬性鍵不存在：由 Repository 處理
    /// - 屬性值重複：由資料庫唯一約束處理
    /// 
    /// 注意事項：
    /// - 新增後的實體會包含自動生成的 ID
    /// - 建議在 UI 層顯示新增成功的訊息
    /// 
    /// 參考實作：
    /// - AttributeValueDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - BrandAddHandler.HandleAsync：類似的新增邏輯
    /// </summary>
    /// <param name="request">新增屬性值命令物件，包含屬性鍵 ID 和屬性值資訊</param>
    /// <returns>新增後的屬性值實體，包含自動生成的 ID</returns>
    public async Task<AttributeValue> HandleAsync(AttributeValueAddCommand request)
    {
        // ========== 第一步：新增屬性值 ==========
        // 使用 IAttributeKeyRepository.AddValue() 新增屬性值
        // 這個方法會建立新的 AttributeValue 實體並加入 DbContext
        // 注意：此時尚未寫入資料庫，只是標記為待新增
        var attributeValue = _repository.AddValue(
            request.KeyId,           // 屬性鍵 ID
            request.Value,           // 屬性值內容
            request.SortOrder,       // 排序順序
            request.Description      // 屬性值描述
        );

        // ========== 第二步：儲存變更 ==========
        // 使用 IAttributeKeyRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會執行 INSERT SQL 語句，並自動生成 ID
        await _repository.SaveChangeAsync();

        // ========== 第三步：回傳新增後的實體 ==========
        // 回傳新增後的實體，包含自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return attributeValue;
    }
}
