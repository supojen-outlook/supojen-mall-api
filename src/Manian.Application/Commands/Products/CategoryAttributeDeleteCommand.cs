using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 移除類別屬性關聯的請求物件
/// 
/// 用途：
/// - 移除指定類別下的某個屬性鍵關聯
/// - 用於類別管理時，移除不再需要的屬性選項
/// 
/// 設計模式：
/// - 實作 IRequest，表示這是一個無回傳值的命令
/// - 遵循 CQRS 原則，專注於寫入操作
/// - 與 CategoryAttributeDeleteCommandHandler 配合使用
/// 
/// 業務規則：
/// - 如果關聯不存在，不拋出錯誤 (安靜失敗)
/// - 移除後，該類別下的商品將無法再使用該屬性 (需視前端邏輯而定)
/// </summary>
public class CategoryAttributeDeleteCommand : IRequest
{
    /// <summary>
    /// 類別 ID
    /// 
    /// 用途：
    /// - 指定要移除屬性關聯的目標類別
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// 屬性鍵 ID
    /// 
    /// 用途：
    /// - 指定要從類別中移除的屬性鍵
    /// </summary>
    public int AttributeKeyId { get; init; }
}

/// <summary>
/// 移除類別屬性關聯的處理器
/// 
/// 職責：
/// - 接收 CategoryAttributeDeleteCommand 請求
/// - 呼叫 Repository 執行關聯移除邏輯
/// - 儲存變更並回傳結果
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoryAttributeDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例
/// </summary>
public class CategoryAttributeDeleteCommandHandler : IRequestHandler<CategoryAttributeDeleteCommand>
{
    /// <summary>
    /// 類別屬性關聯的資料存取層
    /// 
    /// 用途：
    /// - 處理類別屬性關聯的資料存取邏輯
    /// - 包含新增、修改、刪除等操作
    /// </summary>
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="repository">類別屬性關聯的資料存取層</param>
    public CategoryAttributeDeleteCommandHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理移除關聯請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 呼叫 Repository 的 RemoveAttributeKey 方法
    /// 2. 呼叫 SaveChangesAsync 將變更寫入資料庫
    /// </summary>
    /// <param name="command">移除關聯請求物件</param>
    public async Task HandleAsync(CategoryAttributeDeleteCommand command)
    {
        // 呼叫 Repository 方法處理業務邏輯
        // Repository 內部會處理是否存在檢查
        _repository.RemoveAttributeKey(command.CategoryId, command.AttributeKeyId);

        // 將變更持久化到資料庫
        await _repository.SaveChangeAsync();
    }
}
