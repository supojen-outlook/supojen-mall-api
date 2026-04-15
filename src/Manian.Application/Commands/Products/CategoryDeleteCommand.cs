using System;
using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 刪除產品類別命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除產品類別所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的產品類別
/// - 清理測試資料
/// - 類別結構重組
/// 
/// 注意事項：
/// - 刪除類別可能會影響已關聯的產品
/// - 建議在刪除前檢查是否有產品使用此類別
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class CategoryDeleteCommand : IRequest
{
    /// <summary>
    /// 類別唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的類別
    /// - 必須是資料庫中已存在的類別 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的類別
    /// 
    /// 錯誤處理：
    /// - 如果類別不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; } 
}


/// <summary>
/// 刪除產品類別命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CategoryDeleteCommand 命令
/// - 查詢類別是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CategoryDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICategoryRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查類別是否有子類別
/// - 未檢查是否有產品使用此類別
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// </summary>
public class CategoryDeleteHandler : IRequestHandler<CategoryDeleteCommand>
{
    /// <summary>
    /// 產品類別倉儲介面
    /// 
    /// 用途：
    /// - 存取產品類別資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/CategoryRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// </summary>
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">產品類別倉儲，用於查詢和刪除類別</param>
    public CategoryDeleteHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除產品類別命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢類別實體
    /// 2. 驗證類別是否存在
    /// 3. 刪除類別
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 類別不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 考慮實作軟刪除（標記為已刪除）而非硬刪除
    /// - 建議檢查是否有子類別或產品使用此類別
    /// </summary>
    /// <param name="request">刪除產品類別命令物件，包含類別 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(CategoryDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢類別實體 ==========
        // 使用 ICategoryRepository.GetByIdAsync() 查詢類別
        // 這個方法會從資料庫中取得完整的類別實體
        var category = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證類別是否存在 ==========
        // 如果找不到類別，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 類別 ID 不存在
        // - 類別已被刪除（軟刪除）
        if(category == null)
            throw Failure.NotFound();

        // ========== 第三步：刪除類別 ==========
        // 使用 ICategoryRepository.DeleteAsync() 刪除類別
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新類別的狀態欄位
        _repository.Delete(category);

        // ========== 第四步：儲存變更 ==========
        // 使用 ICategoryRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
