using Manian.Application.Commands.Promotions;
using Manian.Application.Models;
using Manian.Application.Queries.Promotions;
using Manian.Domain.Entities.Promotions;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Promotions;

/// <summary>
/// 優惠券 API 端點
/// 
/// 職責：
/// - 定義優惠券相關的 API 端點
/// - 處理優惠券的查詢和命令請求
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
/// - 基礎路徑：/api/coupons
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）、DELETE（刪除）
/// 
/// 使用場景：
/// - 優惠券管理頁面
/// - 用戶優惠券查詢
/// - 儀表板顯示優惠券統計
/// </summary>
public static class CouponEndpoint
{
    /// <summary>
    /// 註冊優惠券相關的 API 端點
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
    /// app.MapCouponEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapCoupons(this WebApplication app)
    {
        // =========================================================================
        // GET /api/coupons - 查詢優惠券列表
        // =========================================================================
        
        app.MapGet("/api/coupons", HandleGetCouponsAsync)
            .WithSummary("查詢優惠券列表")
            .WithDescription(@"
                查詢系統中的優惠券列表
                
                查詢參數：
                - userId：用戶 ID（可選），用於查詢特定用戶的優惠券
                - isUsed：是否已使用（可選），true 表示已使用，false 表示未使用
                - couponCode：優惠券代碼（可選），用於精確查詢特定優惠券
                - cursor：游標（可選），用於分頁
                - size：每頁資料筆數（可選），預設 20
                
                回傳格式：
                - 200 OK：優惠券列表（分頁）
                
                使用範例：
                - GET /api/coupons
                - GET /api/coupons?userId=1
                - GET /api/coupons?isUsed=false
                - GET /api/coupons?couponCode=VIP85
                - GET /api/coupons?size=10
                
                說明：
                - 支援游標分頁
                - 預設按建立時間排序
                - 管理員可查詢所有優惠券，一般用戶只能查詢自己的優惠券
            ")
            .WithTags("促銷管理")
            .Produces<Pagination<Coupon>>(StatusCodes.Status200OK);

        // =========================================================================
        // GET /api/coupons/count - 統計優惠券數量
        // =========================================================================
        
        app.MapGet("/api/coupons/count", HandleGetCouponCountAsync)
            .WithSummary("統計優惠券數量")
            .WithDescription(@"
                統計符合條件的優惠券總數量
                
                查詢參數：
                - userId：用戶 ID（可選），用於統計特定用戶的優惠券數量
                - isUsed：是否已使用（可選），true 表示已使用，false 表示未使用
                
                回傳格式：
                - 200 OK：優惠券總數
                
                使用範例：
                - GET /api/coupons/count
                - GET /api/coupons/count?userId=1
                - GET /api/coupons/count?isUsed=false
                
                說明：
                - 優先使用估計數量，提升效能
                - 如果估計數量不可用，執行精確計數
            ")
            .WithTags("促銷管理")
            .Produces<int>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/coupons - 新增優惠券
        // =========================================================================
        
        app.MapPost("/api/coupons", HandleAddCouponAsync)
            .WithSummary("新增優惠券")
            .WithDescription(@"
                新增一個優惠券
                
                請求格式：
                - JSON 格式的 CouponAddCommand
                
                回傳格式：
                - 200 OK：新增的優惠券
                - 400 Bad Request：請求參數錯誤
                
                使用範例：
                - POST /api/coupons
                
                說明：
                - 會自動產生優惠券 ID
                - 優惠券代碼必須唯一
                - 可以指定用戶（user_id），不指定則為公開優惠券
            ")
            .WithTags("促銷管理")
            .Produces<Coupon>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // PUT /api/coupons - 更新優惠券
        // =========================================================================
        
        app.MapPut("/api/coupons", HandleUpdateCouponAsync)
            .WithSummary("更新優惠券")
            .WithDescription(@"
                更新現有的優惠券資訊
                
                請求格式：
                - JSON 格式的 CouponUpdateCommand
                
                回傳格式：
                - 200 OK：更新成功
                - 404 Not Found：優惠券不存在
                
                使用範例：
                - PUT /api/coupons
                
                說明：
                - 已使用的優惠券不能修改
                - 優惠券代碼修改後必須保持唯一
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // DELETE /api/coupons - 刪除優惠券
        // =========================================================================
        
        app.MapDelete("/api/coupons", HandleDeleteCouponAsync)
            .WithSummary("刪除優惠券")
            .WithDescription(@"
                刪除指定的優惠券
                
                請求格式：
                - JSON 格式的 CouponDeleteCommand
                
                回傳格式：
                - 200 OK：刪除成功
                - 404 Not Found：優惠券不存在
                
                使用範例：
                - DELETE /api/coupons
                
                說明：
                - 刪除操作不可逆
                - 建議在 UI 層加入確認對話框
                - 已使用的優惠券不允許刪除
            ")
            .WithTags("促銷管理")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    // =========================================================================
    // Handler 方法
    // =========================================================================

    /// <summary>
    /// 處理查詢優惠券列表請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetCouponsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CouponsQuery query)
    {
        var result = await mediator.SendAsync(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理統計優惠券數量請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleGetCouponCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CouponCountQuery query)
    {
        var count = await mediator.SendAsync(query);
        return Results.Ok(count);
    }

    /// <summary>
    /// 處理新增優惠券請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleAddCouponAsync(
        [FromServices] IMediator mediator,
        [FromBody] CouponAddCommand command)
    {
        var result = await mediator.SendAsync(command);
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新優惠券請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleUpdateCouponAsync(
        [FromServices] IMediator mediator,
        [FromBody] CouponUpdateCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 處理刪除優惠券請求的私有方法
    /// </summary>
    private static async Task<IResult> HandleDeleteCouponAsync(
        [FromServices] IMediator mediator,
        [AsParameters] CouponDeleteCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }
}
