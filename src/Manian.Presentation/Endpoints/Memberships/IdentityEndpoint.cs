using Manian.Application.Commands.Users;
using Manian.Application.Queries.Users;
using Manian.Domain.Entities.Memberships;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Memberships;

/// <summary>
/// 身份認證 API 端點
/// 
/// 職責：
/// - 定義身份認證相關的 API 端點
/// - 處理身份認證的查詢和命令請求
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
/// - 基礎路徑：/api/users/{userId}/identities
/// - 支援的動作：GET（查詢）、POST（新增）、DELETE（刪除）
/// 
/// 使用場景：
/// - 用戶綁定第三方登入
/// - 用戶解除綁定第三方登入
/// - 查詢用戶的登入方式
/// </summary>
public static class IdentityEndpoint
{
    /// <summary>
    /// 註冊身份認證相關的 API 端點
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
    /// app.MapIdentityEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // =========================================================================
        // GET /api/users/{userId}/identities - 查詢用戶的身份認證資訊
        // =========================================================================
        
        // 定義 GET 端點，路由為 /api/users/{userId}/identities
        app.MapGet("/api/users/{userId}/identities", HandleGetIdentitiesAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("查詢用戶的身份認證資訊")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            查詢指定用戶的所有身份認證資訊
            
            路徑參數：
            - userId：用戶 ID（必填）
            
            回傳格式：
            - 200 OK：身份認證資訊集合
            
            使用範例：
            - GET /api/users/1/identities
            
            說明：
            - 身份認證資訊包含：認證廠商（google、line、microsoft、facebook）
            - 預設按 Provider 排序
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<IEnumerable<Identity>>(StatusCodes.Status200OK);

        // =========================================================================
        // POST /api/users/{userId}/identities - 新增身份認證資訊
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/users/{userId}/identities
        app.MapPost("/api/users/{userId}/identities", HandleAddIdentityAsync)
        
        // 設定端點摘要
        .WithSummary("新增身份認證資訊")
        
        // 設定端點描述
        .WithDescription(@"
            為用戶新增一個身份認證資訊
            
            路徑參數：
            - userId：用戶 ID（必填）
            
            請求內容：
            - IdentityAddCommand：身份認證資訊（必填）
            
            回傳格式：
            - 200 OK：新增後的身份認證資訊（Identity）
            - 404 Not Found：用戶不存在
            - 400 Bad Request：請求內容錯誤或身份認證資訊重複
            
            使用範例：
            - POST /api/users/1/identities
            {
                ""provider"": ""google"",
                ""providerUid"": ""123456789""
            }
            
            說明：
            - provider 必須是有效的認證廠商（google、line、microsoft、facebook）
            - (UserId + Provider + ProviderUid) 必須唯一
        ")
        
        // 設定端點標籤
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<Identity>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // DELETE /api/identities/{id} - 刪除身份認證資訊
        // =========================================================================
        
        // 定義 DELETE 端點，路由為 /api/identities/{id}
        app.MapDelete("/api/identities/{id}", HandleDeleteIdentityAsync)
        
        // 設定端點摘要
        .WithSummary("刪除身份認證資訊")
        
        // 設定端點描述
        .WithDescription(@"
            刪除指定的身份認證資訊
            
            路徑參數：
            - id：身份認證資訊 ID（必填）
            
            回傳格式：
            - 204 No Content：刪除成功
            - 404 Not Found：身份認證資訊不存在
            - 400 Bad Request：用戶只有一種登入方式，無法刪除
            
            使用範例：
            - DELETE /api/identities/1
            
            說明：
            - 如果用戶只有一種登入方式，不允許刪除
            - 建議在 UI 層加入確認對話框
        ")
        
        // 設定端點標籤
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }

    // ===== Handler 方法 =====

    /// <summary>
    /// 處理查詢身份認證資訊請求的私有方法
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
    /// <param name="query">身份認證查詢請求物件，包含用戶 ID</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：身份認證資訊集合
    /// </returns>
    private static async Task<IResult> HandleGetIdentitiesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] IdentitiesQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（IdentitiesQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和身份認證資訊集合
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增身份認證資訊請求的私有方法
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
    /// 1. 設定用戶 ID
    /// 2. 透過 Mediator 分發命令請求
    /// 3. 回傳新增結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="userId">用戶 ID（路徑參數）</param>
    /// <param name="command">新增身份認證命令物件（包含身份認證資訊）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：新增後的身份認證資訊（Identity）
    /// </returns>
    private static async Task<IResult> HandleAddIdentityAsync(
        [FromServices] IMediator mediator,
        int userId,
        [FromBody] IdentityAddCommand command)
    {
        // ========== 第一步：設定用戶 ID ==========
        // 將路徑參數中的 userId 設定到命令物件中
        command.UserId = userId;
        
        // ========== 第二步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（IdentityAddHandler）
        // Handler 會執行新增並回傳結果
        var result = await mediator.SendAsync(command);
        
        // ========== 第三步：回傳新增結果 ==========
        // 回傳 200 OK 狀態碼和新增後的身份認證資訊
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理刪除身份認證資訊請求的私有方法
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
    /// <param name="id">身份認證資訊 ID（路徑參數）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 204 No Content：刪除成功
    /// </returns>
    private static async Task<IResult> HandleDeleteIdentityAsync(
        [FromServices] IMediator mediator,
        int id)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（IdentityDeleteHandler）
        // Handler 會執行刪除並回傳結果
        await mediator.SendAsync(new IdentityDeleteCommand { Id = id });
        
        // ========== 第二步：回傳刪除結果 ==========
        // 回傳 204 No Content 狀態碼
        return Results.NoContent();
    }
}
