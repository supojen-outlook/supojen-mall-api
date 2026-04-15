using Manian.Application.Commands.Promotions;
using Manian.Application.Models;
using Manian.Application.Queries.Promotions;
using Manian.Domain.Entities.Promotions;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Promotions;

/// <summary>
/// 促銷活動 API 端點
/// 
/// 職責：
/// - 定義促銷活動相關的 API 端點
/// - 處理促銷活動的查詢和命令請求
/// - 處理促銷規則和促銷範圍的相關請求
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
/// - 基礎路徑：/api/promotions
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）、DELETE（刪除）
/// - 規則路徑：/api/promotions/rules（扁平化設計）
/// - 範圍路徑：/api/promotions/scopes（扁平化設計）
/// 
/// 使用場景：
/// - 促銷活動管理頁面
/// - 促銷規則管理功能
/// - 促銷範圍管理功能
/// - 儀表板顯示促銷活動統計
/// </summary>
public static class PromotionEndpoint
{
    /// <summary>
    /// 註冊促銷活動相關的 API 端點
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
    /// - 規則和範圍採用扁平化路由設計
    /// 
    /// 註冊方式：
    /// 在 Program.cs 中呼叫：
    /// <code>
    /// app.MapPromotionEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapPromotions(this WebApplication app)
    {
        // =========================================================================
        // 促銷活動主端點
        // =========================================================================

        // 定義 GET 端點，路由為 /api/promotions
        app.MapGet("/api/promotions", HandleGetPromotionsAsync)
            .WithSummary("查詢促銷活動列表")
            .WithDescription(@"
                查詢系統中的促銷活動列表
                
                查詢參數：
                - status：促銷活動狀態（可選），可選值：draft（草稿）、active（啟用）、expired（過期）、disabled（停用）
                - cursor：游標（可選），用於分頁
                - size：每頁資料筆數（可選），預設 20
                
                回傳格式：
                - 200 OK：促銷活動列表（分頁）
                
                使用範例：
                - GET /api/promotions
                - GET /api/promotions?status=active
                - GET /api/promotions?size=10
                
                說明：
                - 支援游標分頁
                - 預設按建立時間排序
            ")
            .WithTags("促銷管理")
            .Produces<Pagination<Promotion>>(StatusCodes.Status200OK);

        // 定義 GET 端點，路由為 /api/promotions/count
        app.MapGet("/api/promotions/count", HandleGetPromotionCountAsync)
            .WithSummary("統計促銷活動數量")
            .WithDescription(@"
                統計符合條件的促銷活動總數量
                
                查詢參數：
                - 無
                
                回傳格式：
                - 200 OK：促銷活動總數
                
                使用範例：
                - GET /api/promotions/count
                
                說明：
                - 優先使用估計數量，提升效能
                - 如果估計數量不可用，執行精確計數
            ")
            .WithTags("促銷管理")
            .Produces<int>(StatusCodes.Status200OK);

        // 定義 POST 端點，路由為 /api/promotions
        app.MapPost("/api/promotions", HandleAddPromotionAsync)
            .WithSummary("新增促銷活動")
            .WithDescription(@"
                新增一個促銷活動
                
                請求格式：
                - JSON 格式的 PromotionAddCommand
                
                回傳格式：
                - 200 OK：新增的促銷活動
                - 400 Bad Request：請求參數錯誤
                
                使用範例：
                - POST /api/promotions
                
                說明：
                - 會自動產生促銷活動 ID
                - 預設狀態為 draft（草稿）
            ")
            .WithTags("促銷管理")
            .Produces<Promotion>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // 定義 PUT 端點，路由為 /api/promotions
        app.MapPut("/api/promotions", HandleUpdatePromotionAsync)
            .WithSummary("更新促銷活動")
            .WithDescription(@"
                更新現有的促銷活動資訊
                
                請求格式：
                - JSON 格式的 PromotionUpdateCommand
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：促銷活動不存在
                
                使用範例：
                - PUT /api/promotions
                
                說明：
                - 只能更新草稿狀態的促銷活動
                - 啟用後的促銷活動不能修改基本資訊
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // 定義 DELETE 端點，路由為 /api/promotions
        app.MapDelete("/api/promotions", HandleDeletePromotionAsync)
            .WithSummary("刪除促銷活動")
            .WithDescription(@"
                刪除指定的促銷活動
                
                請求格式：
                - JSON 格式的 PromotionDeleteCommand
                
                回傳格式：
                - 200 OK：刪除成功
                - 404 Not Found：促銷活動不存在
                
                使用範例：
                - DELETE /api/promotions
                
                說明：
                - 刪除操作不可逆
                - 建議在 UI 層加入確認對話框
                - 刪除促銷活動會一併刪除所有關聯的規則
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // 促銷規則端點（扁平化設計）
        // =========================================================================

        // 定義 GET 端點，路由為 /api/promotions/rules
        app.MapGet("/api/promotions/rules", HandleGetRulesAsync)
            .WithSummary("查詢促銷活動規則列表")
            .WithDescription(@"
                查詢指定促銷活動的所有規則
                
                查詢參數：
                - promotionId：促銷活動 ID（必填）
                
                回傳格式：
                - 200 OK：促銷規則列表
                - 404 Not Found：促銷活動不存在
                
                使用範例：
                - GET /api/promotions/rules?promotionId=1
                
                說明：
                - 不支援分頁（假設一個促銷活動的規則數量有限）
                - 預設按建立時間排序
            ")
            .WithTags("促銷管理")
            .Produces<IEnumerable<PromotionRule>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // 定義 POST 端點，路由為 /api/promotions/rules
        app.MapPost("/api/promotions/rules", HandleAddRuleAsync)
            .WithSummary("新增促銷規則")
            .WithDescription(@"
                為指定促銷活動新增規則
                
                請求格式：
                - JSON 格式的 RuleAddCommand
                
                回傳格式：
                - 200 OK：新增的促銷規則
                - 400 Bad Request：請求參數錯誤
                - 404 Not Found：促銷活動不存在
                
                使用範例：
                - POST /api/promotions/rules
                
                說明：
                - 會自動產生規則 ID
                - 驗證規則類型是否可以共存
                - 驗證規則類型專屬欄位
            ")
            .WithTags("促銷管理")
            .Produces<PromotionRule>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // 定義 PUT 端點，路由為 /api/promotions/rules
        app.MapPut("/api/promotions/rules", HandleUpdateRuleAsync)
            .WithSummary("更新促銷規則")
            .WithDescription(@"
                更新現有的促銷規則資訊
                
                請求格式：
                - JSON 格式的 RuleUpdateCommand
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：規則不存在
                
                使用範例：
                - PUT /api/promotions/rules
                
                說明：
                - 支援部分更新（PATCH 語意）
                - 只更新非 null 的欄位
                - 驗證規則類型專屬欄位
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // 定義 DELETE 端點，路由為 /api/promotions/rules
        app.MapDelete("/api/promotions/rules", HandleDeleteRuleAsync)
            .WithSummary("刪除促銷規則")
            .WithDescription(@"
                刪除指定的促銷規則
                
                請求格式：
                - JSON 格式的 RuleDeleteCommand
                
                回傳格式：
                - 200 OK：刪除成功
                - 404 Not Found：規則不存在
                
                使用範例：
                - DELETE /api/promotions/rules
                
                說明：
                - 刪除操作不可逆
                - 建議在 UI 層加入確認對話框
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // 促銷範圍端點（扁平化設計）
        // =========================================================================

        // 定義 GET 端點，路由為 /api/promotions/scopes
        app.MapGet("/api/promotions/scopes", HandleGetScopesAsync)
            .WithSummary("查詢促銷活動範圍列表")
            .WithDescription(@"
                查詢指定促銷活動的所有範圍
                
                查詢參數：
                - promotionId：促銷活動 ID（必填）
                
                回傳格式：
                - 200 OK：促銷範圍列表
                - 404 Not Found：促銷活動不存在
                
                使用範例：
                - GET /api/promotions/scopes?promotionId=1
                
                說明：
                - 不支援分頁（假設一個促銷活動的範圍數量有限）
                - 預設按建立時間排序
            ")
            .WithTags("促銷管理")
            .Produces<IEnumerable<PromotionScope>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // 定義 POST 端點，路由為 /api/promotions/scopes
        app.MapPost("/api/promotions/scopes", HandleAddScopeAsync)
            .WithSummary("新增促銷範圍")
            .WithDescription(@"
                為指定促銷活動新增範圍
                
                請求格式：
                - JSON 格式的 ScopeAddCommand
                
                回傳格式：
                - 200 OK：新增的促銷範圍
                - 400 Bad Request：請求參數錯誤
                - 404 Not Found：促銷活動不存在
                
                使用範例：
                - POST /api/promotions/scopes
                
                說明：
                - 會自動產生範圍 ID
                - 驗證範圍類型是否有效
            ")
            .WithTags("促銷管理")
            .Produces<PromotionScope>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // 定義 DELETE 端點，路由為 /api/promotions/scopes
        app.MapDelete("/api/promotions/scopes", HandleDeleteScopeAsync)
            .WithSummary("刪除促銷範圍")
            .WithDescription(@"
                刪除指定的促銷範圍
                
                請求格式：
                - JSON 格式的 ScopeDeleteCommand
                
                回傳格式：
                - 200 OK：刪除成功
                - 404 Not Found：範圍不存在
                
                使用範例：
                - DELETE /api/promotions/scopes
                
                說明：
                - 刪除操作不可逆
                - 建議在 UI 層加入確認對話框
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    // =========================================================================
    // Handler 方法
    // =========================================================================

    /// <summary>
    /// 處理查詢促銷活動列表請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetPromotionsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] PromotionsQuery query)
    {
        var result = await mediator.SendAsync(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理統計促銷活動數量請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetPromotionCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] PromotionCountQuery query)
    {
        var count = await mediator.SendAsync(query);
        return Results.Ok(count);
    }

    /// <summary>
    /// 處理新增促銷活動請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleAddPromotionAsync(
        [FromServices] IMediator mediator,
        [FromBody] PromotionAddCommand command)
    {
        var result = await mediator.SendAsync(command);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新促銷活動請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleUpdatePromotionAsync(
        [FromServices] IMediator mediator,
        [FromBody] PromotionUpdateCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 處理刪除促銷活動請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleDeletePromotionAsync(
        [FromServices] IMediator mediator,
        [AsParameters] PromotionDeleteCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 處理查詢促銷規則列表請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetRulesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] RulesQuery query)
    {
        var result = await mediator.SendAsync(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增促銷規則請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleAddRuleAsync(
        [FromServices] IMediator mediator,
        [FromBody] RuleAddCommand command)
    {
        var result = await mediator.SendAsync(command);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新促銷規則請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發更新命令
    /// - 回傳更新結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發更新命令
    /// 2. 回傳更新結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發更新命令</param>
    /// <param name="command">更新促銷規則命令物件（包含規則資料）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：更新成功
    /// </returns>
    private static async Task<IResult> HandleUpdateRuleAsync(
        [FromServices] IMediator mediator,
        [FromBody] RuleUpdateCommand command)
    {
        // ========== 第一步：透過 Mediator 分發更新命令 ==========
        // Mediator 會找到對應的 Handler（RuleUpdateHandler）
        // Handler 會執行更新並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳更新結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 處理刪除促銷規則請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleDeleteRuleAsync(
        [FromServices] IMediator mediator,
        [AsParameters] RuleDeleteCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 處理查詢促銷範圍列表請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetScopesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ScopesQuery query)
    {
        var result = await mediator.SendAsync(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增促銷範圍請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleAddScopeAsync(
        [FromServices] IMediator mediator,
        [FromBody] ScopeAddCommand command)
    {
        var result = await mediator.SendAsync(command);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理刪除促銷範圍請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleDeleteScopeAsync(
        [FromServices] IMediator mediator,
        [AsParameters] ScopeDeleteCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }
}
