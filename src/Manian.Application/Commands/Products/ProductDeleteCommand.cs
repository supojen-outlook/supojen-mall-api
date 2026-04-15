using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 刪除商品命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除商品所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的商品
/// - 清理測試資料
/// - 商品結構重組
/// 
/// 注意事項：
/// - 刪除商品會一併刪除所有關聯的 SKU（由資料庫級聯刪除保證）
/// - 建議在刪除前檢查是否有訂單使用此商品
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class ProductDeleteCommand : IRequest
{
    /// <summary>
    /// 商品唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的商品
    /// - 必須是資料庫中已存在的商品 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的商品
    /// 
    /// 錯誤處理：
    /// - 如果商品不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 刪除商品命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ProductDeleteCommand 命令
/// - 查詢商品是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ProductDeleteCommand> 介面
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
/// - 未檢查商品是否有關聯的訂單
/// - 未檢查商品是否有庫存記錄
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - BrandDeleteHandler：類似的刪除邏輯
/// - CategoryDeleteHandler：類似的刪除邏輯
/// </summary>
internal class ProductDeleteHandler : IRequestHandler<ProductDeleteCommand>
{
    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/ProductRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 擴展了 AddSku、GetSkuAsync 等方法
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於查詢和刪除商品</param>
    public ProductDeleteHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// 處理刪除商品命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢商品實體
    /// 2. 驗證商品是否存在
    /// 3. 刪除商品
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 商品不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 刪除商品會一併刪除所有關聯的 SKU（由資料庫級聯刪除保證）
    /// - 建議檢查是否有訂單使用此商品
    /// - 建議檢查是否有庫存記錄
    /// 
    /// 參考實作：
    /// - BrandDeleteHandler.HandleAsync：類似的刪除邏輯
    /// - CategoryDeleteHandler.HandleAsync：類似的刪除邏輯
    /// </summary>
    /// <param name="request">刪除商品命令物件，包含商品 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(ProductDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢商品實體 ==========
        // 使用 IProductRepository.GetByIdAsync() 查詢商品
        // 這個方法會從資料庫中取得完整的商品實體
        var product = await _productRepository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證商品是否存在 ==========
        // 如果找不到商品，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 商品 ID 不存在
        // - 商品已被刪除（軟刪除）
        if (product == null)
            throw Failure.NotFound($"商品不存在，ID: {request.Id}");

        // ========== 第三步：刪除商品 ==========
        // 使用 IProductRepository.Delete() 刪除商品
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新商品的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        // 根據 SkuConfiguration.cs 的配置，刪除商品會一併刪除所有關聯的 SKU
        _productRepository.Delete(product);

        // ========== 第四步：儲存變更 ==========
        // 使用 IProductRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        // 包括商品和所有關聯 SKU 的刪除操作
        await _productRepository.SaveChangeAsync();
    }
}
