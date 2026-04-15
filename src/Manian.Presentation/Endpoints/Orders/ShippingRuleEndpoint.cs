using Manian.Application.Commands.Orders;
using Manian.Application.Models;
using Manian.Application.Queries.Orders;
using Manian.Domain.Entities.Orders;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Orders;

/// <summary>
/// 運費規則相關的 API 端點定義
/// 
/// 職責：
/// - 定義運費規則相關的 API 端點
/// - 處理運費規則的查詢和命令請求
/// 
/// 設計模式：
/// - Minimal API：使用 ASP.NET Core 的 Minimal API 風格
/// - CQRS：透過 Mediator 分發查詢和命令
/// - 依賴注入：注入所需的服務
/// 
/// 架構位置：
/// - 位於 Presentation 層（展示層）
/// - 負責處理 HTTP 請求和回應
/// - 不包含業務邏輯，只負責路由和參數處理
/// 
/// 路由設計：
/// - 基礎路徑：/api/shipping-rules
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）、DELETE（刪除）
/// 
/// 使用場景：
/// - 運費規則管理頁面
/// - 運費規則資料編輯
/// - 運費規則資訊查詢
/// </summary>
public static class ShippingRuleEndpoint
{
    /// <summary>
    /// 註冊運費規則相關的 API 端點
    /// 
    /// 職責：
    /// - 定義 API 路由
    /// - 處理 HTTP 請求
    /// - 呼叫 Mediator 分發請求
    /// 
    /// 設計考量：
    /// - 使用擴充方法模式，方便在 Program.cs 中呼叫
    /// - 所有端點都使用 Mediator 分發請求
    /// - 遵循 RESTful API 設計原則
    /// 
    /// 註冊方式：
    /// 在 Program.cs 中呼叫：
    /// <code>
    /// app.MapShippingRuleEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapShippingRules(this WebApplication app)
    {
        // =========================================================================
        // GET /api/shipping-rules - 查詢運費規則列表
        // =========================================================================

        // 定義 GET 端點，路由為 /api/shipping-rules
        app.MapGet("/api/shipping-rules", HandleGetShippingRulesAsync)

        // 設定端點摘要
        .WithSummary("查詢運費規則列表")

        // 設定端點描述
        .WithDescription(@"
            查詢系統中的運費規則列表
            
            查詢參數：
            - isActive：是否啟用（可選）
            - search：搜尋關鍵字（可選），會在 Name、Description 中搜尋
            - cursor：游標（可選），用於分頁
            - size：每頁資料筆數（可選），預設 20
            
            回傳格式：
            - 200 OK：運費規則列表
            
            使用範例：
            - GET /api/shipping-rules
            - GET /api/shipping-rules?isActive=true
            - GET /api/shipping-rules?search=免運
            - GET /api/shipping-rules?size=50
            
            說明：
            - 支援分頁查詢
            - 預設按優先級排序
        ")

        // 設定端點標籤
        .WithTags("運費規則管理")

        // 產生 OpenAPI 回應定義
        .Produces<Pagination<ShippingRule>>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/shipping-rules - 新增運費規則
        // =========================================================================

        // 定義 POST 端點，路由為 /api/shipping-rules
        app.MapPost("/api/shipping-rules", HandleAddShippingRuleAsync)

        // 設定端點摘要
        .WithSummary("新增運費規則")

        // 設定端點描述
        .WithDescription(@"
            新增一個運費規則
            
            請求內容：
            - ShippingRuleAddCommand：運費規則資料（必填）
            
            回傳格式：
            - 200 OK：新增後的運費規則資料（ShippingRule）
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - POST /api/shipping-rules
            {
                ""name"": ""滿千免運"",
                ""description"": ""訂單金額滿 1000 元免運費"",
                ""condition"": {
                    ""ruleType"": ""amount"",
                    ""minAmount"": 1000
                },
                ""shippingFee"": 0,
                ""isActive"": true
            }
        ")

        // 設定端點標籤
        .WithTags("運費規則管理")

        // 產生 OpenAPI 回應定義
        .Produces<ShippingRule>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // PUT /api/shipping-rules - 更新運費規則
        // =========================================================================

        // 定義 PUT 端點，路由為 /api/shipping-rules
        app.MapPut("/api/shipping-rules", HandleUpdateShippingRuleAsync)

        // 設定端點摘要
        .WithSummary("更新運費規則")

        // 設定端點描述
        .WithDescription(@"
            更新現有的運費規則資料
            
            請求內容：
            - ShippingRuleUpdateCommand：運費規則資料（必填）
            
            回傳格式：
            - 200 OK：更新成功
            - 404 Not Found：運費規則不存在
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - PUT /api/shipping-rules
            {
                ""id"": 1,
                ""name"": ""滿千免運"",
                ""isActive"": false
            }
        ")

        // 設定端點標籤
        .WithTags("運費規則管理")

        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // DELETE /api/shipping-rules - 刪除運費規則
        // =========================================================================

        // 定義 DELETE 端點，路由為 /api/shipping-rules
        app.MapDelete("/api/shipping-rules", HandleDeleteShippingRuleAsync)

        // 設定端點摘要
        .WithSummary("刪除運費規則")

        // 設定端點描述
        .WithDescription(@"
            刪除指定的運費規則
            
            請求內容：
            - ShippingRuleDeleteCommand：運費規則 ID（必填）
            
            回傳格式：
            - 204 No Content：刪除成功
            - 404 Not Found：運費規則不存在
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - DELETE /api/shipping-rules?id=1
        ")

        // 設定端點標籤
        .WithTags("運費規則管理")

        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // POST /api/shipping-rules/fee - 計算運費
        // =========================================================================
        
        app.MapPost("/api/shipping-rules/fee", HandleCalculateShippingFeeAsync)
        .WithSummary("計算運費")
        .WithDescription(@"
            根據訂單項目計算運費
            
            請求內容：
            - ShippingFeeQuery：訂單項目資料（必填）
            
            回傳格式：
            - 200 OK：計算後的運費金額（decimal）
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - POST /api/shipping-rules/fee
            {
                ""orderItems"": [
                    {
                        ""skuId"": 1001,
                        ""quantity"": 2,
                        ""unitPrice"": 100
                    },
                    {
                        ""skuId"": 1002,
                        ""quantity"": 1,
                        ""unitPrice"": 200
                    }
                ]
            }
        ")
        .WithTags("運費規則")
        .Produces<decimal>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    // ===== Handler 方法 =====

    /// <summary>
    /// 處理查詢運費規則列表請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發查詢
    /// - 回傳查詢結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發查詢請求
    /// 2. 回傳查詢結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發查詢請求</param>
    /// <param name="query">運費規則查詢請求物件，包含篩選和分頁參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：運費規則列表
    /// </returns>
    private static async Task<IResult> HandleGetShippingRulesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ShippingRulesQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（ShippingRulesQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和運費規則列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增運費規則請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發命令
    /// - 回傳新增結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發命令請求
    /// 2. 回傳新增結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">新增運費規則命令物件（包含運費規則資料）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：新增後的運費規則資料（ShippingRule）
    /// </returns>
    private static async Task<IResult> HandleAddShippingRuleAsync(
        [FromServices] IMediator mediator,
        [FromBody] ShippingRuleAddCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（ShippingRuleAddHandler）
        // Handler 會執行新增並回傳結果
        var result = await mediator.SendAsync(command);
        
        // ========== 第二步：回傳新增結果 ==========
        // 回傳 200 OK 狀態碼和新增後的運費規則資料
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新運費規則請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發命令
    /// - 回傳更新結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發命令請求
    /// 2. 回傳更新結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">更新運費規則命令物件（包含運費規則資料）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：更新成功
    /// </returns>
    private static async Task<IResult> HandleUpdateShippingRuleAsync(
        [FromServices] IMediator mediator,
        [FromBody] ShippingRuleUpdateCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（ShippingRuleUpdateHandler）
        // Handler 會執行更新並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳更新結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 處理刪除運費規則請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發命令
    /// - 回傳刪除結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發命令請求
    /// 2. 回傳刪除結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">刪除運費規則命令物件（包含運費規則 ID）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 204 No Content：刪除成功
    /// </returns>
    private static async Task<IResult> HandleDeleteShippingRuleAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ShippingRuleDeleteCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（ShippingRuleDeleteHandler）
        // Handler 會執行刪除並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳刪除結果 ==========
        // 回傳 204 No Content 狀態碼
        return Results.NoContent();
    }

    /// <summary>
    /// 處理計算運費請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發查詢
    /// - 回傳計算結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發查詢請求
    /// 2. 回傳計算結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發查詢請求</param>
    /// <param name="query">運費查詢請求物件，包含 OrderItems</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：計算後的運費金額
    /// </returns>
    private static async Task<IResult> HandleCalculateShippingFeeAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ShippingFeeQuery query)
    {
        // 透過 Mediator 分發查詢
        var shippingFee = await mediator.SendAsync(query);
        
        // 回傳計算結果
        return Results.Ok(shippingFee);
    }    
}
