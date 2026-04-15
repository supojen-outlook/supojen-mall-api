using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 刪除品牌命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除品牌所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的品牌
/// - 清理測試資料
/// - 品牌結構重組
/// 
/// 注意事項：
/// - 刪除品牌可能會影響已關聯的產品
/// - 建議在刪除前檢查是否有產品使用此品牌
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class BrandDeleteCommand : IRequest
{
    /// <summary>
    /// 品牌唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的品牌
    /// - 必須是資料庫中已存在的品牌 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的品牌
    /// 
    /// 錯誤處理：
    /// - 如果品牌不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除品牌命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 BrandDeleteCommand 命令
/// - 查詢品牌是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<BrandDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IBrandRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查品牌是否有子品牌
/// - 未檢查是否有產品使用此品牌
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// </summary>
public class BrandDeleteHandler : IRequestHandler<BrandDeleteCommand>
{
    /// <summary>
    /// 品牌倉儲介面
    /// 
    /// 用途：
    /// - 存取品牌資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/BrandRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// </summary>
    private readonly IBrandRepository _brandRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="brandRepository">品牌倉儲，用於查詢和刪除品牌</param>
    public BrandDeleteHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    /// <summary>
    /// 處理刪除品牌命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢品牌實體
    /// 2. 驗證品牌是否存在
    /// 3. 刪除品牌
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 品牌不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 考慮實作軟刪除（標記為已刪除）而非硬刪除
    /// - 建議檢查是否有子品牌或產品使用此品牌
    /// </summary>
    /// <param name="request">刪除品牌命令物件，包含品牌 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(BrandDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢品牌實體 ==========
        // 使用 IBrandRepository.GetByIdAsync() 查詢品牌
        // 這個方法會從資料庫中取得完整的品牌實體
        var brand = await _brandRepository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證品牌是否存在 ==========
        // 如果找不到品牌，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 品牌 ID 不存在
        // - 品牌已被刪除（軟刪除）
        if (brand == null)
            throw Failure.NotFound($"品牌 ID 為 {request.Id} 的品牌不存在");

        // ========== 第三步：刪除品牌 ==========
        // 使用 IBrandRepository.DeleteAsync() 刪除品牌
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新品牌的狀態欄位
        _brandRepository.Delete(brand);

        // ========== 第四步：儲存變更 ==========
        // 使用 IBrandRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _brandRepository.SaveChangeAsync();
    }
}
