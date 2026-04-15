using Manian.Application.Commands.Assets;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Domain.ValueObjects;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Products;

/// <summary>
/// 更新商品命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝更新商品所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Product>，表示這是一個會回傳 Product 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ProductUpdateHandler 配合使用，完成更新商品的業務邏輯
/// 
/// 使用場景：
/// - 管理員修改商品資訊
/// - 商品資料維護
/// - 商品結構調整
/// 
/// 設計特點：
/// - 所有屬性皆為可空（nullable），支援部分更新
/// - 未提供的欄位保持原值不變
/// - 遵循 HTTP PATCH 語意（部分更新）
/// - 與 BrandUpdateCommand 類似的設計模式
/// 
/// 注意事項：
/// - Specifications 陣列會完全替換現有規格
/// - 更新商品不會影響已存在的 SKU
/// - 如果需要更新 SKU，應該使用專門的 SKU 更新命令
/// </summary>
public class ProductUpdateCommand : IRequest<Product>
{
    /// <summary>
    /// 商品唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的商品
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
    
    /// <summary>
    /// 商品名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的商品名稱
    /// - 用於商品搜尋和篩選
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-200 字元
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 商品編碼 (SPU Code)
    /// 
    /// 用途：
    /// - 標準產品單元 (Standard Product Unit) 的唯一識別碼
    /// - 用於庫存管理和訂單系統
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：10-5000 字元
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 商品主圖 URL
    /// 
    /// 用途：
    /// - 商品列表和主展示的圖片
    /// - 用於吸引使用者點擊
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// - 建議圖片尺寸：800x800 像素
    /// </summary>
    public string? MainImageUrl { get; set; }
    
    /// <summary>
    /// 商品詳情圖片 URL 列表
    /// 
    /// 用途：
    /// - 商品詳情頁展示的圖片
    /// - 用於展示商品細節
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須是有效的 URL 格式
    /// - 建議視頻格式：MP4
    /// </summary>
    public string? VideoUrl { get; set; }
    
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 商品所屬分類 ID
    /// 
    /// 用途：
    /// - 將商品歸類到特定分類
    /// - 用於商品篩選和導航
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須對應資料庫中存在的品牌 ID
    /// </summary>
    public int? BrandId { get; set; }
    
    /// <summary>
    /// 商品規格陣列
    /// 
    /// 用途：
    /// - 定義商品的規格參數
    /// - 根據 AttributeKey.ForSales 屬性決定加入 Product.Specs 還是 Sku.Specs
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，完全替換現有規格
    /// 
    /// 規格分類：
    /// - 銷售屬性（ForSales = true）：加入 Sku.Specs
    ///   - 範例：顏色、尺寸、容量
    /// - 非銷售屬性（ForSales = false）：加入 Product.Specs
    ///   - 範例：材質、產地、重量
    /// 
    /// 使用範例：
    /// <code>
    /// new Specification[] {
    ///     new Specification { KeyId = 1, ValueId = 100, Name = "顏色", Value = "紅色" },
    ///     new Specification { KeyId = 2, ValueId = 200, Name = "尺寸", Value = "XL" },
    ///     new Specification { KeyId = 3, ValueId = 300, Name = "材質", Value = "棉" }
    /// }
    /// </code>
    /// </summary>
    public Specification[] Specifications { get; set; }

    /// <summary>
    /// 商品標籤列表
    /// 
    /// 用途：
    /// - 標識商品的特點或類型
    /// - 用於商品篩選和行銷
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，完全替換現有標籤
    /// 
    /// 使用範例：
    /// - ["新品", "熱銷", "限時優惠"]
    /// - ["手工製作", "環保材質", "台灣製"]
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// 商品 SKU 的預設價格
    /// 
    /// 用途：
    /// - 設定商品的基本價格
    /// - 當創建商品時，會同時創建一個預設的 SKU
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 建議使用 2 位小數
    /// 
    /// 注意事項：
    /// - SKU 可以有自己的價格，覆蓋商品的基本價格
    /// </summary>
    public decimal? Price { get; set; }
}

/// <summary>
/// 更新商品命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ProductUpdateCommand 命令
/// - 查詢商品是否存在
/// - 處理商品規格的分類邏輯（Product.Specs vs Sku.Specs）
/// - 更新商品資訊
/// - 回傳更新後的 Product 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ProductUpdateCommand, Product> 介面
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
/// - BrandUpdateHandler：類似的更新邏輯
/// - CategoryUpdateHandler：類似的更新邏輯
/// </summary>
internal class ProductUpdateHandler : IRequestHandler<ProductUpdateCommand, Product>
{
    /// <summary>
    /// 商品倉儲，用於查詢和更新商品資料
    /// </summary>
    private readonly IProductRepository _productRepository;

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
    /// <param name="productRepository">商品倉儲，用於查詢和更新商品資料</param>
    /// <param name="mediator">中介者服務</param>
    public ProductUpdateHandler(IProductRepository productRepository, IMediator mediator)
    {
        _productRepository = productRepository;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理更新商品命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢商品實體
    /// 2. 驗證商品是否存在
    /// 3. 記錄原始 SpuCode
    /// 4. 更新商品屬性（只更新非 null 的欄位）
    /// 5. 處理 SpuCode 更新（如果變更，同步更新所有關聯 SKU 的 SkuCode）
    /// 6. 處理 Specifications（如果提供則完全替換）
    /// 7. 儲存變更到資料庫
    /// 8. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - 商品不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新 SpuCode 會自動更新所有關聯 SKU 的 SkuCode
    /// - SkuCode 格式：{SpuCode}-{序號}（三位數，不足補零）
    /// - 所有屬性都是可選的，只更新提供的屬性
    /// - Specifications 陣列會完全替換 Product.Specs
    /// </summary>
    /// <param name="request">更新商品命令物件，包含商品 ID 和要更新的欄位</param>
    /// <returns>更新後的 Product 實體</returns>
    public async Task<Product> HandleAsync(ProductUpdateCommand request)
    {
        // ========== 第一步：查詢商品實體 ==========
        // 使用 IProductRepository.GetByIdAsync() 查詢商品
        // 這個方法會從資料庫中取得完整的商品實體
        // 如果商品不存在，會拋出 Failure.NotFound() 例外
        var product = await _productRepository.GetByIdAsync(request.Id);
        if (product == null)
            throw Failure.NotFound($"商品不存在，ID: {request.Id}");

        // ========== 第二步：記錄原始 SpuCode ==========
        // 記錄更新前的 SpuCode，用於後續比較
        // 如果 SpuCode 發生變更，需要同步更新所有關聯 SKU 的 SkuCode
        var oldSpuCode = product.SpuCode;

        // 初始化需要刪除的照片 URL 列表
        var urlsToDelete = new List<string>();

        // ========== 第三步：更新商品屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        // 每個屬性都是可選的，前端可以只傳送需要更新的欄位

        // 更新基本屬性
        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        
        // 更新主圖
        if (request.MainImageUrl != null)
        {
            // 如果商品原本有主圖，將舊主圖加入刪除列表
            if(product.MainImageUrl != null)
                urlsToDelete.Add(product.MainImageUrl);

            // 更新為新主圖
            product.MainImageUrl = request.MainImageUrl;   
        }
        
        // 更新詳情圖片
        if (request.DetailImages != null) 
        {
            // 準備新的詳情圖片列表
            var newDetailImages = request.DetailImages;
            
            // 找出舊有但新請求中沒有的照片
            var detailImagesToDelete = product.DetailImages
                .Except(newDetailImages)
                .ToList();
            
            // 將需要刪除的詳情圖片加入列表
            urlsToDelete.AddRange(detailImagesToDelete);

            // 更新詳情圖片
            product.DetailImages = newDetailImages;
        }
        
        // 更新視頻
        if (request.VideoUrl != null) product.VideoUrl = request.VideoUrl;

        // 更新關聯屬性
        if (request.CategoryId != null) product.CategoryId = request.CategoryId;
        if (request.BrandId != null) product.BrandId = request.BrandId;

        // 更新狀態屬性
        if (request.Status != null) product.Status = request.Status;

        // 更新價格屬性
        if (request.Price != null) product.Price = request.Price.Value;

        // 更新標籤屬性
        if (request.Tags != null) product.Tags = request.Tags;

        // ========== 第四步：處理 SpuCode 更新 ==========
        if (request.SpuCode != null && request.SpuCode != oldSpuCode)
        {
            // 更新商品的 SpuCode
            product.SpuCode = request.SpuCode;

            // 查詢該商品的所有 SKU
            var skus = await _productRepository.GetSkusByProductIdAsync(product.Id);

            // 更新每個 SKU 的 SkuCode
            foreach (var sku in skus)
            {
                // 解析原始 SkuCode 的序號部分
                var parts = sku.SkuCode.Split('-');
                var suffix = parts.Length > 1 ? parts.Last() : "001";

                // 使用新的 SpuCode 重建 SkuCode
                sku.SkuCode = $"{request.SpuCode}-{suffix}";
            }
        }

        // ========== 第五步：處理 Specifications ==========
        // 如果提供了 Specifications 陣列，需要處理規格更新邏輯
        // 規格分類：
        // - 銷售屬性（ForSales = true）：加入 Sku.Specs
        //   - 範例：顏色、尺寸、容量
        // - 非銷售屬性（ForSales = false）：加入 Product.Specs
        //   - 範例：材質、產地、重量
        // 
        // 注意事項：
        // - Specifications 陣列會完全替換現有規格
        // - 可以參考 ProductAddCommand 中的處理方式
        // - 需要查詢每個 Specification 對應的 AttributeKey.ForSales 屬性
        if (request.Specifications != null)
        {
            // 直接賦值給 product.Specs
            // Specifications 陣列會完全替換現有規格
            // 不區分銷售屬性和非銷售屬性
            // 如果 Specifications 為空陣列，則 product.Specs 為空集合
            product.Specs = request.Specifications.ToList();
        }

        // ========== 第六步：儲存變更 ==========
        // 使用 IProductRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        // 包括：
        // - 商品實體的屬性變更
        // - SKU 實體的 SkuCode 變更（如果 SpuCode 變更）
        // - 其他關聯實體的變更
        await _productRepository.SaveChangeAsync();

        // ========== 第七步：刪除不再需要的照片 ==========
        if (urlsToDelete.Any())
        {
            // 使用 Mediator 發送 AssetDeleteCommand
            // 這會同時刪除資料庫記錄和 S3 檔案
            await _mediator.SendAsync(new AssetDeleteCommand
            {
                Urls = urlsToDelete.ToArray()
            });
        }

        // ========== 第八步：回傳更新後的實體 ==========
        // 回傳更新後的 Product 實體
        // 包含所有更新後的屬性值
        // 呼叫者可以使用這個實體進行後續操作
        return product;
    }
}
