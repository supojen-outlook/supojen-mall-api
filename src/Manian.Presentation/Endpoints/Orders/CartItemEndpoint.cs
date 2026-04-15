using Manian.Application.Commands.Carts;
using Manian.Application.Models;
using Manian.Application.Queries.Carts;
using Manian.Domain.Entities.Carts;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Orders;

/// <summary>
/// 購物車項目 API 端點
/// 
/// 職責：
/// - 定義購物車項目相關的 API 端點
/// - 處理購物車項目的查詢和命令請求
/// 
/// 設計模式：
/// - Minimal API：使用 ASP.NET Core 的 Minimal API 風格
/// - CQRS：透過 Mediator 分發查詢和命令請求
/// - 依賴注入：注入所需的服務
/// 
/// 架構位置：
/// - 位於 Presentation 層（展示層）
/// - 負責處理 HTTP 請求和回應
/// - 不包含業務邏輯，只負責路由和參數處理
/// 
/// 路由設計：
/// - 基礎路徑：/api/cart-items
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）、DELETE（刪除）
/// 
/// 使用場景：
/// - 購物車頁面顯示購物車項目
/// - 商品詳情頁加入購物車
/// - 購物車頁面修改數量
/// - 購物車頁面刪除項目
/// </summary>
public static class CartItemEndpoint
{
    /// <summary>
    /// 註冊購物車項目相關的 API 端點
    /// 
    /// 職責：
    /// - 定義 API 路由
    /// - 處理 HTTP 請求
    /// - 呼叫 Mediator 分發查詢和命令
    /// 
    /// 設計考量：
    /// - 使用擴充方法模式，方便在 Program.cs 中呼叫
    /// - 所有端點都使用 Mediator 分發請求
    /// - 遵循 RESTful API 設計原則
    /// 
    /// 註冊方式：
    /// 在 Program.cs 中呼叫：
    /// <code>
    /// app.MapCartItemEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapCartItems(this WebApplication app)
    {
        // =========================================================================
        // GET /api/cart-items - 查詢購物車項目列表
        // =========================================================================
        
        // 定義 GET 端點，路由為 /api/cart-items
        app.MapGet("/api/cart-items", HandleGetCartItemsAsync)
        .WithSummary("查詢購物車項目列表")
        .WithDescription(@"
            查詢當前使用者的購物車項目列表
            
            查詢參數：
            - type：購物車類型（可選），可選值：shopping（購物車）、wishlist（願望清單），預設為 shopping
            - cursor：游標（可選），用於分頁
            - size：每頁資料筆數（可選），預設 20
            
            回傳格式：
            - 200 OK：購物車項目列表（分頁）
            
            使用範例：
            - GET /api/cart-items
            - GET /api/cart-items?type=shopping
            - GET /api/cart-items?type=wishlist&size=10
            
            說明：
            - 只回傳當前登入使用者的購物車項目
            - 支援游標分頁
            - 預設按建立時間排序
        ")
        .WithTags("購物車管理")
        .RequireAuthorization()
        .Produces<Pagination<CartItem>>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/cart-items - 新增購物車項目
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/cart-items
        app.MapPost("/api/cart-items", HandleAddCartItemAsync)
        .WithSummary("新增購物車項目")
        .WithDescription(@"
            新增一個購物車項目
            
            請求格式：
            - JSON 格式的 CartItemAddCommand
            
            回傳格式：
            - 200 OK：新增的購物車項目
            - 400 Bad Request：請求參數錯誤
            
            使用範例：
            - POST /api/cart-items
            
            說明：
            - 如果購物車中已存在相同 SKU 的項目，會更新數量
            - 會快照商品資訊（名稱、價格、屬性等）
            - 預設加入購物車（type=shopping）
        ")
        .WithTags("購物車管理")
        .RequireAuthorization()
        .Produces<CartItem>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // PUT /api/cart-items - 更新購物車項目
        // =========================================================================
        
        // 定義 PUT 端點，路由為 /api/cart-items
        app.MapPut("/api/cart-items", HandleUpdateCartItemAsync)
        .WithSummary("更新購物車項目")
        .WithDescription(@"
            更新現有的購物車項目資訊
            
            請求格式：
            - JSON 格式的 CartItemUpdateCommand
            
            回傳格式：
            - 200 OK：更新成功
            - 404 Not Found：購物車項目不存在
            
            使用範例：
            - PUT /api/cart-items
            
            說明：
            - 可以更新購物車項目的數量
            - 可以在購物車和願望清單之間切換
            - 數量必須大於 0
        ")
        .WithTags("購物車管理")
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // DELETE /api/cart-items - 刪除購物車項目
        // =========================================================================
        
        // 定義 DELETE 端點，路由為 /api/cart-items
        app.MapDelete("/api/cart-items", HandleDeleteCartItemAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("刪除購物車項目")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            刪除指定的購物車項目
            
            請求格式：
            - JSON 格式的 CartItemDeleteCommand
            
            回傳格式：
            - 200 OK：刪除成功
            - 404 Not Found：購物車項目不存在
            
            使用範例：
            - DELETE /api/cart-items
            
            說明：
            - 刪除操作不可逆
            - 建議在 UI 層加入確認對話框
            - 不會影響商品庫存
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("購物車管理")
        
        // 設定需要認證
        .RequireAuthorization()
        
        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }

    // =========================================================================
    // Handler 方法
    // =========================================================================

    /// <summary>
    /// 處理查詢購物車項目列表請求的私有方法
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
    /// <param name="query">購物車項目查詢請求物件，包含類型和分頁參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：購物車項目列表（分頁）
    /// </returns>
    private static async Task<IResult> HandleGetCartItemsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CartItemsQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（CartItemsQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和購物車項目列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增購物車項目請求的私有方法
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
    /// <param name="command">新增購物車項目命令物件，包含購物車項目的所有資訊</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：新增的購物車項目
    /// - 400 Bad Request：請求參數錯誤
    /// </returns>
    private static async Task<IResult> HandleAddCartItemAsync(
        [FromServices] IMediator mediator,
        [FromBody] CartItemAddCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（CartItemAddHandler）
        // Handler 會執行新增邏輯並回傳結果
        var result = await mediator.SendAsync(command);
        
        // ========== 第二步：回傳新增結果 ==========
        // 回傳 200 OK 狀態碼和新增的購物車項目
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新購物車項目請求的私有方法
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
    /// <param name="command">更新購物車項目命令物件，包含購物車項目的所有資訊</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：更新成功
    /// - 404 Not Found：購物車項目不存在
    /// </returns>
    private static async Task<IResult> HandleUpdateCartItemAsync(
        [FromServices] IMediator mediator,
        [FromBody] CartItemUpdateCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（CartItemUpdateHandler）
        // Handler 會執行更新邏輯並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳更新結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 處理刪除購物車項目請求的私有方法
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
    /// <param name="command">刪除購物車項目命令物件，包含購物車項目的所有資訊</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：刪除成功
    /// - 404 Not Found：購物車項目不存在
    /// </returns>
    private static async Task<IResult> HandleDeleteCartItemAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CartItemDeleteCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（CartItemDeleteHandler）
        // Handler 會執行刪除邏輯並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳刪除結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }
}
