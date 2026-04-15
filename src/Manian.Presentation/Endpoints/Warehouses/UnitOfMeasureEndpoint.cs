using Manian.Application.Queries.Products;
using Manian.Domain.Entities.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Warehouses;

/// <summary>
/// 計量單位 API 端點
/// 
/// 職責：
/// - 定義計量單位相關的 API 端點
/// - 處理計量單位的查詢請求
/// 
/// 設計模式：
/// - Minimal API：使用 ASP.NET Core 的 Minimal API 風格
/// - CQRS：透過 Mediator 分發查詢請求
/// - 依賴注入：注入所需的服務
/// 
/// 架構位置：
/// - 位於 Presentation 層（展示層）
/// - 負責處理 HTTP 請求和回應
/// - 不包含業務邏輯，只負責路由和參數處理
/// 
/// 路由設計：
/// - 基礎路徑：/api/unit-of-measures
/// - 支援的動作：GET（查詢）
/// 
/// 使用場景：
/// - 商品管理頁面顯示計量單位選項
/// - 庫存管理頁面顯示計量單位選項
/// - 訂單處理時顯示計量單位資訊
/// </summary>
public static class UnitOfMeasureEndpoint
{
    /// <summary>
    /// 註冊計量單位相關的 API 端點
    /// 
    /// 職責：
    /// - 定義 API 路由
    /// - 處理 HTTP 請求
    /// - 呼叫 Mediator 分發查詢
    /// 
    /// 設計考量：
    /// - 使用擴充方法模式，方便在 Program.cs 中呼叫
    /// - 所有端點都使用 Mediator 分發請求
    /// - 遵循 RESTful API 設計原則
    /// 
    /// 註冊方式：
    /// 在 Program.cs 中呼叫：
    /// <code>
    /// app.MapUnitOfMeasureEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapUnitOfMeasureEndpoints(this WebApplication app)
    {
        // =========================================================================
        // GET /api/unit-of-measures - 查詢計量單位列表
        // =========================================================================
        
        // 定義 GET 端點，路由為 /api/unit-of-measures
        app.MapGet("/api/unit-of-measures", HandleGetUnitOfMeasuresAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("查詢計量單位列表")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            查詢系統中的計量單位列表
            
            查詢參數：
            - search：搜尋關鍵字（可選），會在 Code、Name、Description 中搜尋
            - lastCreatedAt：上一頁最後一筆資料的建立時間（可選），用於分頁
            - size：每頁資料筆數（可選），預設 1000
            
            回傳格式：
            - 200 OK：計量單位列表
            
            使用範例：
            - GET /api/unit-of-measures
            - GET /api/unit-of-measures?search=box
            - GET /api/unit-of-measures?size=50
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("計量單位")
        
        // 產生 OpenAPI 回應定義
        .Produces<IEnumerable<UnitOfMeasure>>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// 處理查詢計量單位列表請求的私有方法
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
    /// <param name="query">計量單位查詢請求物件，包含搜尋和分頁參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：計量單位列表
    /// </returns>
    private static async Task<IResult> HandleGetUnitOfMeasuresAsync(
        [FromServices] IMediator mediator,
        [AsParameters] UnitOfMeasuresQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（UnitOfMeasuresQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和計量單位列表
        return Results.Ok(result);
    }
}
