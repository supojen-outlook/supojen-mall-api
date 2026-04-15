using Manian.Application.Commands.Products;
using Manian.Application.Models.Products;
using Manian.Application.Queries.Products;
using Manian.Domain.Entities.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Products;

/// <summary>
/// 商品 API 端點
/// 
/// 職責：
/// - 定義商品相關的 API 端點
/// - 處理商品的查詢、新增、更新、刪除請求
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// - GET /api/products - 查詢商品列表
/// - GET /api/products/{id} - 查詢單一商品
/// - GET /api/products/count - 查詢商品總數
/// - POST /api/products - 新增商品
/// - PUT /api/products/{id} - 更新商品
/// - DELETE /api/products/{id} - 刪除商品
/// 
/// Scalar 文件：
/// - 使用 Scalar 替代 Swagger UI
/// - 提供更現代化的 API 文件介面
/// - 支援深色模式和更好的互動體驗
/// </summary>
public static class ProductEndpoint
{
    /// <summary>
    /// 註冊所有商品相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapProducts() 即可註冊所有端點
    /// 
    /// 設計考量：
    /// - 使用擴充方法讓端點註冊更模組化
    /// - 每個端點對應一個處理方法，保持單一職責
    /// - 端點命名遵循 RESTful 規範
    /// 
    /// Scalar 整合：
    /// - 端點會自動出現在 Scalar 文件中
    /// - 使用 WithSummary() 和 WithDescription() 提供詳細說明
    /// - 使用 WithTags() 進行分組顯示
    /// </summary>
    /// <param name="app">IEndpointRouteBuilder 實例，用於註冊端點</param>
    public static void MapProducts(this IEndpointRouteBuilder app)
    {
        // =========================================================================
        // GET /api/products - 查詢商品列表
        // =========================================================================
        app.MapGet("/api/products", GetProductsAsync)
            .WithName("GetProducts")
            .WithSummary("查詢商品列表")
            .WithDescription(@"
                查詢系統中的商品列表，支援分頁和篩選
                
                認證要求：
                - 不需要登入（公開端點）
                
                查詢參數：
                - search：搜尋關鍵字（可選），會在 Name、Description 中搜尋
                - categoryId：類別 ID（可選），篩選指定類別的商品
                - brandId：品牌 ID（可選），篩選指定品牌的商品
                - status：商品狀態（可選），篩選指定狀態的商品
                - lastCreatedAt：上一頁最後一筆資料的建立時間（可選），用於分頁
                - size：每頁資料筆數（可選），預設 1000
                
                回傳格式：
                - 200 OK：商品列表（ProductResponse 陣列）
                
                回傳欄位說明：
                - Id：商品 ID
                - SpuCode：商品編碼
                - Name：商品名稱
                - Price：商品價格
                - CategoryId：類別 ID
                - BrandId：品牌 ID
                - MainImageUrl：主圖 URL
                - Tags：標籤陣列
                - Status：商品狀態
                - CreatedAt：建立時間
                
                使用範例：
                - GET /api/products
                - GET /api/products?search=手機
                - GET /api/products?categoryId=5&status=active
                - GET /api/products?size=50
                
                注意事項：
                - 商品列表不包含詳細資訊（Description、DetailImages、VideoUrl、Specs）
                - 如需詳細資訊，請使用 GET /api/products/{id}
                - 分頁使用 lastCreatedAt 參數，而非傳統的 page 參數
            ")
            .WithTags("商品")
            .AllowAnonymous()
            .Produces<IEnumerable<ProductBase>>(StatusCodes.Status200OK);

        // =========================================================================
        // GET /api/products/{id} - 查詢單一商品
        // =========================================================================
        app.MapGet("/api/products/{id}", GetProductAsync)
            .WithName("GetProduct")
            .WithSummary("查詢單一商品")
            .WithDescription(@"
                查詢指定 ID 的商品詳細資訊
                
                認證要求：
                - 不需要登入（公開端點）
                
                路徑參數：
                - id：商品 ID（必填）
                
                回傳格式：
                - 200 OK：商品詳細資訊（ProductResponse）
                - 404 Not Found：商品不存在
                
                回傳欄位說明：
                - Id：商品 ID
                - SpuCode：商品編碼
                - Name：商品名稱
                - Description：商品描述
                - MainImageUrl：主圖 URL
                - DetailImages：詳情圖片 URL 陣列
                - VideoUrl：視頻 URL
                - Price：商品價格
                - CategoryId：類別 ID
                - BrandId：品牌 ID
                - Tags：標籤陣列
                - Status：商品狀態
                - Specs：規格陣列（Specification 物件）
                - CreatedAt：建立時間
                
                使用範例：
                - GET /api/products/123
                
                注意事項：
                - 包含商品的所有詳細資訊
                - Specs 陣列包含所有規格（銷售屬性和非銷售屬性）
                - 如果商品不存在，會回傳 404 Not Found
            ")
            .WithTags("商品")
            .AllowAnonymous()
            .Produces<Product>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // GET /api/products/count - 查詢商品總數
        // =========================================================================
        app.MapGet("/api/products/count", GetProductCountAsync)
            .WithName("GetProductCount")
            .WithSummary("查詢商品總數")
            .WithDescription(@"
                查詢系統中的商品總數
                
                認證要求：
                - 不需要登入（公開端點）
                
                回傳格式：
                - 200 OK：商品總數（整數）
                
                使用範例：
                - GET /api/products/count
                
                注意事項：
                - 使用 EstimatedCount() 方法，可能不精確但效能較好
                - 如果需要精確數量，請改用 CountAsync() 方法
            ")
            .WithTags("商品")
            .AllowAnonymous()
            .Produces<int>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/products - 新增商品
        // =========================================================================
        app.MapPost("/api/products", AddProductAsync)
            .WithName("AddProduct")
            .WithSummary("新增商品")
            .WithDescription(@"
                新增一個商品到系統中
                
                認證要求：
                - 需要登入（通過 Cookie）
                - 需要管理員權限
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含商品的所有資訊
                
                必填欄位：
                - Name：商品名稱
                - Description：商品描述
                - MainImageUrl：主圖 URL
                - Price：商品價格
                - UnitOfMeasureId：計量單位 ID
                - Status：商品狀態
                - Tags：標籤陣列
                
                可選欄位：
                - SpuCode：商品編碼（未提供則自動生成）
                - DetailImages：詳情圖片 URL 陣列
                - VideoUrl：視頻 URL
                - CategoryId：類別 ID
                - BrandId：品牌 ID
                - Specifications：規格陣列
                
                回傳格式：
                - 200 OK：新增成功的商品（Product 實體）
                - 401 Unauthorized：未登入
                - 403 Forbidden：權限不足
                
                使用範例：
                POST /api/products
                {
                    ""Name"": ""iPhone 15 Pro"",
                    ""Description"": ""蘋果最新旗艦手機"",
                    ""MainImageUrl"": ""https://example.com/iphone15.jpg"",
                    ""Price"": 39900,
                    ""UnitOfMeasureId"": 1,
                    ""Status"": ""active"",
                    ""Tags"": [""新品"", ""熱銷""],
                    ""CategoryId"": 5,
                    ""BrandId"": 10
                }
                
                注意事項：
                - 新增商品時會同時建立一個預設的 SKU
                - Specifications 陣列中的每個 Specification 會根據 KeyId 對應的 AttributeKey.ForSales 屬性
                  決定是加入 Product.Specs 還是 Sku.Specs
                - SpuCode 如果未提供，會自動使用商品 ID
                - 建議使用 HTTPS 傳輸（防止資料被截取）
            ")
            .WithTags("商品")
            .Produces<Product>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // =========================================================================
        // PUT /api/products/{id} - 更新商品
        // =========================================================================
        app.MapPut("/api/products", UpdateProductAsync)
            .WithName("UpdateProduct")
            .WithSummary("更新商品")
            .WithDescription(@"
                更新指定 ID 的商品資訊
                
                認證要求：
                - 需要登入（通過 Cookie）
                - 需要管理員權限
                
                路徑參數：
                - id：商品 ID（必填）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含要更新的欄位
                
                必填欄位：
                - 無（所有欄位皆為可選）
                
                可選欄位：
                - Name：商品名稱
                - SpuCode：商品編碼
                - Description：商品描述
                - MainImageUrl：主圖 URL
                - DetailImages：詳情圖片 URL 陣列
                - VideoUrl：視頻 URL
                - CategoryId：類別 ID
                - BrandId：品牌 ID
                - Price：商品價格
                - Status：商品狀態
                - Tags：標籤陣列
                - Specifications：規格陣列
                
                回傳格式：
                - 200 OK：更新後的商品（Product 實體）
                - 401 Unauthorized：未登入
                - 403 Forbidden：權限不足
                - 404 Not Found：商品不存在
                
                使用範例：
                PUT /api/products/123
                {
                    ""Name"": ""iPhone 15 Pro Max"",
                    ""Price"": 43900
                }
                
                注意事項：
                - 只更新提供的欄位，未提供的欄位保持不變
                - 如果更新 SpuCode，會同時更新所有關聯 SKU 的 SkuCode
                - Specifications 陣列會完全替換現有規格
                - 建議使用 HTTPS 傳輸（防止資料被截取）
            ")
            .WithTags("商品")
            .Produces<Product>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // DELETE /api/products/{id} - 刪除商品
        // =========================================================================
        app.MapDelete("/api/products", DeleteProductAsync)
            .WithName("DeleteProduct")
            .WithSummary("刪除商品")
            .WithDescription(@"
                刪除指定 ID 的商品
                
                認證要求：
                - 需要登入（通過 Cookie）
                - 需要管理員權限
                
                路徑參數：
                - id：商品 ID（必填）
                
                回傳格式：
                - 200 OK：刪除成功
                - 401 Unauthorized：未登入
                - 403 Forbidden：權限不足
                - 404 Not Found：商品不存在
                
                使用範例：
                - DELETE /api/products/123
                
                注意事項：
                - 刪除操作不可逆，建議在 UI 層加入確認對話框
                - 刪除商品會一併刪除所有關聯的 SKU（由資料庫級聯刪除保證）
                - 建議檢查是否有訂單使用此商品
                - 建議檢查是否有庫存記錄
            ")
            .WithTags("商品")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// 查詢商品列表
    /// 
    /// 請求方式：GET /api/products
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的 ProductResponse 陣列
    /// 
    /// 執行流程：
    /// 1. 接收查詢參數（search、categoryId、brandId、status、lastCreatedAt、size）
    /// 2. 使用 Mediator 發送 ProductsQuery 查詢
    /// 3. 回傳商品列表
    /// 
    /// 錯誤處理：
    /// - 無錯誤（總是回傳 200 OK，即使沒有資料）
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">ProductsQuery 查詢物件（包含搜尋和分頁參數）</param>
    /// <returns>包含商品列表的 JSON 回應</returns>
    private static async Task<IResult> GetProductsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ProductsQuery query)
    {
        // 使用 Mediator 發送 ProductsQuery 查詢
        // Mediator 會自動找到對應的 Handler (ProductsQueryHandler)
        // Handler 會從資料庫查詢商品列表並映射為 ProductResponse 陣列
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和商品列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢單一商品
    /// 
    /// 請求方式：GET /api/products/{id}
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的 ProductResponse
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（id）
    /// 2. 使用 Mediator 發送 ProductQuery 查詢
    /// 3. 回傳商品詳細資訊
    /// 
    /// 錯誤處理：
    /// - 商品不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">ProductQuery 查詢物件（包含商品 ID）</param>
    /// <returns>包含商品詳細資訊的 JSON 回應</returns>
    private static async Task<IResult> GetProductAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ProductQuery query)
    {
        // 使用 Mediator 發送 ProductQuery 查詢
        // Mediator 會自動找到對應的 Handler (ProductQueryHandler)
        // Handler 會從資料庫查詢商品詳細資訊並映射為 ProductResponse
        var result = await mediator.SendAsync(query);
        
        // 如果查詢結果為 null，回傳 404 Not Found
        if (result == null)
            return Results.NotFound();
        
        // 回傳 200 OK 狀態碼和商品詳細資訊
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢商品總數
    /// 
    /// 請求方式：GET /api/products/count
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的整數
    /// 
    /// 執行流程：
    /// 1. 使用 Mediator 發送 ProductCountQuery 查詢
    /// 2. 回傳商品總數
    /// 
    /// 錯誤處理：
    /// - 無錯誤（總是回傳 200 OK）
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">ProductCountQuery 查詢物件（不包含任何參數）</param>
    /// <returns>包含商品總數的 JSON 回應</returns>
    private static async Task<IResult> GetProductCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ProductCountQuery query)
    {
        // 使用 Mediator 發送 ProductCountQuery 查詢
        // Mediator 會自動找到對應的 Handler (ProductCountQueryHandler)
        // Handler 會從資料庫查詢商品總數
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和商品總數
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增商品
    /// 
    /// 請求方式：POST /api/products
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：JSON 格式的 ProductAddCommand
    /// 回應格式：JSON 格式的 Product 實體
    /// 
    /// 執行流程：
    /// 1. 接收請求主體（ProductAddCommand）
    /// 2. 使用 Mediator 發送 ProductAddCommand 命令
    /// 3. 回傳新增後的商品
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 權限不足：回傳 403 Forbidden
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="command">ProductAddCommand 命令物件（包含商品的所有資訊）</param>
    /// <returns>包含新增後的商品的 JSON 回應</returns>
    private static async Task<IResult> AddProductAsync(
        [FromServices] IMediator mediator,
        [FromBody] ProductAddCommand command)
    {
        // 使用 Mediator 發送 ProductAddCommand 命令
        // Mediator 會自動找到對應的 Handler (ProductAddHandler)
        // Handler 會建立新的商品實體並儲存到資料庫
        var result = await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼和新增後的商品
        return Results.Ok(result);
    }

    /// <summary>
    /// 更新商品
    /// 
    /// 請求方式：PUT /api/products/{id}
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：JSON 格式的 ProductUpdateCommand
    /// 回應格式：JSON 格式的 Product 實體
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（id）和請求主體（ProductUpdateCommand）
    /// 2. 使用 Mediator 發送 ProductUpdateCommand 命令
    /// 3. 回傳更新後的商品
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 權限不足：回傳 403 Forbidden
    /// - 商品不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="id">商品 ID（路徑參數）</param>
    /// <param name="command">ProductUpdateCommand 命令物件（包含要更新的欄位）</param>
    /// <returns>包含更新後的商品的 JSON 回應</returns>
    private static async Task<IResult> UpdateProductAsync(
        [FromServices] IMediator mediator,
        [FromBody] ProductUpdateCommand command)
    {       
        // 使用 Mediator 發送 ProductUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (ProductUpdateHandler)
        // Handler 會更新商品實體並儲存到資料庫
        var result = await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼和更新後的商品
        return Results.Ok(result);
    }

    /// <summary>
    /// 刪除商品
    /// 
    /// 請求方式：DELETE /api/products/{id}
    /// 認證要求：需要登入（通過 Cookie）
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（id）
    /// 2. 使用 Mediator 發送 ProductDeleteCommand 命令
    /// 3. 回傳 200 OK
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 權限不足：回傳 403 Forbidden
    /// - 商品不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="command"> ProductDeleteCommand 命令物件（包含商品 ID）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> DeleteProductAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ProductDeleteCommand command)
    {   
        // 使用 Mediator 發送 ProductDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (ProductDeleteHandler)
        // Handler 會刪除商品實體並儲存變更到資料庫
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }
}
