using Manian.Application.Commands.Warehouses;
using Manian.Application.Models;
using Manian.Application.Queries.Warehouses;
using Manian.Domain.Entities.Warehouses;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Warehouses;

/// <summary>
/// 倉庫儲位相關的 API 端點定義
/// </summary>
public static class LocationEndpoint
{
    /// <summary>
    /// 註冊倉庫儲位相關的 API 端點
    /// 
    /// 路由前綴：/api/locations
    /// </summary>
    public static void MapLocations(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/locations")
            .WithTags("倉庫管理");

        // ===== 查詢端點 =====

        group.MapGet("/", GetLocationsAsync)
            .WithSummary("查詢儲位列表")
            .WithDescription(@"
                查詢系統中的儲位資料，支援多種篩選條件和分頁功能。
                
                查詢參數：
                - parentId：父位置 ID
                - status：狀態
                - search：搜尋關鍵字
                - locationType：位置類型
                - zoneType：區域類型
                - cursor：游標
                - size：每頁筆數
            ")
            .Produces<Pagination<Location>>(StatusCodes.Status200OK);

        // 補充：獲取儲位數量統計
        group.MapGet("/count", GetCountAsync)
            .WithSummary("統計儲位數量")
            .WithDescription(@"
                統計符合條件的儲位總數量。
                
                查詢參數：
                - parentId：父位置 ID
                - status：狀態
                - locationType：位置類型
                - zoneType：區域類型
            ")
            .Produces<int>(StatusCodes.Status200OK);

        // 補充：獲取儲位路徑
        group.MapGet("/{id}/path", GetPathAsync)
            .WithSummary("獲取儲位路徑")
            .WithDescription(@"
                獲取從根節點到指定儲位的完整路徑資訊。
                
                路徑參數：
                - id：儲位 ID
                
                回傳格式：
                - 返回包含路徑上所有節點的列表
            ")
            .Produces<IEnumerable<Location>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // ===== 命令端點 =====

        group.MapPost("/", CreateLocationAsync)
            .WithSummary("新增儲位")
            .WithDescription(@"
                新增一個儲位。
            ")
            .Produces<Location>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/", UpdateLocationAsync)
            .WithSummary("更新儲位")
            .WithDescription(@"
                更新現有的儲位資訊。
            ")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/", DeleteLocationAsync)
            .WithSummary("刪除儲位")
            .WithDescription(@"
                刪除指定的儲位。
            ")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    // ===== Handler 方法 =====

    /// <summary>
    /// 查詢儲位列表的處理方法
    /// </summary>
    private static async Task<IResult> GetLocationsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] LocationsQuery query)
    {
        var result = await mediator.SendAsync(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// 統計儲位數量的處理方法
    /// </summary>
    private static async Task<IResult> GetCountAsync(
        [FromServices] IMediator mediator,
        [AsParameters] LocationCountQuery query) // 假設你有這個 Query
    {
        var count = await mediator.SendAsync(query);
        return Results.Ok(count);
    }

    /// <summary>
    /// 獲取儲位路徑的處理方法
    /// </summary>
    private static async Task<IResult> GetPathAsync(
        [FromServices] IMediator mediator,
        [AsParameters] LocationPathQuery query)
    {
        var path = await mediator.SendAsync(query);   
        if (path == null) return Results.NotFound();
        return Results.Ok(path);
    }

    /// <summary>
    /// 新增儲位的處理方法
    /// </summary>
    private static async Task<IResult> CreateLocationAsync(
        [FromServices] IMediator mediator,
        [FromBody] LocationAddCommand command)
    {
        var result = await mediator.SendAsync(command);
        return Results.Ok(result);
    }

    /// <summary>
    /// 更新儲位的處理方法
    /// </summary>
    private static async Task<IResult> UpdateLocationAsync(
        [FromServices] IMediator mediator,
        [FromBody] LocationUpdateCommand command)
    {
        await mediator.SendAsync(command);
        return Results.Ok();
    }

    /// <summary>
    /// 刪除儲位的處理方法
    /// </summary>
    private static async Task<IResult> DeleteLocationAsync(
        [FromServices] IMediator mediator,
        [AsParameters] LocationDeleteCommand command)
    {
        await mediator.SendAsync(command);
        return Results.NoContent();
    }
}
