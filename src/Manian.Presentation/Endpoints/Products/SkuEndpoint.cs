using Manian.Application.Commands.Products;
using Manian.Application.Queries.Products;
using Manian.Domain.Entities.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Products;

/// <summary>
/// SKU (Stock Keeping Unit) API 端點
/// 
/// 職責：
/// - 定義 SKU 相關的 API 端點
/// - 處理 SKU 的查詢、新增、更新、刪除請求
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// - GET /api/products/{productId}/skus - 查詢商品的所有 SKU
/// - GET /api/skus/{id} - 查詢單一 SKU
/// - POST /api/products/{productId}/skus - 新增 SKU
/// - PUT /api/skus/{id} - 更新 SKU
/// - DELETE /api/skus/{id} - 刪除 SKU
/// 
/// Scalar 文件：
/// - 使用 Scalar 替代 Swagger UI
/// - 提供更現代化的 API 文件介面
/// - 支援深色模式和更好的互動體驗
/// </summary>
public static class SkuEndpoint
{
    /// <summary>
    /// 註冊所有 SKU 相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapSkus() 即可註冊所有端點
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
    public static void MapSkus(this IEndpointRouteBuilder app)
    {
        // =========================================================================
        // GET /api/products/{productId}/skus - 查詢商品的所有 SKU
        // =========================================================================
        app.MapGet("/api/products/skus", GetSkusAsync)
            .WithName("GetSkus")
            .WithSummary("查詢商品的所有 SKU")
            .WithDescription(@"
                查詢指定商品的所有 SKU
                
                認證要求：
                - 不需要登入（公開端點）
                
                路徑參數：
                - productId：商品 ID（必填）
                
                回傳格式：
                - 200 OK：SKU 列表（Sku 陣列）
                - 404 Not Found：商品不存在
                
                回傳欄位說明：
                - Id：SKU ID
                - ProductId：商品 ID
                - Name：SKU 名稱
                - SkuCode：SKU 編碼
                - Price：SKU 價格
                - StockQuantity：庫存數量
                - IsDefault：是否為預設 SKU
                - ImageUrl：SKU 圖片 URL
                - Status：SKU 狀態
                - Specs：規格陣列（Specification 物件）
                - CreatedAt：建立時間
                
                使用範例：
                - GET /api/products/123/skus
                
                注意事項：
                - 如果商品沒有 SKU，會返回空陣列
                - Specs 陣列包含該 SKU 的所有規格
            ")
            .WithTags("SKU")
            .AllowAnonymous()
            .Produces<IEnumerable<Sku>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // POST /api/products/{productId}/skus - 新增 SKU
        // =========================================================================
        app.MapPost("/api/products/{productId}/skus", AddSkuAsync)
            .WithName("AddSku")
            .WithSummary("新增 SKU")
            .WithDescription(@"
                為指定商品新增一個 SKU
                
                認證要求：
                - 需要登入（通過 Cookie）
                - 需要管理員權限
                
                路徑參數：
                - productId：商品 ID（必填）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含 SKU 的所有資訊
                
                必填欄位：
                - Name：SKU 名稱
                - Price：SKU 價格
                - StockQuantity：庫存數量
                - Status：SKU 狀態
                - UnitOfMeasureId：計量單位 ID
                
                可選欄位：
                - IsDefault：是否為預設 SKU（預設 false）
                - ImageUrl：SKU 圖片 URL
                - Specifications：規格陣列
                
                回傳格式：
                - 200 OK：新增成功的 SKU（Sku 實體）
                - 401 Unauthorized：未登入
                - 403 Forbidden：權限不足
                - 404 Not Found：商品不存在
                
                使用範例：
                POST /api/products/123/skus
                {
                    ""Name"": ""iPhone 15 Pro 黑色 128G"",
                    ""Price"": 39900,
                    ""StockQuantity"": 100,
                    ""Status"": ""active"",
                    ""UnitOfMeasureId"": 1,
                    ""IsDefault"": false,
                    ""Specifications"": [
                        { ""KeyId"": 1, ""ValueId"": 100, ""Name"": ""顏色"", ""Value"": ""黑色"" },
                        { ""KeyId"": 2, ""ValueId"": 200, ""Name"": ""容量"", ""Value"": ""128G"" }
                    ]
                }
                
                注意事項：
                - SKU 編碼會自動生成（格式：{SPU編碼}-{序號}）
                - Specifications 陣列會直接賦值給 Sku.Specs
                - 建議使用 HTTPS 傳輸（防止資料被截取）
            ")
            .WithTags("SKU")
            .Produces<Sku>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // PUT /api/skus/{id} - 更新 SKU
        // =========================================================================
        app.MapPut("/api/skus", UpdateSkuAsync)
            .WithName("UpdateSku")
            .WithSummary("更新 SKU")
            .WithDescription(@"
                更新指定 ID 的 SKU 資訊
                
                認證要求：
                - 需要登入（通過 Cookie）
                - 需要管理員權限
                
                路徑參數：
                - id：SKU ID（必填）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含要更新的欄位
                
                必填欄位：
                - 無（所有欄位皆為可選）
                
                可選欄位：
                - Name：SKU 名稱
                - Price：SKU 價格
                - StockQuantity：庫存數量
                - IsDefault：是否為預設 SKU
                - ImageUrl：SKU 圖片 URL
                - Status：SKU 狀態
                - Specifications：規格陣列
                
                回傳格式：
                - 200 OK：更新後的 SKU（Sku 實體）
                - 401 Unauthorized：未登入
                - 403 Forbidden：權限不足
                - 404 Not Found：SKU 不存在
                
                使用範例：
                PUT /api/skus/456
                {
                    ""Price"": 38900,
                    ""StockQuantity"": 80
                }
                
                注意事項：
                - 只更新提供的欄位，未提供的欄位保持不變
                - Specifications 陣列會完全替換現有規格
                - 建議使用 HTTPS 傳輸（防止資料被截取）
            ")
            .WithTags("SKU")
            .Produces<Sku>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // DELETE /api/skus/{id} - 刪除 SKU
        // =========================================================================
        app.MapDelete("/api/skus/{id}", DeleteSkuAsync)
            .WithName("DeleteSku")
            .WithSummary("刪除 SKU")
            .WithDescription(@"
                刪除指定 ID 的 SKU
                
                認證要求：
                - 需要登入（通過 Cookie）
                - 需要管理員權限
                
                路徑參數：
                - id：SKU ID（必填）
                
                回傳格式：
                - 200 OK：刪除成功
                - 401 Unauthorized：未登入
                - 403 Forbidden：權限不足
                - 404 Not Found：SKU 不存在
                
                使用範例：
                - DELETE /api/skus/456
                
                注意事項：
                - 刪除操作不可逆，建議在 UI 層加入確認對話框
                - 建議檢查是否有庫存記錄
                - 建議檢查是否有訂單使用此 SKU
            ")
            .WithTags("SKU")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// 查詢商品的所有 SKU
    /// 
    /// 請求方式：GET /api/products/{productId}/skus
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的 Sku 陣列
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（productId）
    /// 2. 使用 Mediator 發送 SkusQuery 查詢
    /// 3. 回傳 SKU 列表
    /// 
    /// 錯誤處理：
    /// - 無錯誤（總是回傳 200 OK，即使沒有資料）
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">SkusQuery 查詢物件（包含商品 ID）</param>
    /// <returns>包含 SKU 列表的 JSON 回應</returns>
    private static async Task<IResult> GetSkusAsync(
        [FromServices] IMediator mediator,
        [AsParameters] SkusQuery query)
    {
        // 使用 Mediator 發送 SkusQuery 查詢
        // Mediator 會自動找到對應的 Handler (SkusQueryHandler)
        // Handler 會從資料庫查詢該商品的所有 SKU
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和 SKU 列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增 SKU
    /// 
    /// 請求方式：POST /api/products/{productId}/skus
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：JSON 格式的 SkuAddCommand
    /// 回應格式：JSON 格式的 Sku 實體
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（productId）和請求主體（SkuAddCommand）
    /// 2. 設定命令的 ProductId 屬性
    /// 3. 使用 Mediator 發送 SkuAddCommand 命令
    /// 4. 回傳新增後的 SKU
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 權限不足：回傳 403 Forbidden
    /// - 商品不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="productId">商品 ID（路徑參數）</param>
    /// <param name="command">SkuAddCommand 命令物件（包含 SKU 的所有資訊）</param>
    /// <returns>包含新增後的 SKU 的 JSON 回應</returns>
    private static async Task<IResult> AddSkuAsync(
        [FromServices] IMediator mediator,
        int productId,
        [FromBody] SkuAddCommand command)
    {
        // 設定命令的 ProductId 屬性
        command.ProductId = productId;
        
        // 使用 Mediator 發送 SkuAddCommand 命令
        // Mediator 會自動找到對應的 Handler (SkuAddHandler)
        // Handler 會建立新的 SKU 實體並儲存到資料庫
        var result = await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼和新增後的 SKU
        return Results.Ok(result);
    }

    /// <summary>
    /// 更新 SKU
    /// 
    /// 請求方式：PUT /api/skus/{id}
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：JSON 格式的 SkuUpdateCommand
    /// 回應格式：JSON 格式的 Sku 實體
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（id）和請求主體（SkuUpdateCommand）
    /// 2. 設定命令的 Id 屬性
    /// 3. 使用 Mediator 發送 SkuUpdateCommand 命令
    /// 4. 回傳更新後的 SKU
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 權限不足：回傳 403 Forbidden
    /// - SKU 不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="id">SKU ID（路徑參數）</param>
    /// <param name="command">SkuUpdateCommand 命令物件（包含要更新的欄位）</param>
    /// <returns>包含更新後的 SKU 的 JSON 回應</returns>
    private static async Task<IResult> UpdateSkuAsync(
        [FromServices] IMediator mediator,
        [FromBody] SkuUpdateCommand command)
    {   
        // 使用 Mediator 發送 SkuUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (SkuUpdateHandler)
        // Handler 會更新 SKU 實體並儲存到資料庫
        var result = await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼和更新後的 SKU
        return Results.Ok(result);
    }

    /// <summary>
    /// 刪除 SKU
    /// 
    /// 請求方式：DELETE /api/skus/{id}
    /// 認證要求：需要登入（通過 Cookie）
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（id）
    /// 2. 使用 Mediator 發送 SkuDeleteCommand 命令
    /// 3. 回傳 200 OK
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 權限不足：回傳 403 Forbidden
    /// - SKU 不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="id">SKU ID（路徑參數）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> DeleteSkuAsync(
        [FromServices] IMediator mediator,
        [AsParameters] SkuDeleteCommand command)
    {        
        // 使用 Mediator 發送 SkuDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (SkuDeleteHandler)
        // Handler 會刪除 SKU 實體並儲存變更到資料庫
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }
}
