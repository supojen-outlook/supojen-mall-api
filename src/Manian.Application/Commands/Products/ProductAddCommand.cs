using Manian.Application.Commands.Assets;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Manian.Domain.ValueObjects;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 新增商品命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增商品所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Product>，表示這是一個會回傳 Product 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ProductAddHandler 配合使用，完成新增商品的業務邏輯
/// 
/// 使用場景：
/// - 管理員新增商品
/// - 系統批量導入商品
/// - API 端點接收商品新增請求
/// 
/// 設計特點：
/// - 包含商品基本資訊（名稱、描述、圖片等）
/// - 包含商品關聯資訊（分類、品牌、屬性等）
/// - 包含商品規格資訊（Specifications 陣列）
/// - 支援可選屬性（如 VideoUrl、CategoryId 等）
/// 
/// 注意事項：
/// - Specifications 陣列中的所有規格目前統一存入 Product.Specs (非銷售屬性)
/// - 預設 SKU 的建立邏輯取決於所屬分類是否定義了銷售屬性
/// </summary>
public class ProductAddCommand : IRequest<Product>
{
    /// <summary>
    /// 商品名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的商品名稱
    /// - 用於商品搜尋和篩選
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-200 字元
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 商品編碼 (SPU Code)
    /// 
    /// 用途：
    /// - 標準產品單元 (Standard Product Unit) 的唯一識別碼
    /// - 用於庫存管理和訂單系統
    /// 
    /// 預設值：
    /// - 如果未提供，會自動使用商品 ID
    /// 
    /// 約束：
    /// - 必須唯一（由資料庫唯一約束保證）
    /// </summary>
    public string? SpuCode { get; set; }

    /// <summary>
    /// 商品詳細描述
    /// 
    /// 用途：
    /// - 提供商品的詳細資訊
    /// - 用於 SEO 優化
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：10-5000 字元
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 商品主圖 URL
    /// 
    /// 用途：
    /// - 商品列表和主展示的圖片
    /// - 用於吸引使用者點擊
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// - 建議圖片尺寸：800x800 像素
    /// </summary>
    public string MainImageUrl { get; set; }

    /// <summary>
    /// 商品詳情圖片 URL 列表
    /// 
    /// 用途：
    /// - 商品詳情頁展示的圖片
    /// - 用於展示商品細節
    /// 
    /// 預設值：
    /// - 空陣列（如果未提供）
    /// 
    /// 驗證規則：
    /// - 每個 URL 必須是有效的 URL 格式
    /// - 建議最多 10 張圖片
    /// </summary>
    public string[]? DetailImages { get; set; }
    
    /// <summary>
    /// 商品視頻 URL
    /// 
    /// 用途：
    /// - 展示商品的使用方式或細節
    /// - 提升使用者購買意願
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// - 建議視頻格式：MP4
    /// </summary>
    public string? VideoUrl { get; set; }
    
    /// <summary>
    /// 商品所屬分類 ID
    /// 
    /// 用途：
    /// - 將商品歸類到特定分類
    /// - 用於商品篩選和導航
    /// - 決定是否需要建立預設 SKU 的關鍵依據
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 必須對應資料庫中存在的分類 ID
    /// </summary>
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// 商品所屬品牌 ID
    /// 
    /// 用途：
    /// - 將商品關聯到特定品牌
    /// - 用於品牌篩選和導航
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 必須對應資料庫中存在的品牌 ID
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// 商品狀態
    /// 
    /// 用途：
    /// - 控制商品是否可見和可購買
    /// 
    /// 可選值：
    /// - "active"：上架（可見且可購買）
    /// - "inactive"：下架（不可見且不可購買）
    /// - "draft"：草稿（不可見且不可購買）
    /// 
    /// 預設值：
    /// - "active"（通常情況）
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 商品 SKU 的預設價格
    /// 
    /// 用途：
    /// - 設定商品的基本價格
    /// - 當創建商品時，若建立預設 SKU，該 SKU 將繼承此價格
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 建議使用 2 位小數
    /// 
    /// 注意事項：
    /// - SKU 可以有自己的價格，覆蓋商品的基本價格
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 商品計量單位 ID
    /// 
    /// 用途：
    /// - 標識商品的計量單位（如：個、件、箱等）
    /// - 用於庫存管理和訂單處理
    /// 
    /// 驗證規則：
    /// - 必須對應資料庫中存在的計量單位 ID
    /// </summary>
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// 商品標籤列表
    /// 
    /// 用途：
    /// - 標識商品的特點或類型
    /// - 用於商品篩選和行銷
    /// 
    /// 預設值：
    /// - 空陣列（如果未提供）
    /// 
    /// 使用範例：
    /// - ["新品", "熱銷", "限時優惠"]
    /// - ["手工製作", "環保材質", "台灣製"]
    /// </summary>
    public string[] Tags { get; set; }

    /// <summary>
    /// 商品規格陣列
    /// 
    /// 用途：
    /// - 定義商品的規格參數
    /// - 目前實作中，所有規格皆視為非銷售屬性，存入 Product.Specs
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 規格分類：
    /// - 當前邏輯：所有規格統一存入 Product.Specs
    /// - 銷售屬性（如顏色、尺寸）將由分類配置決定，並在建立具體 SKU 時處理
    /// 
    /// 使用範例：
    /// <code>
    /// new Specification[] {
    ///     new Specification { KeyId = 1, ValueId = 100, Name = "材質", Value = "棉" },
    ///     new Specification { KeyId = 2, ValueId = 200, Name = "產地", Value = "台灣" }
    /// }
    /// </code>
    /// </summary>
    public List<Specification>? Specifications { get; set; }
}



/// <summary>
/// 新增商品命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ProductAddCommand 命令
/// - 建立 Product 實體
/// - 根據分類配置判斷並建立預設 Sku 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Product 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ProductAddCommand, Product> 介面
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
/// 
/// 參考實作：
/// - CategoryAddHandler：類似的新增邏輯
/// - BrandAddHandler：類似的新增邏輯
/// </summary>
internal class ProductAddHandler : IRequestHandler<ProductAddCommand, Product>
{
    /// <summary>
    /// 唯一識別碼服務
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
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性鍵資料
    /// - 查詢特定分類下的銷售屬性
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/AttributeKeyRepository.cs）
    /// - 提供 GetCategoryAttributesAsync 方法查詢分類屬性
    /// </summary>
    private readonly IAttributeKeyRepository _attributeKeyRepository;

    /// <summary>
    /// 中介者服務
    /// 
    /// 用途：
    /// - 傳遞命令和事件
    /// - 處理跨邊界邏輯
    /// 
    /// 實作方式：
    /// - 使用 MediatR 框架（見 Infrastructure/MediatR/MediatorExtensions.cs）
    /// - 提供 SendAsync 方法傳遞命令和事件
    /// </summary>
    private readonly IMediator _mediator;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生 ID</param>
    /// <param name="productRepository">商品倉儲，用於存取商品資料</param>
    /// <param name="attributeKeyRepository">屬性鍵倉儲，用於查詢分類屬性</param>
    /// <<param name="mediator">中介者服務，用於傳遞命令和事件</param>
    public ProductAddHandler(
        IUniqueIdentifier uniqueIdentifier,
        IProductRepository productRepository,
        IAttributeKeyRepository attributeKeyRepository,
        IMediator mediator)
    {
        _uniqueIdentifier = uniqueIdentifier;
        _productRepository = productRepository;
        _attributeKeyRepository = attributeKeyRepository;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理新增商品命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 產生商品 ID
    /// 2. 建立 Product 實體（所有規格存入 Product.Specs）
    /// 3. 判斷分類是否定義了銷售屬性
    /// 4. 若無銷售屬性，則建立預設的 Sku 實體
    /// 5. 將實體加入倉儲並儲存
    /// 6. 回傳儲存後的 Product 實體
    /// 
    /// 預設 SKU 邏輯：
    /// - 只有當所屬分類沒有定義任何銷售屬性（ForSales = true）時，才會建立預設 SKU
    /// - 預設 SKU 的 SkuCode 為 Product.SpuCode + "-001"
    /// - 預設 SKU 的 IsDefault 為 true
    /// 
    /// 錯誤處理：
    /// - 如果資料庫儲存失敗，會拋出例外
    /// 
    /// 注意事項：
    /// - 銷售屬性的判斷依賴於 CategoryId 的配置
    /// </summary>
    /// <param name="request">新增商品命令物件，包含商品的所有資訊</param>
    /// <returns>儲存後的 Product 實體，包含資料庫自動生成的欄位</returns>
    public async Task<Product> HandleAsync(ProductAddCommand request)
    {
        // ========== 第一步：產生商品 ID ==========
        // 使用唯一識別碼服務產生全域唯一的整數 ID
        // 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
        var id = _uniqueIdentifier.NextInt();

        // ========== 第二步：建立 Product 實體 ==========
        var product = new Product
        {
            // 使用前面產生的 ID
            Id = id,
            
            // 設定基本屬性
            CategoryId = request.CategoryId,           
            BrandId = request.BrandId,                 
            SpuCode = string.IsNullOrEmpty(request.SpuCode) ? $"{id}" : request.SpuCode,
            Name = request.Name,                       
            Description = request.Description,         
            Status = request.Status,                   
            MainImageUrl = request.MainImageUrl,       
            DetailImages = request.DetailImages ?? [], // 如果未提供，使用空陣列
            VideoUrl = request.VideoUrl,               
            CreatedAt = DateTimeOffset.UtcNow, // 設定建立時間為目前 UTC 時間
            
            // 設定規格（目前邏輯：所有規格皆存入 Product.Specs）
            Specs = request.Specifications != null ? request.Specifications : [],  
            
            // 設定價格和標籤
            Price = request.Price, 
            Tags = request.Tags ?? [], // 如果未提供，使用空陣列
        };

        // ========== 第三步：將 Product 實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _productRepository.Add(product);


        // ========== 第四步：判斷是否需要建立預設 SKU ==========
        // 預設為 false，表示假設分類沒有銷售屬性（需要建立預設 SKU）
        var hasSalesAttributes = false;
        
        if (request.CategoryId != null)
        {
            // 查詢該分類下是否定義了銷售屬性 (ForSales = true)
            var salesCategoryAttributes = await _attributeKeyRepository.GetCategoryAttributesAsync(
                request.CategoryId.Value,
                forSales: true
            );

            // 如果查詢結果不為空，表示該分類下有定義銷售屬性
            // 有銷售屬性通常意味著需要使用者手動生成 SKU，因此不需要在這裡建立預設 SKU
            if (salesCategoryAttributes != null && salesCategoryAttributes.Any())
            {
                hasSalesAttributes = true;
            }   
        }

        // ========== 第五步：建立 Sku 實體 ==========
        // 只有當 hasSalesAttributes 為 false 時（即分類沒有銷售屬性），才建立預設 SKU
        if (!hasSalesAttributes)
        {
            var sku = new Sku
            {
                // 產生新的 SKU ID
                Id = _uniqueIdentifier.NextInt(), 
                
                // 設定基本屬性
                Name = request.Name,
                ProductId = id,                     // 使用前面產生的 Product ID
                SkuCode = $"{product.SpuCode}-001", // 預設 SKU 編碼
                ImageUrl = request.MainImageUrl,    // 使用 Product 的主圖片
                Price = request.Price,              // 價格與 Product 一致
                ReservedStock = 0,                  // 預設預占庫存為 0
                IsDefault = true,                   // 標記為預設 SKU
                Status = "active",                  // 預設狀態為啟用
                CreatedAt = DateTimeOffset.UtcNow,  // 設定建立時間為目前 UTC 時間
                
                // 設定規格（預設 SKU 無銷售屬性規格）
                Specs = [],  
                
                // 設定計量單位
                UnitOfMeasureId = 1,
            };

            _productRepository.AddSku(sku);        
        }
    
        // ========== 第六步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 包括 Product 和 Sku 的新增操作
        await _productRepository.SaveChangeAsync();

        // ========== 第七步：更新資產庫 ==========
        // 將商品圖片更新到資產庫
        await _mediator.SendAsync(new AssetUpdateCommand()
        {
            Urls = [ request.MainImageUrl, ..request.DetailImages ?? [] ],
            TargetType = "product",
            TargetId = id
        });

        // ========== 第八步：回傳儲存後的 Product 實體 ==========
        return product;
    }
}
