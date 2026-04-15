using Manian.Application.Commands.Assets;
using Manian.Application.Queries.Assets;
using Manian.Domain.Entities.Assets;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints;

/// <summary>
/// 資產 API 端點
/// 
/// 職責：
/// - 定義資產相關的 API 端點
/// - 處理資產的查詢、新增、更新、刪除請求
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// - GET /api/assets - 查詢資產列表
/// - POST /api/assets - 新增資產
/// - PUT /api/assets - 更新資產
/// - DELETE /api/assets/{id} - 刪除資產
/// 
/// Scalar 文件：
/// - 使用 Scalar 替代 Swagger UI
/// - 提供更現代化的 API 文件介面
/// - 支援深色模式和更好的互動體驗
/// </summary>
public static class AssetEndpoint
{
    /// <summary>
    /// 註冊所有資產相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapAssets() 即可註冊所有端點
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
    public static void MapAssets(this IEndpointRouteBuilder app)
    {
        // =========================================================================
        // GET /api/assets - 查詢資產列表
        // =========================================================================
        app.MapGet("/api/assets", GetAssetsAsync)
            .WithName("GetAssets")
            .WithSummary("查詢資產列表")
            .WithDescription(@"
                查詢系統中的資產列表，支援游標分頁和篩選
                
                認證要求：
                - 不需要登入（公開端點）
                
                查詢參數：
                - cursor：游標（可選），上一頁最後一筆資料的 Id，用於分頁
                - size：每頁資料筆數（可選）
                - targetType：關聯目標類型（可選），篩選指定類型的資產
                - isTargetIdNull：是否篩選未關聯資源（可選），true 查詢孤兒資源，false 查詢已關聯資源
                
                回傳格式：
                - 200 OK：資產列表（Asset 實體陣列）
                
                回傳欄位說明：
                - Id：資產 ID
                - TargetType：關聯目標類型
                - TargetId：關聯目標 ID
                - MediaType：媒體類型
                - Url：公開訪問 URL
                - OriginalFileName：原始檔案名稱
                - MimeType：MIME 類型
                - FileSizeBytes：檔案大小
                - S3Bucket：S3 存儲桶名稱
                - S3Key：S3 對象鍵
                - SortOrder：排序順序
                
                使用範例：
                - GET /api/assets
                - GET /api/assets?targetType=product
                - GET /api/assets?isTargetIdNull=true (查詢孤兒資源)
                - GET /api/assets?cursor=100&size=20
                
                注意事項：
                - 分頁使用 cursor 參數，而非傳統的 page 參數
                - cursor 為上一頁最後一筆資料的 Id
                - 若回傳數量小於 size，代表已無更多資料
            ")
            .WithTags("資產")
            .AllowAnonymous()
            .Produces<IEnumerable<Asset>>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/assets - 新增資產
        // =========================================================================
        app.MapPost("/api/assets", AddAssetAsync)
            .WithName("AddAsset")
            .WithSummary("新增資產")
            .WithDescription(@"
                新增一個資產到系統中並上傳檔案
                
                認證要求：
                - 需要登入（通過 Cookie）
                
                請求格式：
                - Content-Type: multipart/form-data
                
                必填欄位：
                - file：檔案二進位流
                - fileExt：檔案副檔名 (例如 .jpg, .mp4)
                
                回傳格式：
                - 200 OK：新增成功的資產（Asset 實體）
                - 401 Unauthorized：未登入
                
                回傳欄位說明：
                - Id：資產 ID
                - Url：公開訪問 URL
                - S3Key：S3 對象鍵
                - FileSizeBytes：檔案大小
                
                使用範例：
                POST /api/assets
                Content-Type: multipart/form-data; boundary=----WebKitFormBoundary
                
                ------WebKitFormBoundary
                Content-Disposition: form-data; name=""file""; filename=""image.jpg""
                Content-Type: image/jpeg
                
                [binary data]
                ------WebKitFormBoundary
                Content-Disposition: form-data; name=""fileExt""
                
                .jpg
                ------WebKitFormBoundary--
                
                注意事項：
                - 檔案會直接上傳至 S3
                - S3Key 會自動生成為 {Id}{fileExt}
                - MediaType 會根據 fileExt 自動判斷 (image/video)
                - 建議限制上傳檔案大小
            ")
            .WithTags("資產")
            .Produces<Asset>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // =========================================================================
        // PUT /api/assets - 更新資產
        // =========================================================================
        app.MapPut("/api/assets", UpdateAssetAsync)
            .WithName("UpdateAsset")
            .WithSummary("更新資產")
            .WithDescription(@"
                更新指定資產的關聯資訊
                
                認證要求：
                - 需要登入（通過 Cookie）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含要更新的欄位
                
                必填欄位：
                - url：資產 URL（作為查詢條件）
                
                可選欄位：
                - targetType：關聯目標類型
                - targetId：關聯目標 ID
                
                回傳格式：
                - 200 OK：更新成功
                - 401 Unauthorized：未登入
                - 404 Not Found：資產不存在
                
                使用範例：
                PUT /api/assets
                {
                    ""url"": ""https://example.com/assets/123.jpg"",
                    ""targetType"": ""product"",
                    ""targetId"": 5
                }
                
                注意事項：
                - 使用 URL 作為查詢條件，確保更新正確的資產
                - 若 targetId 為 null，則解除關聯
                - 此操作不會修改檔案本身
            ")
            .WithTags("資產")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // DELETE /api/assets/{id} - 刪除資產
        // =========================================================================
        app.MapDelete("/api/assets", DeleteAssetAsync)
            .WithName("DeleteAsset")
            .WithSummary("刪除資產")
            .WithDescription(@"
                刪除指定 ID 的資產
                
                認證要求：
                - 需要登入（通過 Cookie）
                
                路徑參數：
                - id：資產 ID（必填）
                
                回傳格式：
                - 200 OK：刪除成功
                - 401 Unauthorized：未登入
                - 404 Not Found：資產不存在
                
                使用範例：
                - DELETE /api/assets/123
                
                注意事項：
                - 刪除操作不可逆，建議在 UI 層加入確認對話框
                - 刪除操作僅刪除資料庫記錄，不會刪除 S3 上的實體檔案
                - S3 檔案清理需由獨立的定時任務處理
            ")
            .WithTags("資產")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// 查詢資產列表
    /// 
    /// 請求方式：GET /api/assets
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的 Asset 陣列
    /// 
    /// 執行流程：
    /// 1. 接收查詢參數（cursor、size、targetType、isTargetIdNull）
    /// 2. 使用 Mediator 發送 AssetsQuery 查詢
    /// 3. 回傳資產列表
    /// 
    /// 錯誤處理：
    /// - 無錯誤（總是回傳 200 OK，即使沒有資料）
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">AssetsQuery 查詢物件（包含搜尋和分頁參數）</param>
    /// <returns>包含資產列表的 JSON 回應</returns>
    private static async Task<IResult> GetAssetsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AssetsQuery query)
    {
        // 使用 Mediator 發送 AssetsQuery 查詢
        // Mediator 會自動找到對應的 Handler (AssetsQueryHandler)
        // Handler 會從資料庫查詢資產列表
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和資產列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增資產
    /// 
    /// 請求方式：POST /api/assets
    /// 認證要求：不需要登入（端點本身允許匿名，但需通過防偽驗證）
    /// 請求格式：multipart/form-data
    /// 回應格式：JSON 格式的 Asset 實體
    /// 
    /// 執行流程：
    /// 1. 手動驗證防偽令牌
    /// 2. 檢查請求內容類型是否為 Form Data
    /// 3. 從 Form 中讀取檔案
    /// 4. 將檔案流轉換為 MemoryStream
    /// 5. 發送 AssetAddCommand 給 Mediator
    /// 6. 回傳新增後的資產
    /// 
    /// 錯誤處理：
    /// - 防偽驗證失敗：拋出 400 Bad Request
    /// - 非表單請求：回傳 400 Bad Request
    /// - 未提供檔案：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="antiforgery">防偽服務實例，用於手動驗證令牌</param>
    /// <param name="context">HTTP 上下文，用於存取請求細節</param>
    /// <returns>包含新增後的資產的 JSON 回應</returns>
    private static async Task<IResult> AddAssetAsync(
        [FromServices] IMediator mediator,
        [FromServices] IAntiforgery antiforgery,
        HttpContext context)
    {
        // ============================================
        // 1. 手動驗證防偽令牌
        // ============================================
        // 由於我們需要從 Header 讀取 Token 而非預設的 Form Field，
        // 並且在 Controller 外部 (Minimal API)，我們需要手動觸發驗證。
        // 這會檢查 Header "X-CSRF-TOKEN" 是否與 Cookie 中的 Token 匹配。
        await antiforgery.ValidateRequestAsync(context);

        // ============================================
        // 2. 檢查是否為 Form 請求
        // ============================================
        // 確保請求的 Content-Type 是 multipart/form-data
        // 避免嘗試從非表單請求中讀取檔案導致異常
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Invalid request format" });
        }

        // ============================================
        // 3. 手動讀取 Form 中的檔案
        // ============================================
        // 使用 ReadFormAsync 異步讀取表單數據
        // GetFile("file") 根據 name 屬性獲取上傳的檔案
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.GetFile("file");

        // 驗證檔案是否存在及是否有內容
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "未選擇檔案" });
        }

        // ============================================
        // 4. 處理檔案
        // ============================================
        // 將上傳的檔案流複製到 MemoryStream
        // 這是因為 IFormFile 是一個向後流，只能讀取一次，
        // 轉換為 MemoryStream 後可以隨機讀取或傳遞給其他服務
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        // ============================================
        // 5. 建立並發送 Command
        // ============================================
        // 準備 AssetAddCommand，包含檔案流和副檔名
        // 使用 ToLowerInvariant() 確保副檔名大小寫一致性
        var command = new AssetAddCommand
        {
            File = memoryStream,
            FileExt = Path.GetExtension(file.FileName).ToLowerInvariant()
        };

        // 使用 Mediator 發送 AssetAddCommand 命令
        // Mediator 會自動找到對應的 Handler (AssetAddHandler)
        // Handler 會建立新的資產實體並上傳檔案至 S3
        var result = await mediator.SendAsync(command);

        // ============================================
        // 6. 回傳結果
        // ============================================
        // 回傳 200 OK 狀態碼和新增後的資產
        return Results.Ok(result);
    }


    /// <summary>
    /// 更新資產
    /// 
    /// 請求方式：PUT /api/assets
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：JSON 格式的 AssetUpdateCommand
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 接收請求主體（AssetUpdateCommand）
    /// 2. 使用 Mediator 發送 AssetUpdateCommand 命令
    /// 3. 回傳 200 OK
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 資產不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="command">AssetUpdateCommand 命令物件（包含 URL 及要更新的欄位）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> UpdateAssetAsync(
        [FromServices] IMediator mediator,
        [FromBody] AssetUpdateCommand command)
    {
        // 使用 Mediator 發送 AssetUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (AssetUpdateHandler)
        // Handler 會更新資產實體並儲存到資料庫
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 刪除資產
    /// 
    /// 請求方式：DELETE /api/assets/{id}
    /// 認證要求：需要登入（通過 Cookie）
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 接收路徑參數（id）
    /// 2. 使用 Mediator 發送 AssetDeleteCommand 命令
    /// 3. 回傳 200 OK
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 資產不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="id">資產 ID（路徑參數）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> DeleteAssetAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AssetDeleteCommand command)
    {        
        // 使用 Mediator 發送 AssetDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (AssetDeleteHandler)
        // Handler 會刪除資產實體並儲存變更到資料庫
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }
}
