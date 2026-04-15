using Manian.Application.Commands.Warehouses;
using Manian.Application.Models;
using Manian.Application.Queries.Warehouses;
using Manian.Domain.Entities.Warehouses;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Warehouses;

/// <summary>
/// 庫存相關的 API 端點定義
/// 
/// 職責：
/// - 定義庫存相關的 API 端點
/// - 處理庫存的查詢和命令請求
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
/// - 基礎路徑：/api/inventories
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）、DELETE（刪除）
/// 
/// 使用場景：
/// - 庫存查詢（查看指定 SKU 或儲位的庫存）
/// - 庫存調整（盤點調整、損耗調整）
/// - 庫存初始化（設定初始庫存）
/// - 庫存記錄管理（新增、刪除庫存記錄）
/// </summary>
public static class InventoryEndpoint
{
    /// <summary>
    /// 註冊庫存相關的 API 端點
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
    /// app.MapInventoryEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapInventories(this WebApplication app)
    {
        var group = app.MapGroup("/api/inventories")
            .WithTags("倉庫管理");

        // =========================================================================
        // 查詢端點 (Query Endpoints)
        // =========================================================================

        // GET /api/inventories - 查詢庫存列表
        group.MapGet("/", GetInventoriesAsync)
            .WithSummary("查詢庫存列表")
            .WithDescription(@"
                查詢系統中的庫存資料。
                
                查詢參數：
                - skuId：SKU ID（查詢指定 SKU 在所有儲位的庫存）
                - locationId：儲位 ID（查詢指定儲位的所有庫存）
                
                回傳格式：
                - 200 OK：庫存列表
                
                使用範例：
                - GET /api/inventories?skuId=1（查詢 SKU 1 在所有儲位的庫存）
                - GET /api/inventories?locationId=1（查詢儲位 1 的所有庫存）
            ")
            .Produces<Pagination<Inventory>>(StatusCodes.Status200OK);

        // =========================================================================
        // 命令端點 (Command Endpoints)
        // =========================================================================

        // POST /api/inventories - 新增庫存記錄
        group.MapPost("/", CreateInventoryAsync)
            .WithSummary("新增庫存記錄")
            .WithDescription(@"
                新增一個庫存記錄。
                
                請求格式：
                - JSON 格式的 InventoryAddCommand
                
                回傳格式：
                - 200 OK：新增的庫存記錄
                - 400 BadRequest：請求格式錯誤
            ")
            .Produces<Inventory>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // PUT /api/inventories - 更新庫存
        group.MapPut("/", UpdateInventoryAsync)
            .WithSummary("更新庫存")
            .WithDescription(@"
                更新現有的庫存資訊。
                
                請求格式：
                - JSON 格式的 InventoryUpdateCommand
                
                回傳格式：
                - 200 OK：更新成功
                - 404 NotFound：庫存記錄不存在
                - 400 BadRequest：請求格式錯誤
            ")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        // DELETE /api/inventories - 刪除庫存記錄
        group.MapDelete("/", DeleteInventoryAsync)
            .WithSummary("刪除庫存記錄")
            .WithDescription(@"
                刪除指定的庫存記錄。
                
                請求格式：
                - JSON 格式的 InventoryDeleteCommand
                
                回傳格式：
                - 204 NoContent：刪除成功
                - 404 NotFound：庫存記錄不存在
                - 400 BadRequest：請求格式錯誤
            ")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    // =========================================================================
    // Handler 方法 (Handler Methods)
    // =========================================================================

    /// <summary>
    /// 處理查詢庫存列表請求的私有方法
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
    /// <param name="query">庫存查詢請求物件</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：庫存列表
    /// </returns>
    private static async Task<IResult> GetInventoriesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] InventoriesQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（InventoriesQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和庫存列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增庫存記錄請求的私有方法
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
    /// 1. 透過 Mediator 分發新增命令
    /// 2. 回傳新增的庫存記錄
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">新增庫存命令物件</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：新增的庫存記錄
    /// </returns>
    private static async Task<IResult> CreateInventoryAsync(
        [FromServices] IMediator mediator,
        [FromBody] InventoryAddCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（InventoryAddHandler）
        // Handler 會執行新增邏輯並回傳結果
        var result = await mediator.SendAsync(command);
        
        // ========== 第二步：回傳新增結果 ==========
        // 回傳 200 OK 狀態碼和新增的庫存記錄
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新庫存請求的私有方法
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
    /// 1. 透過 Mediator 分發更新命令
    /// 2. 回傳 200 OK 狀態碼
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">更新庫存命令物件</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：更新成功
    /// </returns>
    private static async Task<IResult> UpdateInventoryAsync(
        [FromServices] IMediator mediator,
        [FromBody] InventoryUpdateCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（InventoryUpdateHandler）
        // Handler 會執行更新邏輯
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳更新結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 處理刪除庫存記錄請求的私有方法
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
    /// 1. 透過 Mediator 分發刪除命令
    /// 2. 回傳 204 NoContent 狀態碼
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">刪除庫存命令物件</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 204 NoContent：刪除成功
    /// </returns>
    private static async Task<IResult> DeleteInventoryAsync(
        [FromServices] IMediator mediator,
        [AsParameters] InventoryDeleteCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（InventoryDeleteHandler）
        // Handler 會執行刪除邏輯
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳刪除結果 ==========
        // 回傳 204 NoContent 狀態碼
        return Results.NoContent();
    }
}
