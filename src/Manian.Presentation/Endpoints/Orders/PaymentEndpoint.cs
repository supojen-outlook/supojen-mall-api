using Manian.Application.Commands.NewebPay;
using Manian.Application.Models.NewebPay;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Orders;

/// <summary>
/// 付款相關 API 端點
/// 
/// 職責：
/// - 定義付款相關的 API 端點
/// - 處理付款建立和回調請求
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
/// - 基礎路徑：/api/payment
/// - 支援的動作：POST（建立付款）、POST（付款回調）
/// 
/// 使用場景：
/// - 使用者選擇藍新金流付款
/// - 藍新金流 Server 對 Server 通知
/// - 付款狀態更新
/// </summary>
public static class PaymentEndpoint
{
    /// <summary>
    /// 註冊付款相關的 API 端點
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
    /// app.MapPaymentEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        // =========================================================================
        // POST /api/payment/create - 建立付款請求
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/payment/create
        app.MapPost("/api/payment/create", HandleCreatePaymentAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("建立藍新金流付款請求")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            建立藍新金流付款請求，用於導向藍新金流付款頁面
            
            請求內容：
            - OrderId：訂單 ID（必填）
            - Domain：網域名稱（必填）
            - Email：聯絡信箱（必填）
            
            回傳格式：
            - 200 OK：付款請求模型（包含 MerchantID、TradeInfo、TradeSha）
            - 400 Bad Request：請求內容錯誤或訂單不存在
            
            使用範例：
            - POST /api/payment/create
            {
                ""orderId"": 1001,
                ""domain"": ""https://example.com"",
                ""email"": ""user@example.com""
            }
            
            說明：
            - 付款金額從訂單資料庫讀取，不允許前端指定
            - 會驗證訂單是否屬於當前登入使用者
            - 回傳的資料可用於導向藍新金流付款頁面
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("訂單管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<NewebPayRequestModel>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // POST /api/payment/notify - 藍新金流付款回調
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/payment/notify
        app.MapPost("/api/payment/notify", HandleNotifyPaymentAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("藍新金流付款回調")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            接收藍新金流平台的付款回調通知
            
            請求內容：
            - Status：回應狀態（必填）
            - TradeInfo：加密的交易資訊（必填）
            
            回傳格式：
            - 200 OK：處理成功
            - 400 Bad Request：請求內容錯誤
            
            使用範例：
            - POST /api/payment/notify
            {
                ""Status"": ""SUCCESS"",
                ""TradeInfo"": ""加密的交易資訊""
            }
            
            說明：
            - TradeInfo 經過 AES 加密，需解密後才能使用
            - 必須驗證 Status 是否為 ""SUCCESS""
            - 必須驗證金額是否正確，防止金額被篡改
            - 這是藍新金流 Server 對 Server 通知，不是使用者轉跳
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("訂單管理")
        
        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous();
    }

    // =========================================================================
    // Handler 方法 (Handler Methods)
    // =========================================================================

    /// <summary>
    /// 處理建立付款請求的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發 NewebPayCreateCommand 命令
    /// - 回傳付款請求模型
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發 NewebPayCreateCommand 命令
    /// 2. 回傳付款請求模型
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">建立付款命令物件（包含 OrderId、Domain 和 Email）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：付款請求模型（包含 MerchantID、TradeInfo、TradeSha）
    /// - 400 Bad Request：請求內容錯誤或訂單不存在
    /// </returns>
    private static async Task<IResult> HandleCreatePaymentAsync(
        HttpContext context,
        [FromServices] IMediator mediator,
        [FromBody] NewebPayCreateCommand command)
    {
        // 檢查當前環境是否為開發環境
        if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            // ========== 第一步：設定 Domain 參數 ==========
            command.Domain = "https://google.com";  
        }
        else
        {
            // ========== 第一步：設定 Domain 參數 ==========
            command.Domain = $"{context.Request.Scheme}://{context.Request.Host}";            
        }

        // ========== 第二步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（NewebPayCreateHandler）
        // Handler 會執行建立付款請求並回傳結果
        var result = await mediator.SendAsync(command);
        
        // ========== 第三步：回傳付款請求模型 ==========
        // 回傳 200 OK 狀態碼和付款請求模型
        // 包含 MerchantID、TradeInfo 和 TradeSha
        // 這些資料可用於導向藍新金流付款頁面
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理藍新金流付款回調的私有方法
    /// 
    /// 職責：
    /// - 透過 Mediator 分發 NewebPayNotifyCommand 命令
    /// - 回傳處理結果
    /// 
    /// 設計考量：
    /// - 將處理邏輯提取為私有方法，提高可讀性和可測試性
    /// - 遵循單一職責原則（SRP）
    /// - 便於未來擴展或修改處理邏輯
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發 NewebPayNotifyCommand 命令
    /// 2. 回傳處理結果
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="command">付款回調命令物件（包含 Status 和 TradeInfo）</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：處理成功
    /// - 400 Bad Request：請求內容錯誤
    /// </returns>
    private static async Task<IResult> HandleNotifyPaymentAsync(
        [FromServices] IMediator mediator,
        [FromForm] NewebPayNotifyCommand command)
    {
        // ========== 第一步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（NewebPayNotifyHandler）
        // Handler 會執行付款回調處理並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第二步：回傳處理結果 ==========
        // 回傳 200 OK 狀態碼
        // 藍新金流會根據這個回應確認是否成功接收回調
        return Results.Ok("OK");
    }
}
