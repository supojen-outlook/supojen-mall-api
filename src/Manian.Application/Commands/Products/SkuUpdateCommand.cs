using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.ValueObjects;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 更新 SKU 命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝更新商品 SKU (Stock Keeping Unit) 所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Sku>，表示這是一個會回傳 Sku 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SkuUpdateHandler 配合使用，完成更新 SKU 的業務邏輯
/// 
/// 使用場景：
/// - 管理員更新商品規格（如：價格、庫存、圖片等）
/// - 批量更新 SKU 狀態
/// - API 端點接收 SKU 更新請求
/// 
/// 設計特點：
/// - 所有屬性都是可選的（使用可空型別）
/// - 只更新提供的屬性，未提供的保持不變
/// - 支援部分更新（Partial Update）
/// 
/// 注意事項：
/// - Specifications 陣列會完全替換 Sku.Specs
/// - 不區分銷售屬性和非銷售屬性
/// - 如果 Specifications 為 null，則不更新 Sku.Specs
/// </summary>
public class SkuUpdateCommand : IRequest<Sku>
{
    /// <summary>
    /// SKU 唯一識別碼
    /// 
    /// 用途：
    /// - 識別要更新的 SKU
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

    /// <summary>
    /// SKU 顯示名稱（可選）
    /// 
    /// 用途：
    /// - 更新顯示給使用者看的 SKU 名稱
    /// - 通常包含規格資訊
    /// 
    /// 驗證規則：
    /// - 如果提供，不能為空白或僅包含空白字元
    /// - 建議長度限制：1-200 字元
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// SKU 銷售價格（可選）
    /// 
    /// 用途：
    /// - 更新該規格的價格
    /// - 可覆蓋商品基礎價格
    /// 
    /// 驗證規則：
    /// - 如果提供，必須大於或等於 0
    /// - 建議使用 2 位小數
    /// 
    /// 注意事項：
    /// - 不同規格可以有不同的價格
    /// - 價格變更會影響訂單計算
    /// </summary>
    public decimal? Price { get; set; }
    
    /// <summary>
    /// 實際庫存數量（可選）
    /// 
    /// 用途：
    /// - 更新該規格的可用庫存
    /// - 用於庫存管理和訂單處理
    /// 
    /// 驗證規則：
    /// - 如果提供，必須大於或等於 0
    /// 
    /// 注意事項：
    /// - 庫存不足時應禁止下單
    /// - 建議實作庫存預占機制
    /// </summary>
    public int? StockQuantity { get; set; }

    /// <summary>
    /// SKU 狀態（可選）
    /// 
    /// 用途：
    /// - 更新該規格是否可見和可購買
    /// 
    /// 可選值：
    /// - "active"：啟用（可見且可購買）
    /// - "inactive"：停用（不可見且不可購買）
    /// 
    /// 使用場景：
    /// - 暫時停用某個規格（而非刪除）
    /// - 預先建立規格但尚未啟用
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 是否為預設 SKU（可選）
    /// 
    /// 用途：
    /// - 更新標識商品頁面預先顯示的 SKU
    /// - 簡化使用者選擇流程
    /// 
    /// 約束：
    /// - 每個商品只能有一個預設 SKU
    /// - 由業務邏輯確保唯一性
    /// 
    /// 使用場景：
    /// - 更改商品頁面首次載入時顯示的規格
    /// - 更改搜尋結果中顯示的預設價格
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// SKU 專屬圖片 URL（可選）
    /// 
    /// 用途：
    /// - 更新該規格的專屬圖片
    /// - 未設定時使用商品主圖
    /// 
    /// 驗證規則：
    /// - 如果提供，必須是有效的 URL 格式
    /// - 建議圖片尺寸：800x800 像素
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// SKU 規格陣列（可選）
    /// 
    /// 用途：
    /// - 更新該 SKU 的規格參數
    /// - 完全替換 Sku.Specs
    /// 
    /// 處理方式：
    /// - 如果提供，完全替換 Sku.Specs
    /// - 如果為 null，則不更新 Sku.Specs
    /// - 不區分銷售屬性和非銷售屬性
    /// 
    /// 使用範例：
    /// <code>
    /// new Specification[] {
    ///     new Specification { KeyId = 1, ValueId = 100, Name = "顏色", Value = "黑色" },
    ///     new Specification { KeyId = 2, ValueId = 200, Name = "容量", Value = "128G" }
    /// }
    /// </code>
    /// </summary>
    public Specification[]? Specifications { get; set; }

    /// <summary>
    /// 計量單位 ID（可選）
    /// 
    /// 用途：
    /// - 更新該規格的計量單位（如：個、件、箱等）
    /// - 用於庫存管理和訂單處理
    /// 
    /// 驗證規則：
    /// - 如果提供，必須對應資料庫中存在的計量單位 ID
    /// 
    /// 範例：
    /// - 1：個
    /// - 2：件
    /// - 3：箱
    /// </summary>
    public int? UnitOfMeasureId { get; set; }
}

/// <summary>
/// 更新 SKU 命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SkuUpdateCommand 命令
/// - 查詢 SKU 是否存在
/// - 更新 SKU 屬性
/// - 儲存變更到資料庫
/// - 回傳更新後的 Sku 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SkuUpdateCommand, Sku> 介面
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
/// - 未檢查 IsDefault 的唯一性（每個商品只能有一個預設 SKU）
/// - 未處理 Specifications 的合併邏輯（目前是完全替換）
/// </summary>
internal class SkuUpdateHandler : IRequestHandler<SkuUpdateCommand, Sku>
{
    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品和 SKU 資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/ProductRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、UpdateAsync 等
    /// - 擴展了 AddSku、GetSkuAsync、RemoveSku 等方法
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於查詢和更新 SKU</param>
    public SkuUpdateHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// 處理更新 SKU 命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢 SKU 實體
    /// 2. 驗證 SKU 是否存在
    /// 3. 更新 SKU 屬性（只更新提供的屬性）
    /// 4. 處理 Specifications（如果提供則完全替換）
    /// 5. 儲存變更到資料庫
    /// 6. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - SKU 不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 所有屬性都是可選的，只更新提供的屬性
    /// - Specifications 陣列會完全替換 Sku.Specs
    /// - 不區分銷售屬性和非銷售屬性
    /// - 如果 Specifications 為 null，則不更新 Sku.Specs
    /// </summary>
    /// <param name="request">更新 SKU 命令物件，包含 SKU 的更新資訊</param>
    /// <returns>更新後的 Sku 實體</returns>
    public async Task<Sku> HandleAsync(SkuUpdateCommand request)
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

        // ========== 第三步：更新 SKU 屬性 ==========
        // 只更新提供的屬性，未提供的保持不變
        if (request.Name != null)
            sku.Name = request.Name;
        
        if (request.Price.HasValue)
            sku.Price = request.Price.Value;
        
        if (request.StockQuantity.HasValue)
            sku.StockQuantity = request.StockQuantity.Value;
        
        if (request.Status != null)
            sku.Status = request.Status;
        
        if (request.IsDefault.HasValue)
            sku.IsDefault = request.IsDefault.Value;
        
        if (request.ImageUrl != null)
            sku.ImageUrl = request.ImageUrl;
        
        if (request.UnitOfMeasureId.HasValue)
            sku.UnitOfMeasureId = request.UnitOfMeasureId.Value;

        // ========== 第四步：處理 Specifications ==========
        // 如果提供了 Specifications，則完全替換 Sku.Specs
        if (request.Specifications != null)
        {
            sku.Specs = request.Specifications.Length > 0 
                ? request.Specifications.ToList() 
                : new List<Specification>();
        }

        // ========== 第五步：儲存變更 ==========
        // 使用 IProductRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _productRepository.SaveChangeAsync();

        // ========== 第六步：回傳更新後的實體 ==========
        return sku;
    }
}
