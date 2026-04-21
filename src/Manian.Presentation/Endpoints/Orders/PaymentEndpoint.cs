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
        .WithTags("藍新金流")
        
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
        .WithTags("藍新金流")
        
        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous()
        .DisableAntiforgery();

        // =========================================================================
        // GET /api/payment/return - 藍新金流付款返回
        // =========================================================================

        // 定義 GET 端點，路由為 /api/payment/return
        app.MapPost("/api/payment/return", HandlePaymentReturnAsync)

        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("藍新金流付款返回")

        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            接收藍新金流平台的付款返回請求
            
            查詢參數：
            - MerchantOrderNo：訂單編號（必填）
            - TradeNo：藍新交易序號（必填）
            - Status：付款狀態（必填）
            - Amt：付款金額（必填）
            
            回傳格式：
            - 302 Redirect：導向不同頁面
            
            使用範例：
            - GET /api/payment/return?MerchantOrderNo=ORD20240101001&TradeNo=240101001&Status=SUCCESS&Amt=1000
            
            說明：
            - 這是使用者從藍新金流付款頁面返回的端點
            - 會根據訂單狀態導向不同頁面
            - 訂單不存在：導向 /shop/checkout/notfound
            - 訂單已付款：導向 /shop/checkout/success
            - 訂單處理中：導向 /shop/checkout/processing
        ")

        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("藍新金流")

        // 產生 OpenAPI 回應定義
        .Produces(StatusCodes.Status302Found)
        .Produces(StatusCodes.Status404NotFound)
        .AllowAnonymous()
        .DisableAntiforgery();

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
    /// - 接收並驗證藍新金流平台的付款回調通知
    /// - 手動解析表單數據，避免自動 URL 解碼
    /// - 透過 Mediator 分發處理命令
    /// - 回傳藍新金流標準格式的回應
    /// 
    /// 設計考量：
    /// - 直接注入 HttpRequest 以獲取原始表單數據
    /// - 手動解析表單數據，避免 ASP.NET Core 的自動 URL 解碼
    /// - 使用藍新金流標準回應格式 "1|OK"
    /// - 支援匿名訪問（由端點配置的 AllowAnonymous 控制）
    /// 
    /// 執行流程：
    /// 1. 驗證請求內容類型
    /// 2. 讀取表單數據
    /// 3. 手動建立命令物件
    /// 4. 透過 Mediator 分發命令
    /// 5. 回傳處理結果
    /// 
    /// 錯誤處理：
    /// - 內容類型不正確：回傳 400 Bad Request
    /// - 處理失敗：由 Handler 拋出例外
    /// 
    /// 安全性考量：
    /// - 藍新金流會驗證回應格式是否為 "1|OK"
    /// - TradeInfo 經過 AES 加密，需解密後才能使用
    /// - 必須驗證 Status 是否為 "SUCCESS"
    /// - 必須驗證金額是否正確，防止金額被篡改
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發命令請求</param>
    /// <param name="request">HTTP 請求物件，用於讀取原始表單數據</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：處理成功，回傳 "1|OK"
    /// - 400 Bad Request：請求內容錯誤
    /// </returns>
    private static async Task<IResult> HandleNotifyPaymentAsync(
        [FromServices] IMediator mediator,
        HttpRequest request)
    {
        // ========== 第一步：驗證請求內容類型 ==========
        // 檢查請求的 Content-Type 是否為表單數據
        // 藍新金流回調使用 application/x-www-form-urlencoded 格式
        if (!request.HasFormContentType) 
            return Results.BadRequest("Invalid Content Type");

        // ========== 第二步：讀取表單數據 ==========
        // 使用 ReadFormAsync() 讀取表單數據
        // 這會返回一個 IFormCollection 物件，包含所有表單欄位
        var form = await request.ReadFormAsync();

        // ========== 第三步：手動建立命令物件 ==========
        // 手動從表單數據中提取欄位值，避免 ASP.NET Core 的自動 URL 解碼
        // 這很重要，因為 TradeInfo 已經是加密數據，不應該被解碼
        var command = new NewebPayNotifyCommand
        {
            // Status：回應狀態
            Status = form["Status"]!,
            
            // TradeInfo：加密的交易資訊
            TradeInfo = form["TradeInfo"]!,
            
            // TradeSha：交易檢查碼
            TradeSha = form["TradeSha"]!
        };

        // ========== 第四步：透過 Mediator 分發命令 ==========
        // Mediator 會找到對應的 Handler（NewebPayNotifyHandler）
        // Handler 會執行付款回調處理並回傳結果
        await mediator.SendAsync(command);
        
        // ========== 第五步：回傳處理結果 ==========
        // 回傳 "1|OK"，這是藍新金流標準格式的成功回應
        // 藍新金流會根據這個回應確認是否成功接收回調
        // 格式必須嚴格為 "1|OK"，否則藍新金流會認為回調失敗
        return Results.Ok("1|OK");
    }

    /// <summary>
    /// 處理藍新金流付款返回的私有方法
    /// 
    /// 職責：
    /// - 接收藍新金流付款返回的請求
    /// - 查詢訂單狀態
    /// - 根據訂單狀態導向不同頁面
    /// 
    /// 設計考量：
    /// - 使用 Mediator 分發查詢請求
    /// - 根據訂單狀態導向不同頁面
    /// - 提供友好的使用者體驗
    /// 
    /// 執行流程：
    /// 1. 透過 Mediator 分發查詢請求
    /// 2. 檢查訂單是否存在
    /// 3. 根據訂單狀態導向不同頁面
    /// 
    /// 錯誤處理：
    /// - 訂單不存在：導向 /shop/checkout/notfound
    /// - 訂單已付款：導向 /shop/checkout/success
    /// - 訂單處理中：導向 /shop/checkout/processing
    /// 
    /// 使用場景：
    /// - 使用者在藍新金流付款頁面完成付款後返回
    /// - 使用者在藍新金流付款頁面取消付款後返回
    /// - 藍新金流自動返回（如付款超時）
    /// </summary>
    /// <param name="mediator">Mediator 服務，用於分發查詢請求</param>
    /// <param name="command">藍新金流返回命令物件，包含 MerchantOrderNo 等參數</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 302 Redirect：導向不同頁面
    /// </returns>
    public static async Task<IResult> HandlePaymentReturnAsync(
        HttpRequest request,
        [FromServices] IMediator mediator)
    {
        // 建立 Command 實例
        var command = new NewebPayReturnCommand
        {
            // 1. 優先從 Query String 抓取 (自動綁定)
            Status = request.Query["Status"],
            MerchantID = request.Query["MerchantID"]
        };

        // 2. 如果有 Form Body，從 Body 補齊加密參數
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            command.TradeInfo = form["TradeInfo"].ToString() ?? command.TradeInfo;
            command.TradeSha = form["TradeSha"].ToString() ?? command.TradeSha;
            // 有些版本 Status 也会放在 Form
            if (string.IsNullOrEmpty(command.Status)) command.Status = form["Status"];
        }

        // 執行你的 Handler
        var order = await mediator.SendAsync(command);

        // ========== 第二步：檢查訂單是否存在 ==========
        if(order == null)
        {
            // 如果訂單不存在，導向找不到頁面
            return Results.Redirect("/shop/checkout/notfound");
        }
        else
        {
            // ========== 第三步：根據訂單狀態導向不同頁面 ==========
            if(order.Status == "paid")
            {
                // 如果訂單已付款，導向成功頁面
                return Results.Redirect($"/shop/checkout/success?orderNo={order.OrderNumber}");
            }
            else
            {
                // 如果訂單處理中，導向處理中頁面
                return Results.Redirect($"/shop/checkout/processing?orderNo={order.OrderNumber}");
            }
        }
    }
}
