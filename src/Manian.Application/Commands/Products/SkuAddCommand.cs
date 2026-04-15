using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Manian.Domain.ValueObjects;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增 SKU 命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增商品 SKU (Stock Keeping Unit) 所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Sku>，表示這是一個會回傳 Sku 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SkuAddHandler 配合使用，完成新增 SKU 的業務邏輯
/// 
/// 使用場景：
/// - 管理員為商品新增規格（如：iPhone 14 黑色 128G）
/// - 商品初始化時建立預設 SKU
/// - API 端點接收 SKU 新增請求
/// 
/// 設計特點：
/// - 包含 SKU 基本資訊（名稱、價格、庫存等）
/// - 包含規格資訊（Specifications 陣列）
/// - 支援可選屬性（如 ImageUrl）
/// 
/// 注意事項：
/// - Specifications 陣列會直接賦值給 Sku.Specs
/// - 不區分銷售屬性和非銷售屬性
/// - 如果 Specifications 為 null，則 Sku.Specs 為空集合
/// </summary>
public class SkuAddCommand : IRequest<Sku>
{
    /// <summary>
    /// 所屬商品 ID
    /// 
    /// 用途：
    /// - 識別 SKU 所屬的商品
    /// - 建立商品與 SKU 的關聯
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的商品
    /// 
    /// 錯誤處理：
    /// - 如果商品不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// SKU 顯示名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的 SKU 名稱
    /// - 通常包含規格資訊
    /// 
    /// 範例：
    /// - "iPhone 14 黑色 128G"
    /// - "Nike Air Force 1 白色 42碼"
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-200 字元
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// SKU 銷售價格
    /// 
    /// 用途：
    /// - 設定該規格的價格
    /// - 可覆蓋商品基礎價格
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 建議使用 2 位小數
    /// 
    /// 注意事項：
    /// - 不同規格可以有不同的價格
    /// - 價格變更會影響訂單計算
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// 實際庫存數量
    /// 
    /// 用途：
    /// - 記錄該規格的可用庫存
    /// - 用於庫存管理和訂單處理
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// 
    /// 注意事項：
    /// - 庫存不足時應禁止下單
    /// - 建議實作庫存預占機制
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// 是否為預設 SKU
    /// 
    /// 用途：
    /// - 標識商品頁面預先顯示的 SKU
    /// - 簡化使用者選擇流程
    /// 
    /// 約束：
    /// - 每個商品只能有一個預設 SKU
    /// - 由業務邏輯確保唯一性
    /// 
    /// 使用場景：
    /// - 商品頁面首次載入時顯示的規格
    /// - 搜尋結果中顯示的預設價格
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// SKU 專屬圖片 URL
    /// 
    /// 用途：
    /// - 顯示該規格的專屬圖片
    /// - 未設定時使用商品主圖
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// - 建議圖片尺寸：800x800 像素
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// SKU 狀態
    /// 
    /// 用途：
    /// - 控制該規格是否可見和可購買
    /// 
    /// 可選值：
    /// - "active"：啟用（可見且可購買）
    /// - "inactive"：停用（不可見且不可購買）
    /// 
    /// 預設值：
    /// - "active"（通常情況）
    /// 
    /// 使用場景：
    /// - 暫時停用某個規格（而非刪除）
    /// - 預先建立規格但尚未啟用
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// SKU 規格陣列
    /// 
    /// 用途：
    /// - 定義該 SKU 的規格參數
    /// - 直接賦值給 Sku.Specs
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 處理方式：
    /// - 直接賦值給 Sku.Specs
    /// - 不區分銷售屬性和非銷售屬性
    /// - 如果為 null，則 Sku.Specs 為空集合
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
    /// 計量單位 ID
    /// 
    /// 用途：
    /// - 標識該規格的計量單位（如：個、件、箱等）
    /// - 用於庫存管理和訂單處理
    /// 
    /// 驗證規則：
    /// - 必須對應資料庫中存在的計量單位 ID
    /// 
    /// 範例：
    /// - 1：個
    /// - 2：件
    /// - 3：箱
    /// </summary>
    public int UnitOfMeasureId { get; set; }
}



/// <summary>
/// 新增 SKU 命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SkuAddCommand 命令
/// - 建立新的 Sku 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Sku 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SkuAddCommand, Sku> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// </summary>
internal class SkuAddHandler : IRequestHandler<SkuAddCommand, Sku>
{
    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/ProductRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 擴展了 AddSku、GetSkuAsync 等方法
    /// </summary>
    private readonly IProductRepository _productRepository;

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
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於新增 SKU</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生 SKU ID</param>
    public SkuAddHandler(
        IProductRepository productRepository,
        IUniqueIdentifier uniqueIdentifier)
    {
        _productRepository = productRepository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增 SKU 命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證商品是否存在
    /// 2. 建立新的 Sku 實體
    /// 3. 設定實體屬性
    /// 4. 處理 Specifications（直接賦值）
    /// 5. 將實體加入倉儲
    /// 6. 儲存變更到資料庫
    /// 7. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 商品不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - Specifications 陣列會直接賦值給 Sku.Specs
    /// - 不區分銷售屬性和非銷售屬性
    /// - 如果 Specifications 為 null，則 Sku.Specs 為空集合
    /// </summary>
    /// <param name="request">新增 SKU 命令物件，包含 SKU 的所有資訊</param>
    /// <returns>儲存後的 Sku 實體，包含資料庫自動生成的欄位</returns>
    public async Task<Sku> HandleAsync(SkuAddCommand request)
    {
        // ========== 第一步：驗證商品是否存在 ==========
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            throw Failure.NotFound($"商品不存在，ID: {request.ProductId}");

        // 產生 SKU 編碼（格式：{SPU編碼}-{序號}）
        var skuCode = await generateSkuCode(product);

        // ========== 第二步：建立新的 Sku 實體 ==========
        var sku = new Sku
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            ProductId = request.ProductId,
            Name = request.Name,
            SkuCode = skuCode,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsDefault = request.IsDefault,
            ImageUrl = request.ImageUrl,
            Status = request.Status,
            UnitOfMeasureId = request.UnitOfMeasureId,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第三步：處理 Specifications ==========
        // 直接檢查是否為空陣列，如果不是，直接賦值給 Sku.Specs
        if (request.Specifications != null && request.Specifications.Length > 0)
        {
            sku.Specs = request.Specifications.ToList();
        }
        else
        {
            // 如果 Specifications 為 null 或空陣列，則設定為空集合
            sku.Specs = new List<Specification>();
        }

        // ========== 第四步：將實體加入倉儲 ==========
        // 使用 IProductRepository.AddSku() 新增 SKU
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _productRepository.AddSku(sku);

        // ========== 第五步：儲存變更到資料庫 ==========
        // 使用 IProductRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會執行 INSERT SQL 語句，並自動生成 ID
        await _productRepository.SaveChangeAsync();

        // ========== 第六步：回傳儲存後的實體 ==========
        return sku;
    }


    /// <summary>
    /// 產生下一個 SKU 編碼
    /// 
    /// 格式：{SPU編碼}-{序號}（三位數，不足補零）
    /// 範例：SPU001-001, SPU001-002
    /// 
    /// 流程：
    /// 1. 查詢該商品下編碼最大的 SKU
    /// 2. 若無 SKU，從 001 開始
    /// 3. 若有 SKU，序號加 1
    /// 
    /// 注意：
    /// - 使用 ProductRepository.GetSkusAsync() 查詢
    /// - 序號格式化為三位數（D3）
    /// - 並發新增可能導致編碼重複
    /// </summary>
    /// <param name="product">商品實體，包含 SPU 編碼</param>
    /// <returns>產生的 SKU 編碼</returns>
    private async Task<string> generateSkuCode(Product product)
    {
        // 查詢該商品的所有 SKU
        var skus = await _productRepository.GetSkusByProductIdAsync(product.Id);

        // 取得編碼最大的 SKU
        var lastSku = skus.OrderByDescending(s => s.SkuCode).FirstOrDefault();
        
        // 若無 SKU，從 001 開始
        if (lastSku == null)
        {
            return $"{product.SpuCode}-001";
        }
        
        // 解析最後一個 SKU 的序號並加 1
        var lastNumber = int.Parse(lastSku.SkuCode.Split('-').Last());
        var nextNumber = lastNumber + 1;

        // 格式化為三位數並回傳
        return $"{product.SpuCode}-{nextNumber:D3}";
    }

}
