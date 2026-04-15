using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 刪除 SKU 命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除 SKU 所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的 SKU 規格
/// - 清理測試資料
/// - 商品規格調整
/// 
/// 注意事項：
/// - 刪除 SKU 可能會影響已關聯的庫存記錄
/// - 建議在刪除前檢查是否有庫存記錄
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class SkuDeleteCommand : IRequest
{
    /// <summary>
    /// SKU 唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的 SKU
    /// - 必須是資料庫中已存在的 SKU ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的 SKU
    /// 
    /// 錯誤處理：
    /// - 如果 SKU 不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除 SKU 命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SkuDeleteCommand 命令
/// - 查詢 SKU 是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SkuDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IProductRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查 SKU 是否有關聯的庫存記錄
/// - 未檢查是否有訂單使用此 SKU
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// </summary>
internal class SkuDeleteHandler : IRequestHandler<SkuDeleteCommand>
{
    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品和 SKU 資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/ProductRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 擴展了 AddSku、GetSkuAsync、RemoveSku 等方法
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於查詢和刪除 SKU</param>
    public SkuDeleteHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// 處理刪除 SKU 命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢 SKU 實體
    /// 2. 驗證 SKU 是否存在
    /// 3. 刪除 SKU
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - SKU 不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 刪除 SKU 可能會影響庫存記錄
    /// - 建議檢查是否有庫存記錄或訂單使用此 SKU
    /// </summary>
    /// <param name="request">刪除 SKU 命令物件，包含 SKU ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(SkuDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢 SKU 實體 ==========
        // 使用 IProductRepository.GetSkuAsync() 查詢 SKU
        // 這個方法會從資料庫中取得完整的 SKU 實體
        var sku = await _productRepository.GetSkuAsync(request.Id);
        
        // ========== 第二步：驗證 SKU 是否存在 ==========
        // 如果找不到 SKU，拋出 404 錯誤
        // 這種情況可能發生在：
        // - SKU ID 不存在
        // - SKU 已被刪除（軟刪除）
        if (sku == null)
            throw Failure.NotFound($"SKU 不存在，ID: {request.Id}");

        // ========== 第三步：刪除 SKU ==========
        // 使用 IProductRepository.RemoveSku() 刪除 SKU
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新 SKU 的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        _productRepository.RemoveSku(sku);

        // ========== 第四步：儲存變更 ==========
        // 使用 IProductRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _productRepository.SaveChangeAsync();
    }
}
