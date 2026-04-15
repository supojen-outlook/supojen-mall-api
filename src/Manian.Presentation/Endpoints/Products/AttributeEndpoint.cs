using Manian.Application.Commands.Products;
using Manian.Application.Queries.Products;
using Manian.Domain.Entities.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Products;

/// <summary>
/// 屬性相關的 API 端點定義類別
/// 
/// 職責：
/// - 定義屬性鍵和屬性值相關的 RESTful API 端點
/// - 處理 HTTP 請求並回傳適當的回應
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// 屬性鍵：
/// - GET /api/attributes/keys/count - 查詢屬性鍵總數
/// - GET /api/attributes/keys - 查詢屬性鍵列表
/// - POST /api/attributes/keys - 新增屬性鍵
/// - PUT /api/attributes/keys - 更新屬性鍵
/// - DELETE /api/attributes/keys - 刪除屬性鍵
/// 
/// 屬性值：
/// - GET /api/attributes/values - 查詢屬性值列表
/// - POST /api/attributes/values - 新增屬性值
/// - PUT /api/attributes/values - 更新屬性值
/// - DELETE /api/attributes/values - 刪除屬性值
/// </summary>
public static class AttributeEndpoint
{
    /// <summary>
    /// 註冊所有屬性相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapAttributes() 即可註冊所有端點
    /// 
    /// 設計考量：
    /// - 使用擴充方法讓端點註冊更模組化
    /// - 每個端點對應一個處理方法，保持單一職責
    /// - 端點命名遵循 RESTful 規範
    /// - 路由設計區分屬性鍵和屬性值
    /// </summary>
    /// <param name="app">IEndpointRouteBuilder 實例，用於註冊端點</param>
    public static void MapAttributes(this IEndpointRouteBuilder app)
    {
        // ========== 屬性鍵端點 ==========
        
        // 查詢屬性鍵總數
        app.MapGet("/api/attributes/keys/count", GetAttributeKeyCountAsync)
            .WithName("GetAttributeKeyCount")
            .WithSummary("查詢屬性鍵總數")
            .WithDescription(@"
                查詢系統中的屬性鍵總數
                
                查詢參數：
                - 無需任何參數
                
                回傳格式：
                - 200 OK：屬性鍵總數（整數）
                
                使用範例：
                - GET /api/attributes/keys/count
                
                效能說明：
                - 優先使用估計數量（查詢 PostgreSQL 系統目錄）
                - 誤差通常在 1-5% 以內
                - 適合儀表板、統計報表等不需要精確數值的場景
            ")
            .WithTags("商品屬性")
            .Produces<int>(StatusCodes.Status200OK);

        // 查詢屬性鍵列表
        app.MapGet("/api/attributes/keys", GetAttributeKeysAsync)
            .WithName("GetAttributeKeys")
            .WithSummary("查詢屬性鍵列表")
            .WithDescription(@"
                查詢系統中的屬性鍵列表
                
                查詢參數：
                - categoryId：類別 ID（可選），用於查詢指定類別關聯的屬性鍵
                
                回傳格式：
                - 200 OK：屬性鍵列表
                
                使用範例：
                - GET /api/attributes/keys
                - GET /api/attributes/keys?categoryId=5
                
                說明：
                - 支援按類別篩選
                - 包含屬性鍵的基本資訊（名稱、是否為銷售屬性等）
            ")
            .WithTags("商品屬性")
            .Produces<IEnumerable<AttributeKey>>(StatusCodes.Status200OK);

        // 新增屬性鍵
        app.MapPost("/api/attributes/keys", AddAttributeKeyAsync)
            .WithName("AddAttributeKey")
            .WithSummary("新增屬性鍵")
            .WithDescription(@"
                新增一個屬性鍵
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含屬性鍵的所有資訊
                
                必填欄位：
                - Name：屬性鍵名稱
                
                可選欄位：
                - ForSales：是否為銷售屬性（預設 false）
                - Description：屬性鍵描述
                
                回傳格式：
                - 201 Created：新增後的屬性鍵資料
                - 400 Bad Request：新增失敗
                
                使用範例：
                POST /api/attributes/keys
                {
                    ""Name"": ""顏色"",
                    ""ForSales"": true,
                    ""Description"": ""商品顏色選項""
                }
            ")
            .WithTags("商品屬性")
            .Produces<AttributeKey>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        // 更新屬性鍵
        app.MapPut("/api/attributes/keys", UpdateAttributeKeyAsync)
            .WithName("UpdateAttributeKey")
            .WithSummary("更新屬性鍵")
            .WithDescription(@"
                更新指定的屬性鍵
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含屬性鍵 ID 和要更新的欄位
                
                必填欄位：
                - Id：屬性鍵 ID
                
                可選欄位：
                - Name：屬性鍵名稱
                - ForSales：是否為銷售屬性
                - Description：屬性鍵描述
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：屬性鍵不存在
                - 400 Bad Request：更新失敗
                
                使用範例：
                PUT /api/attributes/keys
                {
                    ""Id"": 1,
                    ""Name"": ""顏色（已更新）"",
                    ""ForSales"": true
                }
            ")
            .WithTags("商品屬性")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // 刪除屬性鍵
        app.MapDelete("/api/attributes/keys", DeleteAttributeKeyAsync)
            .WithName("DeleteAttributeKey")
            .WithSummary("刪除屬性鍵")
            .WithDescription(@"
                刪除指定的屬性鍵
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含屬性鍵 ID
                
                必填欄位：
                - Id：屬性鍵 ID
                
                回傳格式：
                - 204 No Content：刪除成功
                - 404 Not Found：屬性鍵不存在
                - 400 Bad Request：刪除失敗
                
                使用範例：
                DELETE /api/attributes/keys
                {
                    ""Id"": 1
                }
                
                注意事項：
                - 刪除操作不可逆，建議在 UI 層加入確認對話框
                - 如果屬性鍵有關聯的屬性值，刪除會失敗
                - 如果有產品使用此屬性鍵，可能會導致資料不一致
                - 建議考慮實作軟刪除（標記為已刪除）而非硬刪除
            ")
            .WithTags("商品屬性")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // ========== 屬性值端點 ==========
        
        // 查詢屬性值列表
        app.MapGet("/api/attributes/values", GetAttributeValuesAsync)
            .WithName("GetAttributeValues")
            .WithSummary("查詢屬性值列表")
            .WithDescription(@"
                查詢指定屬性鍵的所有屬性值
                
                查詢參數：
                - id：屬性鍵 ID（必填）
                
                回傳格式：
                - 200 OK：屬性值列表
                - 400 Bad Request：屬性鍵不存在
                
                使用範例：
                - GET /api/attributes/values?id=1
                
                說明：
                - 必須指定屬性鍵 ID
                - 返回該屬性鍵下的所有屬性值
                - 按排序順序（SortOrder）返回
            ")
            .WithTags("商品屬性")
            .Produces<IEnumerable<AttributeValue>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // 新增屬性值
        app.MapPost("/api/attributes/values", AddAttributeValueAsync)
            .WithName("AddAttributeValue")
            .WithSummary("新增屬性值")
            .WithDescription(@"
                為指定屬性鍵新增一個屬性值
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含屬性值的所有資訊
                
                必填欄位：
                - KeyId：屬性鍵 ID
                - Value：屬性值內容
                
                可選欄位：
                - SortOrder：排序順序（預設 0）
                - Description：屬性值描述
                
                回傳格式：
                - 201 Created：新增後的屬性值資料
                - 400 Bad Request：新增失敗
                
                使用範例：
                POST /api/attributes/values
                {
                    ""KeyId"": 1,
                    ""Value"": ""紅色"",
                    ""SortOrder"": 10,
                    ""Description"": ""鮮紅色""
                }
            ")
            .WithTags("商品屬性")
            .Produces<AttributeValue>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        // 更新屬性值
        app.MapPut("/api/attributes/values", UpdateAttributeValueAsync)
            .WithName("UpdateAttributeValue")
            .WithSummary("更新屬性值")
            .WithDescription(@"
                更新指定的屬性值
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含屬性值 ID 和要更新的欄位
                
                必填欄位：
                - Id：屬性值 ID
                
                可選欄位：
                - Value：屬性值內容
                - SortOrder：排序順序
                - Description：屬性值描述
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：屬性值不存在
                - 400 Bad Request：更新失敗
                
                使用範例：
                PUT /api/attributes/values
                {
                    ""Id"": 100,
                    ""Value"": ""紅色（已更新）"",
                    ""SortOrder"": 10
                }
            ")
            .WithTags("商品屬性")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // 刪除屬性值
        app.MapDelete("/api/attributes/values", DeleteAttributeValueAsync)
            .WithName("DeleteAttributeValue")
            .WithSummary("刪除屬性值")
            .WithDescription(@"
                刪除指定的屬性值
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含屬性值 ID
                
                必填欄位：
                - Id：屬性值 ID
                
                回傳格式：
                - 204 No Content：刪除成功
                - 404 Not Found：屬性值不存在
                - 400 Bad Request：刪除失敗
                
                使用範例：
                DELETE /api/attributes/values
                {
                    ""Id"": 100
                }
                
                注意事項：
                - 刪除操作不可逆，建議在 UI 層加入確認對話框
                - 如果有產品使用此屬性值，可能會導致資料不一致
                - 建議考慮實作軟刪除（標記為已刪除）而非硬刪除
            ")
            .WithTags("商品屬性")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    // ========== 屬性鍵處理方法 ==========

    /// <summary>
    /// 查詢屬性鍵總數
    /// 
    /// 請求方式：GET /api/attributes/keys/count
    /// 認證要求：不需要登入
    /// 回應格式：JSON 格式的整數（屬性鍵總數）
    /// 
    /// 執行流程：
    /// 1. 使用 AttributeKeyCountQuery 查詢屬性鍵總數
    /// 2. 回傳屬性鍵總數
    /// 
    /// 錯誤處理：
    /// - 查詢失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">AttributeKeyCountQuery 查詢物件</param>
    /// <returns>包含屬性鍵總數的 JSON 回應</returns>
    private static async Task<IResult> GetAttributeKeyCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeKeyCountQuery query)
    {
        // 使用 Mediator 發送 AttributeKeyCountQuery 查詢
        // Mediator 會自動找到對應的 Handler (AttributeKeyCountQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和屬性鍵總數
        return Results.Ok(result);
    }

    /// <summary>
    /// 查詢屬性鍵列表
    /// 
    /// 請求方式：GET /api/attributes/keys
    /// 認證要求：不需要登入
    /// 請求參數：
    /// - categoryId: 類別 ID（可選）
    /// 回應格式：JSON 格式的 AttributeKey 集合
    /// 
    /// 執行流程：
    /// 1. 接收查詢參數
    /// 2. 使用 AttributeKeysQuery 查詢屬性鍵列表
    /// 3. 回傳屬性鍵列表
    /// 
    /// 錯誤處理：
    /// - 查詢失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">AttributeKeysQuery 查詢物件</param>
    /// <returns>包含屬性鍵列表的 JSON 回應</returns>
    private static async Task<IResult> GetAttributeKeysAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeKeysQuery query)
    {
        // 使用 Mediator 發送 AttributeKeysQuery 查詢
        // Mediator 會自動找到對應的 Handler (AttributeKeysQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和屬性鍵列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增屬性鍵
    /// 
    /// 請求方式：POST /api/attributes/keys
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 AttributeKeyAddCommand
    /// 回應格式：JSON 格式的 AttributeKey（包含資料庫自動生成的欄位）
    /// 
    /// 執行流程：
    /// 1. 接收屬性鍵資料
    /// 2. 使用 AttributeKeyAddCommand 新增屬性鍵
    /// 3. 回傳新增後的屬性鍵資料
    /// 
    /// 錯誤處理：
    /// - 新增失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="command">AttributeKeyAddCommand 命令物件</param>
    /// <returns>包含新增後屬性鍵資料的 JSON 回應</returns>
    private static async Task<IResult> AddAttributeKeyAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeKeyAddCommand command)
    {
        // 使用 Mediator 發送 AttributeKeyAddCommand 命令
        // Mediator 會自動找到對應的 Handler (AttributeKeyAddHandler)
        var result = await mediator.SendAsync(command);
        
        // 回傳 201 Created 狀態碼和新增後的屬性鍵資料
        // 第二個參數是資源的位置（Location header）
        return Results.Created($"/api/attributes/keys/{result.Id}", result);
    }

    /// <summary>
    /// 更新屬性鍵
    /// 
    /// 請求方式：PUT /api/attributes/keys
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 AttributeKeyUpdateCommand
    /// 回應格式：JSON 格式的 AttributeKey（更新後的資料）
    /// 
    /// 執行流程：
    /// 1. 接收屬性鍵資料（包含 ID）
    /// 2. 使用 AttributeKeyUpdateCommand 更新屬性鍵
    /// 3. 回傳更新後的屬性鍵資料
    /// 
    /// 錯誤處理：
    /// - 屬性鍵不存在：回傳 404 Not Found
    /// - 更新失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="command">AttributeKeyUpdateCommand 命令物件</param>
    /// <returns>包含更新後屬性鍵資料的 JSON 回應</returns>
    private static async Task<IResult> UpdateAttributeKeyAsync(
        [FromServices] IMediator mediator,
        [FromBody] AttributeKeyUpdateCommand command)
    {
        // 使用 Mediator 發送 AttributeKeyUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (AttributeKeyUpdateHandler)
        await mediator.SendAsync(command);
        
        // 回傳 200 OK
        return Results.Ok();
    }

    /// <summary>
    /// 刪除屬性鍵
    /// 
    /// 請求方式：DELETE /api/attributes/keys
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 AttributeKeyDeleteCommand
    /// 回應格式：204 No Content
    /// 
    /// 執行流程：
    /// 1. 接收屬性鍵資料（包含 ID）
    /// 2. 使用 AttributeKeyDeleteCommand 刪除屬性鍵
    /// 3. 回傳 204 No Content
    /// 
    /// 錯誤處理：
    /// - 屬性鍵不存在：回傳 404 Not Found
    /// - 刪除失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="command">AttributeKeyDeleteCommand 命令物件</param>
    /// <returns>204 No Content 狀態碼</returns>
    private static async Task<IResult> DeleteAttributeKeyAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeKeyDeleteCommand command)
    {
        // 使用 Mediator 發送 AttributeKeyDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (AttributeKeyDeleteHandler)
        await mediator.SendAsync(command);
        
        // 回傳 204 No Content 狀態碼
        return Results.NoContent();
    }

    // ========== 屬性值處理方法 ==========

    /// <summary>
    /// 查詢屬性值列表
    /// 
    /// 請求方式：GET /api/attributes/values
    /// 認證要求：不需要登入
    /// 請求參數：
    /// - id: 屬性鍵 ID（必填）
    /// 回應格式：JSON 格式的 AttributeValue 集合
    /// 
    /// 執行流程：
    /// 1. 接收屬性鍵 ID
    /// 2. 使用 AttributeValuesQuery 查詢屬性值列表
    /// 3. 回傳屬性值列表
    /// 
    /// 錯誤處理：
    /// - 屬性鍵不存在：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">AttributeValuesQuery 查詢物件</param>
    /// <returns>包含屬性值列表的 JSON 回應</returns>
    private static async Task<IResult> GetAttributeValuesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeValuesQuery query)
    {
        // 使用 Mediator 發送 AttributeValuesQuery 查詢
        // Mediator 會自動找到對應的 Handler (AttributeValuesQueryHandler)
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和屬性值列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 新增屬性值
    /// 
    /// 請求方式：POST /api/attributes/values
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 AttributeValueAddCommand
    /// 回應格式：JSON 格式的 AttributeValue（包含資料庫自動生成的欄位）
    /// 
    /// 執行流程：
    /// 1. 接收屬性值資料
    /// 2. 使用 AttributeValueAddCommand 新增屬性值
    /// 3. 回傳新增後的屬性值資料
    /// 
    /// 錯誤處理：
    /// - 新增失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送新增命令</param>
    /// <param name="command">AttributeValueAddCommand 命令物件</param>
    /// <returns>包含新增後屬性值資料的 JSON 回應</returns>
    private static async Task<IResult> AddAttributeValueAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeValueAddCommand command)
    {
        // 使用 Mediator 發送 AttributeValueAddCommand 命令
        // Mediator 會自動找到對應的 Handler (AttributeValueAddHandler)
        var result = await mediator.SendAsync(command);
        
        // 回傳 201 Created 狀態碼和新增後的屬性值資料
        // 第二個參數是資源的位置（Location header）
        return Results.Created($"/api/attributes/values/{result.Id}", result);
    }

    /// <summary>
    /// 更新屬性值
    /// 
    /// 請求方式：PUT /api/attributes/values
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 AttributeValueUpdateCommand
    /// 回應格式：JSON 格式的 AttributeValue（更新後的資料）
    /// 
    /// 執行流程：
    /// 1. 接收屬性值資料（包含 ID）
    /// 2. 使用 AttributeValueUpdateCommand 更新屬性值
    /// 3. 回傳更新後的屬性值資料
    /// 
    /// 錯誤處理：
    /// - 屬性值不存在：回傳 404 Not Found
    /// - 更新失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="command">AttributeValueUpdateCommand 命令物件</param>
    /// <returns>包含更新後屬性值資料的 JSON 回應</returns>
    private static async Task<IResult> UpdateAttributeValueAsync(
        [FromServices] IMediator mediator,
        [FromBody] AttributeValueUpdateCommand command)
    {
        // 使用 Mediator 發送 AttributeValueUpdateCommand 命令
        // Mediator 會自動找到對應的 Handler (AttributeValueUpdateHandler)
        await mediator.SendAsync(command);
        
        // 回傳 200 OK
        return Results.Ok();
    }

    /// <summary>
    /// 刪除屬性值
    /// 
    /// 請求方式：DELETE /api/attributes/values
    /// 認證要求：需要登入（建議限制為管理員）
    /// 請求格式：JSON 格式的 AttributeValueDeleteCommand
    /// 回應格式：204 No Content
    /// 
    /// 執行流程：
    /// 1. 接收屬性值資料（包含 ID）
    /// 2. 使用 AttributeValueDeleteCommand 刪除屬性值
    /// 3. 回傳 204 No Content
    /// 
    /// 錯誤處理：
    /// - 屬性值不存在：回傳 404 Not Found
    /// - 刪除失敗：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送刪除命令</param>
    /// <param name="command">AttributeValueDeleteCommand 命令物件</param>
    /// <returns>204 No Content 狀態碼</returns>
    private static async Task<IResult> DeleteAttributeValueAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AttributeValueDeleteCommand command)
    {
        // 使用 Mediator 發送 AttributeValueDeleteCommand 命令
        // Mediator 會自動找到對應的 Handler (AttributeValueDeleteHandler)
        await mediator.SendAsync(command);
        
        // 回傳 204 No Content 狀態碼
        return Results.NoContent();
    }
}
