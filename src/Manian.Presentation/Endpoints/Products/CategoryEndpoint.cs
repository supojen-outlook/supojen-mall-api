using Manian.Application.Commands.Products;
using Manian.Application.Queries.Products;
using Manian.Domain.Entities.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Products;

/// <summary>
/// 產品類別相關的 API 端點定義類別
/// 
/// 職責：
/// - 定義產品類別相關的 RESTful API 端點
/// - 處理 HTTP 請求並回傳適當的回應
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// - GET /api/categories/count - 查詢類別總數
/// - GET /api/categories - 查詢類別列表
/// - GET /api/categories/{id} - 查詢單一類別
/// - GET /api/categories/{id}/path - 查詢類別路徑
/// - POST /api/categories - 新增類別
/// - PUT /api/categories - 更新類別
/// - DELETE /api/categories - 刪除類別
/// </summary>
public static class CategoryEndpoint
{
    /// <summary>
    /// 註冊所有產品類別相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapCategories() 即可註冊所有端點
    /// 
    /// 設計考量：
    /// - 使用擴充方法讓端點註冊更模組化
    /// - 每個端點對應一個處理方法，保持單一職責
    /// - 端點命名遵循 RESTful 規範
    /// </summary>
    /// <param name="app">IEndpointRouteBuilder 實例，用於註冊端點</param>
    public static void MapCategories(this IEndpointRouteBuilder app)
    {
        // 註冊查詢類別總數的端點
        app.MapGet("/api/categories/count", GetCategoryCountAsync)
        .WithName("GetCategoryCount")
        .WithSummary("查詢產品類別總數")
        .WithDescription(@"
            查詢系統中的產品類別總數
            
            查詢參數：
            - 無需任何參數
            
            回傳格式：
            - 200 OK：產品類別總數（整數）
            
            使用範例：
            - GET /api/categories/count
            
            效能說明：
            - 優先使用估計數量（查詢 PostgreSQL 系統目錄）
            - 誤差通常在 1-5% 以內
            - 適合儀表板、統計報表等不需要精確數值的場景
        ")
        .WithTags("類目庫")
        .Produces<int>(StatusCodes.Status200OK);

        // 註冊查詢類別列表的端點
        app.MapGet("/api/categories", GetCategoriesAsync)
        .WithSummary("查詢產品類別列表")
        .WithDescription(@"
            查詢系統中的產品類別列表
            
            查詢參數：
            - parentId：父類別 ID（可選），用於查詢指定父類別下的子類別
            - level：類別層級（可選），用於查詢指定層級的類別
            - status：類別狀態（可選），用於篩選啟用或停用的類別
            - search：搜尋關鍵字（可選），會在 Name、Slug 中搜尋
            - lastCreatedAt：上一頁最後一筆資料的建立時間（可選），用於分頁
            - size：每頁資料筆數（可選），預設 1000
            
            回傳格式：
            - 200 OK：產品類別列表
            
            使用範例：
            - GET /api/categories
            - GET /api/categories?parentId=1
            - GET /api/categories?level=2
            - GET /api/categories?status=active
            - GET /api/categories?search=phone
            - GET /api/categories?size=50
            
            說明：
            - 支援多層級類別結構
            - 支援按父類別篩選
            - 支援按層級篩選
            - 支援按狀態篩選
            - 支援模糊搜尋
            - 支援分頁查詢
        ")
        .WithTags("類目庫")
        .Produces<IEnumerable<Category>>(StatusCodes.Status200OK);

        // 註冊查詢類別路徑的端點
        app.MapGet("/api/categories/{id}/path", GetCategoryPathAsync)
        .WithName("GetCategoryPath")
        .WithSummary("查詢產品類別路徑")
        .WithDescription(@"
            查詢指定產品類別的完整路徑快取
            
            查詢參數：
            - id：類別 ID（路徑參數，必填）
            
            回傳格式：
            - 200 OK：類別路徑快取（整數陣列）
            - 400 Bad Request：類別不存在
            
            路徑快取說明：
            - PathCache 是一個整數陣列
            - 包含從根節點到當前節點的所有類別 ID
            - 由資料庫觸發器自動維護
            - 範例：[1, 5, 8] 表示路徑為：類別 1 > 類別 5 > 類別 8
            
            使用範例：
            - GET /api/categories/8/path
            - 回傳：[1, 5, 8]
            - 表示路徑為：類別 1 > 類別 5 > 類別 8
            
            使用場景：
            - 顯示麵包屑導航（Breadcrumb Navigation）
            - 顯示類別層級導航
            - 產品類別頁面顯示完整路徑
        ")
        .WithTags("類目庫")
        .Produces<IEnumerable<int>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        
        // 註冊新增類別的端點
        app.MapPost("/api/categories", AddCategoryAsync)
        .WithSummary("新增類別")
        .WithDescription(@"
            新增一個產品類別
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含類別的所有資訊
            
            必填欄位：
            - Name：類別名稱
            
            可選欄位：
            - Slug：URL 友好名稱（用於 SEO）
            - ParentId：父類別 ID（null 表示根類別）
            - SortOrder：排序順序（預設 0）
            - Status：狀態（active/inactive，預設 active）
            - ImageUrl：類別圖片 URL
            - IsLeaf：是否為葉節點（預設 false）
            
            回傳格式：
            - 201 Created：新增後的類別資料
            - 400 Bad Request：新增失敗
            
            使用範例：
            POST /api/categories
            {
                ""Name"": ""智慧型手機"",
                ""Slug"": ""smartphones"",
                ""ParentId"": 1,
                ""Status"": ""active""
            }
            
            注意事項：
            - 新增後會自動計算 Level、PathCache、PathText
            - 由資料庫觸發器自動維護層級關係
        ")
        .WithTags("類目庫")
        .Produces<Category>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // 註冊更新類別的端點
        app.MapPut("/api/categories", UpdateCategoryAsync)
        .WithSummary("更新類別")
        .WithDescription(@"
            更新指定的產品類別
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含類別 ID 和要更新的欄位
            
            必填欄位：
            - Id：類別 ID
            
            可選欄位：
            - Name：類別名稱
            - Slug：URL 友好名稱
            - ParentId：父類別 ID
            - SortOrder：排序順序
            - Status：狀態
            - ImageUrl：類別圖片 URL
            - IsLeaf：是否為葉節點
            
            回傳格式：
            - 200 OK：更新成功
            - 404 Not Found：類別不存在
            - 400 Bad Request：更新失敗
            
            使用範例：
            PUT /api/categories
            {
                ""Id"": 8,
                ""Name"": ""智慧型手機（已更新）"",
                ""Status"": ""inactive""
            }
            
            注意事項：
            - 更新 ParentId 會自動重新計算層級關係
            - 由資料庫觸發器自動維護 PathCache 和 PathText
        ")
        .WithTags("類目庫")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // 註冊刪除類別的端點
        app.MapDelete("/api/categories", DeleteCategoryAsync)
        .WithSummary("刪除類別")
        .WithDescription(@"
            刪除指定的產品類別
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含類別 ID
            
            必填欄位：
            - Id：類別 ID
            
            回傳格式：
            - 204 No Content：刪除成功
            - 404 Not Found：類別不存在
            - 400 Bad Request：刪除失敗
            
            使用範例：
            DELETE /api/categories
            {
                ""Id"": 8
            }
            
            注意事項：
            - 刪除操作不可逆，建議在 UI 層加入確認對話框
            - 如果類別有子類別，刪除會失敗
            - 如果有產品使用此類別，可能會導致資料不一致
            - 建議考慮實作軟刪除（標記為已刪除）而非硬刪除
        ")
        .WithTags("類目庫")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // 註冊新增類別屬性關聯的端點
        app.MapPost("/api/categories/attributes", AddCategoryAttributeAsync)
        .WithSummary("新增類別屬性關聯")
        .WithDescription(@"
            將指定的屬性鍵關聯到指定的類別
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含類別 ID 與屬性鍵 ID
            
            必填欄位：
            - CategoryId：類別 ID
            - AttributeKeyId：屬性鍵 ID
            
            回傳格式：
            - 200 OK：新增成功
            - 404 Not Found：類別或屬性鍵不存在
            - 400 Bad Request：請求參數錯誤
            
            使用範例：
            POST /api/categories/attributes
            {
                ""CategoryId"": 5,
                ""AttributeKeyId"": 10
            }
            
            注意事項：
            - 如果關聯已存在，系統會忽略操作並返回成功
            - 關聯建立後，該類別下的商品即可使用該屬性
        ")
        .WithTags("類目庫")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // 註冊移除類別屬性關聯的端點
        app.MapDelete("/api/categories/attributes", DeleteCategoryAttributeAsync)
        .WithSummary("移除類別屬性關聯")
        .WithDescription(@"
            移除指定類別下的某個屬性鍵關聯
            
            請求格式：
            - Content-Type: application/json
            - 請求主體包含類別 ID 與屬性鍵 ID
            
            必填欄位：
            - CategoryId：類別 ID
            - AttributeKeyId：屬性鍵 ID
            
            回傳格式：
            - 200 OK：移除成功
            - 404 Not Found：類別或屬性鍵不存在
            - 400 Bad Request：請求參數錯誤
            
            使用範例：
            DELETE /api/categories/attributes
            {
                ""CategoryId"": 5,
                ""AttributeKeyId"": 10
            }
            
            注意事項：
            - 如果關聯不存在，不會拋出錯誤 (安靜失敗)
            - 移除後，該類別下的商品將無法再使用該屬性
        ")
        .WithTags("類目庫")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// 查詢產品類別總數
    /// 
    /// 請求方式：GET /api/categories/count
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的整數（類別總數）
    /// 
    /// 執行流程：
    /// 1. 使用 CategoryCountQuery 查詢類別總數
    /// 2. 回傳類別總數
    /// 
    /// 錯誤處理：
    /// - 查詢失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">CategoryCountQuery 查詢物件</param>
    /// <returns>包含類別總數的 JSON 回應</returns>
    private static async Task<IResult> GetCategoryCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CategoryCountQuery query)
    {
        // 使用 Mediator 發送 CategoryCountQuery 查詢
        // Mediator 會自動找到對應的 Handler (CategoryCountQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和類別總數
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢產品類別列表
    /// 
    /// 請求方式：GET /api/categories
    /// 認證要求：不需要登入
    /// 請求參數：
    /// - parentId: 父類別 ID（可選）
    /// - level: 類別層級（可選）
    /// - status: 類別狀態（可選）
    /// 回應格式：JSON 格式的 Category 集合
    /// 
    /// 執行流程：
    /// 1. 接收查詢參數
    /// 2. 使用 CategoriesQuery 查詢類別列表
    /// 3. 回傳類別列表
    /// 
    /// 錯誤處理：
    /// - 查詢失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">CategoriesQuery 查詢物件</param>
    /// <returns>包含類別列表的 JSON 回應</returns>
    private static async Task<IResult> GetCategoriesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CategoriesQuery query)
    {
        // 使用 Mediator 發送 CategoriesQuery 查詢
        // Mediator 會自動找到對應的 Handler (CategoriesQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和類別列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢產品類別路徑
    /// 
    /// 請求方式：GET /api/categories/{id}/path
    /// 認證要求：不需要登入
    /// 請求參數：
    /// - id: 類別 ID（路徑參數）
    /// 回應格式：JSON 格式的整數陣列（從根節點到當前節點的所有類別 ID）
    /// 
    /// 執行流程：
    /// 1. 接收類別 ID
    /// 2. 使用 CategoryPathCacheQuery 查詢類別路徑
    /// 3. 回傳類別路徑
    /// 
    /// 錯誤處理：
    /// - 類別不存在：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">CategoryPathCacheQuery 查詢物件</param>
    /// <returns>包含類別路徑的 JSON 回應</returns>
    private static async Task<IResult> GetCategoryPathAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CategoryPathCacheQuery query)
    {
        // 使用 Mediator 發送 CategoryPathCacheQuery 查詢
        // Mediator 會自動找到對應的 Handler (CategoryPathCacheQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和類別路徑
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增產品類別
    /// 
    /// 請求方式：POST /api/categories
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 CategoryAddCommand
    /// 回應格式：JSON 格式的 Category（包含資料庫自動生成的欄位）
    /// 
    /// 執行流程：
    /// 1. 接收類別資料
    /// 2. 使用 CategoryAddCommand 新增類別
    /// 3. 回傳新增後的類別資料
    /// 
    /// 錯誤處理：
    /// - 新增失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="command">CategoryAddCommand 命令物件</param>
    /// <returns>包含新增後類別資料的 JSON 回應</returns>
    private static async Task<IResult> AddCategoryAsync(
        [FromServices] IMediator mediator,
        [FromBody] CategoryAddCommand command)
    {
        // 使用 Mediator 發送 CategoryAddCommand 命令
        // Mediator 會自動找到對應的 Handler (CategoryAddHandler)
        var result = await mediator.SendAsync(command);
        
        // 回傳 201 Created 狀態碼和新增後的類別資料
        // 第二個參數是資源的位置（Location header）
        return Results.Created($"/api/categories/{result.Id}", result);
    }

    /// <summary>
    /// 更新產品類別
    /// 
    /// 請求方式：PUT /api/categories
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 CategoryUpdateCommand
    /// 回應格式：JSON 格式的 Category（更新後的資料）
    /// 
    /// 執行流程：
    /// 1. 接收類別資料（包含 ID）
    /// 2. 使用 CategoryUpdateCommand 更新類別
    /// 3. 回傳更新後的類別資料
    /// 
    /// 錯誤處理：
    /// - 類別不存在：回傳 404 Not Found
    /// - 更新失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="command">CategoryUpdateCommand 命令物件</param>
    /// <returns>包含更新後類別資料的 JSON 回應</returns>
    private static async Task<IResult> UpdateCategoryAsync(
        [FromServices] IMediator mediator,
        [FromBody] CategoryUpdateCommand command)
    {
        // 使用 Mediator 發送 CategoryUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (CategoryUpdateHandler)
        await mediator.SendAsync(command);
        
        // 回傳 200 OK
        return Results.Ok();
    }

    /// <summary>
    /// 刪除產品類別
    /// 
    /// 請求方式：DELETE /api/categories
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 CategoryDeleteCommand
    /// 回應格式：204 No Content
    /// 
    /// 執行流程：
    /// 1. 接收類別資料（包含 ID）
    /// 2. 使用 CategoryDeleteCommand 刪除類別
    /// 3. 回傳 204 No Content
    /// 
    /// 錯誤處理：
    /// - 類別不存在：回傳 404 Not Found
    /// - 刪除失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="command">CategoryDeleteCommand 命令物件</param>
    /// <returns>204 No Content 狀態碼</returns>
    private static async Task<IResult> DeleteCategoryAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CategoryDeleteCommand command)
    {
        // 使用 Mediator 發送 CategoryDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (CategoryDeleteHandler)
        await mediator.SendAsync(command);
        
        // 回傳 204 No Content 狀態碼
        return Results.NoContent();
    }

    /// <summary>
    /// 新增類別屬性關聯的非同步處理方法
    /// 
    /// 職責：
    /// - 接收 HTTP 請求並將其轉發給 Mediator
    /// - 作為 Endpoint 與 Application 邏輯層之間的橋樑
    /// 
    /// 參數綁定說明：
    /// - [FromServices]：告訴 ASP.NET Core 從依賴注入容器 (DI) 中解析 IMediator
    /// - [FromBody]：告訴 ASP.NET Core 從 HTTP 請求的主體 中反序列化數據
    /// 
    /// 執行流程：
    /// 1. 框架自動從 DI 獲取 Mediator 實例
    /// 2. 框架自動將 JSON Body 轉換為 CategoryAttributeAddCommand 物件
    /// 3. 透過 Mediator 發送命令，觸發對應的 Handler
    /// 4. 等待 Handler 執行完成
    /// 5. 返回 HTTP 200 OK 回應
    /// </summary>
    /// <param name="mediator">
    /// Mediator 實例 (由 [FromServices] 自動注入)
    /// 用於發送 Command 或 Query
    /// </param>
    /// <param name="command">
    /// 新增關聯命令物件 (由 [FromBody] 自自動綁定)
    /// 包含 CategoryId 與 AttributeKeyId
    /// </param>
    /// <returns>
    /// IResult：表示 HTTP 回應的結果
    /// 此處固定返回 Results.Ok() (HTTP 200)
    /// </returns>
    private static async Task<IResult> AddCategoryAttributeAsync(
        [FromServices] IMediator mediator,
        [FromBody] CategoryAttributeAddCommand command)
    {
        // 發送命令給 Mediator
        // Mediator 會找到對應的 CategoryAttributeAddCommandHandler 並執行
        // await 關鍵字確保我們等待資料庫操作完成後再回應
        await mediator.SendAsync(command);

        // 返回 HTTP 200 OK 狀態碼
        // 注意：雖然 Handler 可能返回了實體數據，但此處方法簽名未定義返回類型，
        // 若需返回數據，應改為 Results.Ok(result) 或修改方法簽名
        return Results.Ok();
    }

    /// <summary>
    /// 移除類別屬性關聯的非同步處理方法
    /// 
    /// 職責：
    /// - 接收 HTTP 請求並將其轉發給 Mediator
    /// - 作為 Endpoint 與 Application 邏輯層之間的橋樑
    /// 
    /// 參數綁定說明：
    /// - [FromServices]：告訴 ASP.NET Core 從依賴注入容器 (DI) 中解析 IMediator
    /// - [AsParameters]：告訴 ASP.NET Core 自動將請求參數 (路由、Query、Body) 映射到物件的屬性上
    /// 
    /// 執行流程：
    /// 1. 框架自動從 DI 獲取 Mediator 實例
    /// 2. 框架自動解析路由參數 (如 {categoryId}) 並填充到 Command 物件中
    /// 3. 透過 Mediator 發送命令，觸發對應的 Handler
    /// 4. 等待 Handler 執行完成
    /// 5. 返回 HTTP 200 OK 回應
    /// </summary>
    /// <param name="mediator">
    /// Mediator 實例 (由 [FromServices] 自動注入)
    /// 用於發送 Command 或 Query
    /// </param>
    /// <param name="command">
    /// 移除關聯命令物件 (由 [AsParameters] 自動綁定)
    /// 框架會嘗試從路由、Query String 或 Body 中尋找匹配的屬性名稱並賦值
    /// 例如：路由 /api/categories/5/attributes/10 會自動填入 CategoryId=5, AttributeKeyId=10
    /// </param>
    /// <returns>
    /// IResult：表示 HTTP 回應的結果
    /// 此處固定返回 Results.Ok() (HTTP 200)
    /// </returns>
    private static async Task<IResult> DeleteCategoryAttributeAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CategoryAttributeDeleteCommand command)
    {
        // 發送命令給 Mediator
        // Mediator 會找到對應的 CategoryAttributeDeleteCommandHandler 並執行
        await mediator.SendAsync(command);

        // 返回 HTTP 200 OK 狀態碼
        // 注意：對於刪除操作，通常慣例是返回 204 No Content (Results.NoContent())
        return Results.Ok();
    }
}
