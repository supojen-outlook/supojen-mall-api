using System.Text.Json;
using Manian.Application.Commands.Orders;
using Manian.Application.Models;
using Manian.Application.Queries.Orders;
using Manian.Domain.Entities.Orders;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Orders;

/// <summary>
/// 訂單管理 API 端點
/// 
/// 職責：
/// - 定義訂單相關的 API 端點
/// - 處理訂單的查詢和命令請求
/// - 包含訂單、付款、物流、退貨等相關端點
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
/// - 基礎路徑：/api/orders
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）
/// 
/// 使用場景：
/// - 訂單管理頁面
/// - 訂單詳情頁面
/// - 付款處理頁面
/// - 物流追蹤頁面
/// - 退貨處理頁面
/// </summary>
public static class OrderEndpoint
{
    /// <summary>
    /// 註冊訂單相關的 API 端點
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
    /// app.MapOrderEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapOrders(this WebApplication app)
    {
        // =========================================================================
        // 訂單查詢端點 (Order Query Endpoints)
        // =========================================================================

        // GET /api/orders - 查詢訂單列表
        app.MapGet("/api/orders", HandleGetOrdersAsync)
            .WithSummary("查詢訂單列表")
            .WithDescription(@"
                查詢系統中的訂單列表
                
                查詢參數：
                - search：搜尋關鍵字（可選），會在 OrderNumber 中搜尋
                - cursor：游標（可選），用於分頁
                - size：每頁資料筆數（可選），預設 20
                - status：訂單狀態（可選）
                
                回傳格式：
                - 200 OK：訂單列表
                
                使用範例：
                - GET /api/orders
                - GET /api/orders?search=ORD-001
                - GET /api/orders?status=pending
                - GET /api/orders?size=50
            ")
            .WithTags("訂單管理")
            .Produces<Pagination<Order>>(StatusCodes.Status200OK);

        // GET /api/my-orders - 查詢當前登入用戶的訂單列表
        app.MapGet("/api/my-orders", HandleGetMyOrdersAsync)
            .WithSummary("查詢當前登入用戶的訂單列表")
            .WithDescription(@"
                查詢當前登入用戶的訂單列表，支援多種篩選條件和分頁功能。
                
                查詢參數：
                - status：訂單狀態（可選）
                  * pending：待處理
                  * paid：已付款
                  * shipped：已出貨
                  * completed：已完成
                  * cancelled：已取消
                
                - search：搜尋關鍵字（可選），搜尋訂單編號
                
                - startDate：開始日期（可選），篩選此日期之後的訂單
                
                - endDate：結束日期（可選），篩選此日期之前的訂單
                
                - cursor：游標（可選），用於分頁
                
                - size：每頁資料筆數（可選），預設 20
                
                回傳格式：
                - 200 OK：訂單列表（分頁模型）
                
                使用範例：
                - GET /api/orders/my
                - GET /api/orders/my?status=paid
                - GET /api/orders/my?search=2023
                - GET /api/orders/my?startDate=2023-01-01&endDate=2023-12-31
                - GET /api/orders/my?cursor=100&size=50
            ")
            .WithTags("訂單管理")
            .Produces<Pagination<Order>>(StatusCodes.Status200OK);

        // GET /api/orders/{id}/items - 查詢訂單項目
        app.MapGet("/api/orders/items", HandleGetOrderItemsAsync)
            .WithSummary("查詢訂單項目")
            .WithDescription(@"
                查詢指定訂單的所有項目
                
                路徑參數：
                - id：訂單 ID
                
                回傳格式：
                - 200 OK：訂單項目列表
                - 404 Not Found：訂單不存在
                
                使用範例：
                - GET /api/orders/1001/items
            ")
            .WithTags("訂單管理")
            .Produces<IEnumerable<OrderItem>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // 付款相關端點 (Payment Endpoints)
        // =========================================================================

        // GET /api/orders/payment - 查詢付款記錄
        app.MapGet("/api/orders/payment", HandleGetPaymentAsync)
            .WithSummary("查詢付款記錄")
            .WithDescription(@"
                查詢指定訂單的付款記錄
                
                路徑參數：
                - id：訂單 ID
                
                回傳格式：
                - 200 OK：付款記錄
                - 404 Not Found：付款記錄不存在
                
                使用範例：
                - GET /api/orders/1001/payment
            ")
            .WithTags("訂單管理")
            .Produces<Payment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // 物流相關端點 (Shipment Endpoints)
        // =========================================================================

        // GET /api/orders/shipment - 查詢物流記錄
        app.MapGet("/api/orders/shipment", HandleGetShipmentAsync)
            .WithSummary("查詢物流記錄")
            .WithDescription(@"
                查詢指定訂單的物流記錄
                
                路徑參數：
                - id：訂單 ID
                
                回傳格式：
                - 200 OK：物流記錄
                - 404 Not Found：物流記錄不存在
                
                使用範例：
                - GET /api/orders/1001/shipment
            ")
            .WithTags("訂單管理")
            .Produces<Shipment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // PUT /api/orders/shipment - 更新物流記錄
        app.MapPut("/api/orders/shipment", HandleUpdateShipmentAsync)
            .WithSummary("更新物流記錄")
            .WithDescription(@"
                更新指定訂單的物流記錄
                
                路徑參數：
                - id：訂單 ID
                
                請求內容：
                - ShipmentUpdateCommand：物流資料（必填）
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：訂單或物流記錄不存在
                - 400 Bad Request：請求內容錯誤
                
                使用範例：
                - PUT /api/orders/1001/shipment
                {
                    ""method"": ""tcat"",
                    ""trackingNumber"": ""TCAT123456789"",
                    ""recipientName"": ""張三"",
                    ""recipientPhone"": ""0912345678"",
                    ""shippingAddress"": ""台北市信義區信義路五段7號""
                }
            ")
            .WithTags("訂單管理")
            .Produces<Shipment>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // PUT /api/orders/my-shipment - 更新我的訂單物流記錄（客戶端）
        app.MapPut("/api/orders/my-shipment", HandleUpdateMyShipmentAsync)
            .WithSummary("更新我的訂單物流記錄")
            .WithDescription(@"
                更新當前登入用戶訂單的到貨日期
                
                請求參數：
                - orderId：訂單 ID（必填）
                - deliveredDate：到貨日期（可選）
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：訂單不存在
                - 400 Bad Request：請求內容錯誤
                
                使用範例：
                - PUT /api/orders/my-shipment?orderId=1001&deliveredDate=2023-12-01T10:00:00Z
            ")
            .WithTags("訂單管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // 訂單命令端點 (Order Command Endpoints)
        // =========================================================================

        // POST /api/orders - 新增訂單
        app.MapPost("/api/orders", HandleAddOrderAsync)
            .WithSummary("新增訂單")
            .WithDescription(@"
                新增一個訂單
                
                請求內容：
                - OrderAddCommand：訂單資料（必填）
                
                回傳格式：
                - 200 OK：新增後的訂單資料（Order）
                - 400 Bad Request：請求內容錯誤
                
                使用範例：
                - POST /api/orders
                {
                    ""userId"": 1,
                    ""items"": [
                        {
                            ""productId"": 1001,
                            ""skuId"": 2001,
                            ""quantity"": 2
                        }
                    ]
                }
            ")
            .WithTags("訂單管理")
            .Produces<Order>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // PUT /api/orders - 更新訂單
        app.MapPut("/api/orders", HandleUpdateOrderAsync)
            .WithSummary("更新訂單")
            .WithDescription(@"
                更新現有的訂單資料
                
                請求內容：
                - OrderUpdateCommand：訂單資料（必填）
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：訂單不存在
                - 400 Bad Request：請求內容錯誤
                
                使用範例：
                - PUT /api/orders
                {
                    ""id"": 1001,
                    ""status"": ""shipped"",
                    ""notes"": ""已出貨""
                }
            ")
            .WithTags("訂單管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    


    // =========================================================================
    // 訂單揀貨相關端點 (Order Pick Endpoints)
    // =========================================================================
    
    // GET /api/orders/pick-items - 查詢揀貨項目
    app.MapGet("/api/orders/pick-items", HandleGetPickItemsAsync)
        .WithSummary("查詢揀貨項目")
        .WithDescription(@"
            查詢指定訂單的所有揀貨項目
            
            查詢參數：
            - orderId：訂單 ID（必填）
            
            回傳格式：
            - 200 OK：揀貨項目列表
            - 404 Not Found：訂單不存在
            
            使用範例：
            - GET /api/orders/pick-items?orderId=1001
        ")
        .WithTags("訂單管理")
        .Produces<Pagination<PickItem>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

    }

    // =========================================================================
    // Handler 方法 (Handler Methods)
    // =========================================================================

    /// <summary>
    /// 處理查詢訂單列表請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetOrdersAsync(
        [FromServices] IMediator mediator,
        [AsParameters] OrdersQuery query)
    {
        var result = await mediator.SendAsync(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理查詢當前登入用戶訂單列表請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetMyOrdersAsync(
        [FromServices] IMediator mediator,
        [AsParameters] MyOrdersQuery query)
    {
        // 透過 Mediator 分發查詢
        var result = await mediator.SendAsync(query);
        
        // 回傳查詢結果
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理查詢訂單項目請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetOrderItemsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] OrderItemsQuery query)
    {
        var result = await mediator.SendAsync(query);
        if (result == null) return Results.NotFound();
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理查詢付款記錄請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetPaymentAsync(
        [FromServices] IMediator mediator,
        [AsParameters] PaymentQuery query)
    {
        var result = await mediator.SendAsync(query);
        if (result == null) return Results.NotFound();
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理查詢物流記錄請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetShipmentAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ShipmentQuery query)
    {
        var result = await mediator.SendAsync(query);
        if (result == null) return Results.NotFound();
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新物流記錄請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleUpdateShipmentAsync(
        [FromServices] IMediator mediator,
        [FromBody] ShipmentUpdateCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 客戶處理更新物流記錄請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleUpdateMyShipmentAsync(
        [FromServices] IMediator mediator,
        [FromBody]JsonElement body)
    {        
        await mediator.SendAsync(new ShipmentUpdateCommand()
        {
            OrderId = body.GetProperty("orderId").GetInt32(),
            DeliveredDate = body.GetProperty("deliveredDate").GetDateTimeOffset()
        });
        return Results.Ok();
    }

    /// <summary>
    /// 處理新增訂單請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleAddOrderAsync(
        [FromServices] IMediator mediator,
        [FromBody] OrderAddCommand command)
    {
        var result = await mediator.SendAsync(command);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新訂單請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleUpdateOrderAsync(
        [FromServices] IMediator mediator,
        [FromBody] OrderUpdateCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 處理查詢取貨項目請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetPickItemsAsync(
    [FromServices] IMediator mediator,
    [AsParameters] PickItemsQuery query)
{
    var result = await mediator.SendAsync(query);
    if (result == null) return Results.NotFound();
    return Results.Ok(result);
}
}
