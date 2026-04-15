using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增類別屬性關聯的請求物件
/// 
/// 用途：
/// - 將指定的屬性鍵 關聯到指定的類別
/// - 用於商品發布或類別管理時，動態為類別分配可用屬性
/// 
/// 設計模式：
/// - 實作 IRequest<AttributeKey>，表示這是一個會返回資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CategoryAttributeAddCommandHandler 配合使用，完成寫入並返回實體
/// 
/// 使用場景：
/// - 後台類別管理：為類別勾選可用的屬性規格
/// - API 整合：外部系統同步類別屬性配置
/// 
/// 業務規則：
/// - 同一個類別不能重複關聯同一個屬性鍵
/// - 關聯建立後，該類別下的商品即可使用該屬性
/// - 執行成功後會返回被關聯的 AttributeKey 物件
/// 
/// 參考實作：
/// - CategoryAttributeDeleteCommand：移除關聯的命令
/// </summary>
public class CategoryAttributeAddCommand : IRequest<AttributeKey>
{
    /// <summary>
    /// 類別 ID
    /// 
    /// 用途：
    /// - 指定要關聯屬性的目標類別
    /// 
    /// 驗證：
    /// - 必須大於 0
    /// - 必須存在於資料庫中
    /// 
    /// 範例：
    /// - CategoryId = 5：將屬性關聯到 ID 為 5 的類別
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// 屬性鍵 ID
    /// 
    /// 用途：
    /// - 指定要關聯到類別的屬性鍵
    /// 
    /// 驗證：
    /// - 必須大於 0
    /// - 必須存在於資料庫中
    /// 
    /// 範例：
    /// - AttributeKeyId = 10：關聯 ID 為 10 的屬性鍵
    /// </summary>
    public int AttributeKeyId { get; init; }
}

/// <summary>
/// 新增類別屬性關聯的處理器
/// 
/// 職責：
/// - 接收 CategoryAttributeAddCommand 請求
/// - 建構類別與屬性鍵的關聯
/// - 從資料庫取得符合條件的屬性鍵實體
/// - 回傳 AttributeKey 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoryAttributeAddCommand, AttributeKey> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICategoryRepository 與 IAttributeKeyRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
public class CategoryAttributeAddCommandHandler : IRequestHandler<CategoryAttributeAddCommand, AttributeKey>
{
    /// <summary>
    /// 類別倉儲介面
    /// 
    /// 用途：
    /// - 存取類別及其關聯資料
    /// - 執行 AddAttributeKey 方法
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/CategoryRepository.cs）
    /// </summary>
    private readonly ICategoryRepository _categoryRepository;

    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 查詢屬性鍵詳細資訊
    /// - 用於返回完整的 AttributeKey 實體給呼叫端
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/AttributeKeyRepository.cs）
    /// </summary>
    private readonly IAttributeKeyRepository _attributeKeyRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="categoryRepository">類別倉儲，用於操作關聯</param>
    /// <param name="attributeKeyRepository">屬性鍵倉儲，用於查詢實體</param>
    public CategoryAttributeAddCommandHandler(
        ICategoryRepository categoryRepository, 
        IAttributeKeyRepository attributeKeyRepository)
    {
        _categoryRepository = categoryRepository;
        _attributeKeyRepository = attributeKeyRepository;
    }

    /// <summary>
    /// 處理新增關聯請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 呼叫 CategoryRepository 的 AddAttributeKey 方法建立關聯
    /// 2. 呼叫 SaveChangesAsync 將變更寫入資料庫
    /// 3. 呼叫 AttributeKeyRepository 查詢完整的 AttributeKey 實體
    /// 4. 將查詢到的實體作為結果返回
    /// 
    /// 查詢特性：
    /// - 支援事務
    /// - 確保關聯建立成功後才返回資料
    /// </summary>
    /// <param name="command">新增關聯請求物件</param>
    /// <returns>被關聯的 AttributeKey 實體</returns>
    public async Task<AttributeKey> HandleAsync(CategoryAttributeAddCommand command)
    {
        // 1. 呼叫 Repository 方法處理業務邏輯
        //    Repository 內部會處理重複檢查，若已存在則忽略
        _categoryRepository.AddAttributeKey(command.CategoryId, command.AttributeKeyId);

        // 2. 將變更持久化到資料庫
        //    注意：這一步是必須的，否則關聯不會生效
        await _categoryRepository.SaveChangeAsync();

        // 3. 查詢並返回完整的 AttributeKey 實體
        //    使用 GetByIdAsync 獲取最新的資料狀態
        var attributeKey = await _attributeKeyRepository.GetByIdAsync(command.AttributeKeyId);

        // 4. 返回結果
        //    假設資料一定存在 (因為前面已建立關聯)，直接返回
        return attributeKey!;
    }
}
