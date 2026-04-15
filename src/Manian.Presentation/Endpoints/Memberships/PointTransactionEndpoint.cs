using Manian.Application.Commands.Users;
using Manian.Application.Queries.Users;
using Manian.Domain.Entities.Memberships;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Memberships;

/// <summary>
/// 點數交易 API 端點
/// 
/// 職責：
/// - 定義點數交易相關的 API 端點
/// - 處理點數交易的查詢和命令請求
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
/// - 基礎路徑：/api/point-transactions
/// - 支援的動作：GET（查詢）、POST（新增）
/// 
/// 使用場景：
/// - 用戶點數記錄查詢
/// - 點數增減操作
/// - 點數交易歷史查看
/// </summary>
public static class PointTransactionEndpoint
{
    /// <summary>
    /// 註冊點數交易相關的 API 端點
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
    /// app.MapPointTransactionEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapPointTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        // =========================================================================
        // GET /api/users/{userId}/point-transactions - 查詢用戶的點數交易記錄
        // =========================================================================
        
        // 定義 GET 端點，路由為 /api/users/{userId}/point-transactions
        app.MapGet("/api/users/{userId}/point-transactions", HandleGetPointTransactionsAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("查詢用戶的點數交易記錄")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            查詢指定用戶的點數交易記錄
            
            路徑參數：
            - userId：用戶 ID（必填）
            
            查詢參數：
            - cursor：游標（可選），用於分頁
            - size：每頁資料筆數（可選），預設 20
            
            回傳格式：
            - 200 OK：點數交易記錄集合
            
            使用範例：
            - GET /api/users/1/point-transactions
            - GET /api/users/1/point-transactions?size=50
            
            說明：
            - 點數交易記錄包含：變動量、原因、時間等資訊
            - 支援分頁查詢
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<IEnumerable<PointTransaction>>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/users/{userId}/point-transactions - 新增點數交易記錄
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/users/{userId}/point-transactions
        app.MapPost("/api/users/{userId}/point-transactions", HandleAddPointTransactionAsync)
        
        // 設定端點摘要
        .WithSummary("新增點數交易記錄")
        
        // 設定端點描述
        .WithDescription(@"
            為用戶新增一筆點數交易記錄
            
            路徑參數：
            - userId：用戶 ID（必填）
            
            請求內容：
            - PointTransactionAddCommand：點數交易資料（必填）
            
            回傳格式：
            - 200 OK：新增成功
            - 404 Not Found：用戶不存在
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - POST /api/users/1/point-transactions
            {
                ""delta"": 100,
                ""reason"": ""購物贈點"",
                ""refType"": ""order"",
                ""refId"": ""12345""
            }
            
            說明：
            - delta 為正數表示增加點數，負數表示扣除點數
            - 新增後會自動更新用戶的點數餘額
        ")
        
        // 設定端點標籤
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }

    // ===== Handler 方法 =====

    /// <summary>
    /// 處理查詢點數交易記錄請求的私有方法
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
    /// <param name="query">點數交易查詢請求物件，包含用戶 ID 和分頁參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：點數交易記錄集合
    /// </returns>
    private static async Task<IResult> HandleGetPointTransactionsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] PointTransactionsQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（PointTransactionsQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和點數交易記錄集合
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增點數交易記錄請求的私有方法
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
    /// <param name="userId">用戶 ID（路徑參數）</param>
    /// <param name="command">新增點數交易命令物件（包含點數交易資料）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：新增成功
    /// </returns>
    private static async Task<IResult> HandleAddPointTransactionAsync(
        [FromServices] IMediator mediator,
        int userId,
        [FromBody] PointTransactionAddCommand command)
    {
        // ========== 第一步：設定用戶 ID ==========
        // 將路徑參數中的 userId 設定到命令物件中
        command.UserId = userId;
        
        // ========== 第二步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（PointTransactionAddHandler）
        // Handler 會執行新增並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第三步：回傳新增結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }
}
