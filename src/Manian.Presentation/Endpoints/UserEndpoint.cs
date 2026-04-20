using Manian.Application.Commands.Users;
using Manian.Application.Models;
using Manian.Application.Queries.Users;
using Manian.Domain.Entities.Memberships;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints;

/// <summary>
/// 用戶管理 API 端點
/// 
/// 職責：
/// - 定義用戶相關的 API 端點
/// - 處理用戶的查詢和命令請求
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
/// - 基礎路徑：/api/users
/// - 支援的動作：GET（查詢）、POST（新增）、PUT（更新）
/// 
/// 使用場景：
/// - 用戶管理頁面
/// - 用戶資料編輯
/// - 用戶資訊查詢
/// </summary>
public static class UserEndpoint
{
    /// <summary>
    /// 註冊用戶相關的 API 端點
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
    /// app.MapUserEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapUserEndpoints(this WebApplication app)
    {
        // =========================================================================
        // GET /api/users - 查詢用戶列表
        // =========================================================================

        // 定義 GET 端點，路由為 /api/users
        app.MapGet("/api/users", HandleGetUsersAsync)

        // 設定端點摘要
        .WithSummary("查詢用戶列表")

        // 設定端點描述
        .WithDescription(@"
            查詢系統中的用戶列表
            
            查詢參數：
            - search：搜尋關鍵字（可選），會在 DisplayName、Email 中搜尋
            - cursor：游標（可選），用於分頁
            - size：每頁資料筆數（可選），預設 20
            
            回傳格式：
            - 200 OK：用戶列表
            
            使用範例：
            - GET /api/users
            - GET /api/users?search=zhang
            - GET /api/users?size=50
            
            說明：
            - 支援分頁查詢
            - 預設按 CreatedAt 排序
        ")

        // 設定端點標籤
        .WithTags("用戶管理")

        // 產生 OpenAPI 回應定義
        .Produces<IEnumerable<User>>(StatusCodes.Status200OK);


        // =========================================================================
        // POST /api/users - 新增用戶
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/users
        app.MapPost("/api/users", HandleAddUserAsync)
        
        // 設定端點摘要
        .WithSummary("新增用戶")
        
        // 設定端點描述
        .WithDescription(@"
            新增一個用戶
            
            請求內容：
            - UserAddCommand：用戶資料（必填）
            
            回傳格式：
            - 200 OK：新增後的用戶資料（User）
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - POST /api/users
            {
                ""displayName"": ""張三"",
                ""email"": ""zhangsan@example.com"",
                ""password"": ""password123""
            }
        ")
        
        // 設定端點標籤
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<User>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // PUT /api/users - 更新用戶
        // =========================================================================
        
        // 定義 PUT 端點，路由為 /api/users
        app.MapPut("/api/users", HandleUpdateUserAsync)
        
        // 設定端點摘要
        .WithSummary("更新用戶")
        
        // 設定端點描述
        .WithDescription(@"
            更新現有的用戶資料
            
            請求內容：
            - UserUpdateCommand：用戶資料（必填）
            
            回傳格式：
            - 200 OK：更新成功
            - 404 Not Found：用戶不存在
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - PUT /api/users
            {
                ""id"": 1,
                ""displayName"": ""李四"",
                ""email"": ""lisi@example.com""
            }
        ")
        
        // 設定端點標籤
        .WithTags("用戶管理")
        
        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);


        // =========================================================================
        // GET /api/users/roles - 查詢角色列表
        // =========================================================================
        // 定義 GET 端點，路由為 /api/users/roles
        app.MapGet("/api/users/roles", HandleGetRolesAsync)
        // 設定端點摘要
        .WithSummary("查詢角色列表")
        // 設定端點描述
        .WithDescription(@"
            查詢系統中的角色列表
            
            查詢參數：
            - search：搜尋關鍵字（可選），會在 Name、Code 中搜尋
            - cursor：游標（可選），用於分頁
            - size：每頁資料筆數（可選）
            
            回傳格式：
            - 200 OK：角色列表
            
            使用範例：
            - GET /api/users/roles
            - GET /api/users/roles?search=admin
            - GET /api/users/roles?size=10
            
            說明：
            - 支援 Cursor 分頁
            - 預設按 ID 排序
        ")
        // 設定端點標籤
        .WithTags("用戶管理")
        // 產生 OpenAPI 回應定義
        .Produces<Pagination<Role>>(StatusCodes.Status200OK);
    }

    // ===== Handler 方法 =====

    /// <summary>
    /// 處理查詢用戶列表請求的私有方法
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
    /// <param name="query">用戶查詢請求物件，包含搜尋和分頁參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：用戶列表
    /// </returns>
    private static async Task<IResult> HandleGetUsersAsync(
        [FromServices] IMediator mediator,
        [AsParameters] UsersQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（UsersQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和用戶列表
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理新增用戶請求的私有方法
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
    /// <param name="command">新增用戶命令物件（包含用戶資料）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：新增後的用戶資料（User）
    /// </returns>
    private static async Task<IResult> HandleAddUserAsync(
        [FromServices] IMediator mediator,
        [FromBody] UserAddCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（UserAddHandler）
        // Handler 會執行新增並回傳結果
        var result = await mediator.SendAsync(command);
        
        // ========== 第二步：回傳新增結果 ==========
        // 回傳 200 OK 狀態碼和新增後的用戶資料
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理更新用戶請求的私有方法
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
    /// <param name="command">更新用戶命令物件（包含用戶資料）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：更新成功
    /// </returns>
    private static async Task<IResult> HandleUpdateUserAsync(
        [FromServices] IMediator mediator,
        [FromBody] UserUpdateCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（UserUpdateHandler）
        // Handler 會執行更新並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳更新結果 ==========
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 處理查詢角色列表請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發查詢
    /// - 回傳查詢結果
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發查詢請求
    /// 2. 回傳查詢結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發查詢請求</param>
    /// <param name="query">角色查詢請求物件，包含搜尋和分頁參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：角色列表
    /// </returns>
    private static async Task<IResult> HandleGetRolesAsync(
        [FromServices] IMediator mediator,
        [AsParameters] RolesQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（RolesQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和角色列表
        return Results.Ok(result);
    }
}
