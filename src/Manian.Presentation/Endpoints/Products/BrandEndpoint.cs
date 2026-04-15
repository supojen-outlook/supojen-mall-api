using Manian.Application.Commands.Products;
using Manian.Application.Queries.Products;
using Manian.Domain.Entities.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Products;

/// <summary>
/// 品牌相關的 API 端點定義類別
/// 
/// 職責：
/// - 定義品牌相關的 RESTful API 端點
/// - 處理 HTTP 請求並回傳適當的回應
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// - GET /api/brands/count - 查詢品牌總數
/// - GET /api/brands - 查詢品牌列表
/// - GET /api/brands/{id} - 查詢單一品牌
/// - GET /api/brands/{id}/path - 查詢品牌路徑
/// - POST /api/brands - 新增品牌
/// - PUT /api/brands - 更新品牌
/// - DELETE /api/brands - 刪除品牌
/// </summary>
public static class BrandEndpoint
{
    /// <summary>
    /// 註冊所有品牌相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapBrands() 即可註冊所有端點
    /// 
    /// 設計考量：
    /// - 使用擴充方法讓端點註冊更模組化
    /// - 每個端點對應一個處理方法，保持單一職責
    /// - 端點命名遵循 RESTful 規範
    /// </summary>
    /// <param name="app">IEndpointRouteBuilder 實例，用於註冊端點</param>
    public static void MapBrands(this IEndpointRouteBuilder app)
    {
        // 註冊查詢品牌總數的端點
        app.MapGet("/api/brands/count", GetBrandCountAsync)
        .WithName("GetBrandCount")
        .WithSummary("查詢品牌總數")
        .WithDescription(@"
            查詢系統中的品牌總數
            
            查詢參數：
            - 無需任何參數
            
            回傳格式：
            - 200 OK：品牌總數（整數）
            
            使用範例：
            - GET /api/brands/count
            
            效能說明：
            - 優先使用估計數量（查詢 PostgreSQL 系統目錄）
            - 誤差通常在 1-5% 以內
            - 適合儀表板、統計報表等不需要精確數值的場景
        ")
        .WithTags("品牌庫")
        .Produces<int>(StatusCodes.Status200OK);

        // 註冊查詢品牌列表的端點
        app.MapGet("/api/brands", GetBrandsAsync)
        .WithName("GetBrands")
        .WithSummary("查詢品牌列表")
        .WithDescription(@"
            查詢系統中的品牌列表
            
            查詢參數：
            - parentId：父品牌 ID（可選），用於查詢指定父品牌下的子品牌
            - level：品牌層級（可選），用於查詢指定層級的品牌
            - status：品牌狀態（可選），用於篩選啟用或停用的品牌
            - search：搜尋關鍵字（可選），會在 Name、Slug 中搜尋
            - lastCreatedAt：上一頁最後一筆資料的建立時間（可選），用於分頁
            - size：每頁資料筆數（可選），預設 1000
            
            回傳格式：
            - 200 OK：品牌列表
            
            使用範例：
            - GET /api/brands
            - GET /api/brands?parentId=1
            - GET /api/brands?level=2
            - GET /api/brands?status=active
            - GET /api/brands?search=nike
            - GET /api/brands?size=50
            
            說明：
            - 支援多層級品牌結構
            - 支援按父品牌篩選
            - 支援按層級篩選
            - 支援按狀態篩選
            - 支援模糊搜尋
            - 支援分頁查詢
        ")
        .WithTags("品牌庫")
        .Produces<IEnumerable<Brand>>(StatusCodes.Status200OK);

        // 註冊查詢品牌路徑的端點
        app.MapGet("/api/brands/{id}/path", GetBrandPathAsync)
        .WithName("GetBrandPath")
        .WithSummary("查詢品牌路徑")
        .WithDescription(@"
            查詢指定品牌的完整路徑快取
            
            查詢參數：
            - id：品牌 ID（路徑參數，必填）
            
            回傳格式：
            - 200 OK：品牌路徑快取（整數陣列）
            - 400 Bad Request：品牌不存在
            
            路徑快取說明：
            - PathCache 是一個整數陣列
            - 包含從根節點到當前節點的所有品牌 ID
            - 由資料庫觸發器自動維護
            - 範例：[1, 5, 8] 表示路徑為：品牌 1 > 品牌 5 > 品牌 8
            
            使用範例：
            - GET /api/brands/8/path
            - 回傳：[1, 5, 8]
            - 表示路徑為：品牌 1 > 品牌 5 > 品牌 8
            
            使用場景：
            - 顯示麵包屑導航（Breadcrumb Navigation）
            - 顯示品牌層級導航
            - 品牌頁面顯示完整路徑
        ")
        .WithTags("品牌庫")
        .Produces<IEnumerable<int>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // 註冊新增品牌的端點
        app.MapPost("/api/brands", AddBrandAsync)
        .WithName("AddBrand")
        .WithSummary("新增品牌")
        .WithDescription(@"
            新增一個品牌
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含品牌的所有資訊
            
            必填欄位：
            - Name：品牌名稱
            
            可選欄位：
            - Slug：URL 友好名稱（用於 SEO）
            - ParentId：父品牌 ID（null 表示根品牌）
            - SortOrder：排序順序（預設 0）
            - Status：狀態（active/inactive，預設 active）
            - LogoUrl：品牌標誌圖片 URL
            - Description：品牌描述
            - IsLeaf：是否為葉節點（預設 false）
            
            回傳格式：
            - 201 Created：新增後的品牌資料
            - 400 Bad Request：新增失敗
            
            使用範例：
            POST /api/brands
            {
                ""Name"": ""Nike"",
                ""Slug"": ""nike"",
                ""ParentId"": 1,
                ""Status"": ""active""
            }
            
            注意事項：
            - 新增後會自動計算 Level、PathCache、PathText
            - 由資料庫觸發器自動維護層級關係
        ")
        .WithTags("品牌庫")
        .Produces<Brand>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        
        // 註冊更新品牌的端點
        app.MapPut("/api/brands", UpdateBrandAsync)
        .WithName("UpdateBrand")
        .WithSummary("更新品牌")
        .WithDescription(@"
            更新指定的品牌
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含品牌 ID 和要更新的欄位
            
            必填欄位：
            - Id：品牌 ID
            
            可選欄位：
            - Name：品牌名稱
            - Slug：URL 友好名稱
            - ParentId：父品牌 ID
            - SortOrder：排序順序
            - Status：狀態
            - LogoUrl：品牌標誌圖片 URL
            - Description：品牌描述
            - IsLeaf：是否為葉節點
            
            回傳格式：
            - 200 OK：更新成功
            - 404 Not Found：品牌不存在
            - 400 Bad Request：更新失敗
            
            使用範例：
            PUT /api/brands
            {
                ""Id"": 8,
                ""Name"": ""Nike（已更新）"",
                ""Status"": ""inactive""
            }
            
            注意事項：
            - 更新 ParentId 會自動重新計算層級關係
            - 由資料庫觸發器自動維護 PathCache 和 PathText
        ")
        .WithTags("品牌庫")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        
        // 註冊刪除品牌的端點
        app.MapDelete("/api/brands", DeleteBrandAsync)
        .WithName("DeleteBrand")
        .WithSummary("刪除品牌")
        .WithDescription(@"
            刪除指定的品牌
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含品牌 ID
            
            必填欄位：
            - Id：品牌 ID
            
            回傳格式：
            - 204 No Content：刪除成功
            - 404 Not Found：品牌不存在
            - 400 Bad Request：刪除失敗
            
            使用範例：
            DELETE /api/brands
            {
                ""Id"": 8
            }
            
            注意事項：
            - 刪除操作不可逆，建議在 UI 層加入確認對話框
            - 如果品牌有子品牌，刪除會失敗
            - 如果有產品使用此品牌，可能會導致資料不一致
            - 建議考慮實作軟刪除（標記為已刪除）而非硬刪除
        ")
        .WithTags("品牌庫")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

    }

    /// <summary>
    /// 查詢品牌總數
    /// 
    /// 請求方式：GET /api/brands/count
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的整數（品牌總數）
    /// 
    /// 執行流程：
    /// 1. 使用 BrandCountQuery 查詢品牌總數
    /// 2. 回傳品牌總數
    /// 
    /// 錯誤處理：
    /// - 查詢失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">BrandCountQuery 查詢物件</param>
    /// <returns>包含品牌總數的 JSON 回應</returns>
    private static async Task<IResult> GetBrandCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] BrandCountQuery query)
    {
        // 使用 Mediator 發送 BrandCountQuery 查詢
        // Mediator 會自動找到對應的 Handler (BrandCountQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和品牌總數
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢品牌列表
    /// 
    /// 請求方式：GET /api/brands
    /// 認證要求：不需要登入
    /// 請求參數：
    /// - parentId: 父品牌 ID（可選）
    /// - level: 品牌層級（可選）
    /// - status: 品牌狀態（可選）
    /// 回應格式：JSON 格式的 Brand 集合
    /// 
    /// 執行流程：
    /// 1. 接收查詢參數
    /// 2. 使用 BrandsQuery 查詢品牌列表
    /// 3. 回傳品牌列表
    /// 
    /// 錯誤處理：
    /// - 查詢失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">BrandsQuery 查詢物件</param>
    /// <returns>包含品牌列表的 JSON 回應</returns>
    private static async Task<IResult> GetBrandsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] BrandsQuery query)
    {
        // 使用 Mediator 發送 BrandsQuery 查詢
        // Mediator 會自動找到對應的 Handler (BrandsQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和品牌列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢品牌路徑
    /// 
    /// 請求方式：GET /api/brands/{id}/path
    /// 認證要求：不需要登入
    /// 請求參數：
    /// - id: 品牌 ID（路徑參數）
    /// 回應格式：JSON 格式的整數陣列（從根節點到當前節點的所有品牌 ID）
    /// 
    /// 執行流程：
    /// 1. 接收品牌 ID
    /// 2. 使用 BrandPathCacheQuery 查詢品牌路徑
    /// 3. 回傳品牌路徑
    /// 
    /// 錯誤處理：
    /// - 品牌不存在：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">BrandPathCacheQuery 查詢物件</param>
    /// <returns>包含品牌路徑的 JSON 回應</returns>
    private static async Task<IResult> GetBrandPathAsync(
        [FromServices] IMediator mediator,
        [AsParameters] BrandPathCacheQuery query)
    {
        // 使用 Mediator 發送 BrandPathCacheQuery 查詢
        // Mediator 會自動找到對應的 Handler (BrandPathCacheQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和品牌路徑
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增品牌
    /// 
    /// 請求方式：POST /api/brands
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 BrandAddCommand
    /// 回應格式：JSON 格式的 Brand（包含資料庫自動生成的欄位）
    /// 
    /// 執行流程：
    /// 1. 接收品牌資料
    /// 2. 使用 BrandAddCommand 新增品牌
    /// 3. 回傳新增後的品牌資料
    /// 
    /// 錯誤處理：
    /// - 新增失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="command">BrandAddCommand 命令物件</param>
    /// <returns>包含新增後品牌資料的 JSON 回應</returns>
    private static async Task<IResult> AddBrandAsync(
        [FromServices] IMediator mediator,
        [AsParameters] BrandAddCommand command)
    {
        // 使用 Mediator 發送 BrandAddCommand 命令
        // Mediator 會自動找到對應的 Handler (BrandAddHandler)
        var result = await mediator.SendAsync(command);
        
        // 回傳 201 Created 狀態碼和新增後的品牌資料
        // 第二個參數是資源的位置（Location header）
        return Results.Created($"/api/brands/{result.Id}", result);
    }

    /// <summary>
    /// 更新品牌
    /// 
    /// 請求方式：PUT /api/brands
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 BrandUpdateCommand
    /// 回應格式：JSON 格式的 Brand（更新後的資料）
    /// 
    /// 執行流程：
    /// 1. 接收品牌資料（包含 ID）
    /// 2. 使用 BrandUpdateCommand 更新品牌
    /// 3. 回傳更新後的品牌資料
    /// 
    /// 錯誤處理：
    /// - 品牌不存在：回傳 404 Not Found
    /// - 更新失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="command">BrandUpdateCommand 命令物件</param>
    /// <returns>包含更新後品牌資料的 JSON 回應</returns>
    private static async Task<IResult> UpdateBrandAsync(
        [FromServices] IMediator mediator,
        [FromBody] BrandUpdateCommand command)
    {
        // 使用 Mediator 發送 BrandUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (BrandUpdateHandler)
        await mediator.SendAsync(command);
        
        // 回傳 200 OK
        return Results.Ok();
    }

    /// <summary>
    /// 刪除品牌
    /// 
    /// 請求方式：DELETE /api/brands
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 BrandDeleteCommand
    /// 回應格式：204 No Content
    /// 
    /// 執行流程：
    /// 1. 接收品牌資料（包含 ID）
    /// 2. 使用 BrandDeleteCommand 刪除品牌
    /// 3. 回傳 204 No Content
    /// 
    /// 錯誤處理：
    /// - 品牌不存在：回傳 404 Not Found
    /// - 刪除失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="command">BrandDeleteCommand 命令物件</param>
    /// <returns>204 No Content 狀態碼</returns>
    private static async Task<IResult> DeleteBrandAsync(
        [FromServices] IMediator mediator,
        [AsParameters] BrandDeleteCommand command)
    {
        // 使用 Mediator 發送 BrandDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (BrandDeleteHandler)
        await mediator.SendAsync(command);
        
        // 回傳 204 No Content 狀態碼
        return Results.NoContent();
    }
}
