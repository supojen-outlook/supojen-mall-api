using System.Security.Claims;
using Manian.Application.Commands.Memberships;
using Manian.Application.Commands.Memberships.Signs;
using Manian.Application.Models.Memberships;
using Manian.Application.Queries.Memberships;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Memberships;

/// <summary>
/// 會員相關的 API 端點定義類別
/// 
/// 職責：
/// - 定義會員相關的 RESTful API 端點
/// - 處理 HTTP 請求並回傳適當的回應
/// - 協調應用層的命令和查詢處理器
/// 
/// 設計模式：
/// - 使用 Minimal API 風格（ASP.NET Core 6+）
/// - 遵循 CQRS 模式（命令查詢分離）
/// - 使用 Mediator 模式處理業務邏輯
/// 
/// 端點列表：
/// - GET /api/profile - 獲取當前登入用戶的個人資料
/// - POST /api/signin - 使用密碼登入
/// - GET /api/siginup - 發送註冊驗證碼
/// - POST /api/signup/confirmed - 完成註冊
/// - GET /api/reset-password - 發送重設密碼驗證碼
/// - POST /api/reset-password/confirmed - 完成密碼重設
/// - POST /api/signout - 登出
/// 
/// Scalar 文件：
/// - 使用 Scalar 替代 Swagger UI
/// - 提供更現代化的 API 文件介面
/// - 支援深色模式和更好的互動體驗
/// </summary>
public static class MembershipEndpoint
{
    /// <summary>
    /// 註冊所有會員相關的 API 端點
    /// 
    /// 使用方式：
    /// 在 Program.cs 中呼叫 app.MapMemberships() 即可註冊所有端點
    /// 
    /// 設計考量：
    /// - 使用擴充方法讓端點註冊更模組化
    /// - 每個端點對應一個處理方法，保持單一職責
    /// - 端點命名遵循 RESTful 規範
    /// 
    /// Scalar 整合：
    /// - 端點會自動出現在 Scalar 文件中
    /// - 使用 WithSummary() 和 WithDescription() 提供詳細說明
    /// - 使用 WithTags() 進行分組顯示
    /// </summary>
    /// <param name="app">IEndpointRouteBuilder 實例，用於註冊端點</param>
    public static void MapMemberships(this IEndpointRouteBuilder app)
    {
        // =========================================================================
        // GET /api/profile - 獲取當前登入用戶的個人資料
        // =========================================================================
        app.MapGet("/api/profile", GetProfileAsync)
            .WithName("GetProfile")
            .WithSummary("獲取當前登入用戶的個人資料")
            .WithDescription(@"
                獲取當前登入用戶的個人資料，包含基本資料和點數資訊
                
                認證要求：
                - 必須登入（通過 Cookie）
                - Cookie 名稱：account.cookie
                
                回傳格式：
                - 200 OK：用戶資料（ProfileResponse）
                - 401 Unauthorized：未登入
                - 404 Not Found：用戶不存在
                
                回傳欄位說明：
                - Id：用戶 ID
                - DisplayName：顯示名稱
                - FullName：用戶姓名
                - Email：電子郵件
                - EmailVerified：電子郵件是否已驗證
                - MembershipLevel：會員等級（bronze/silver/gold/vip）
                - Points：獎勵點數餘額
                - Roles：用戶角色
                
                使用範例：
                - GET /api/profile
                
                注意事項：
                - 需要先登入才能訪問此端點
                - 點數資訊來自 PointAccount.Balance
                - 如果用戶已被刪除（軟刪除），會回傳 404
            ")
            .WithTags("會員")
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // POST /api/signin - 使用密碼登入
        // =========================================================================
        app.MapPost("/api/signin", SigninAsync)
            .WithName("Signin")
            .WithSummary("使用密碼登入")
            .WithDescription(@"
                使用電子郵件和密碼登入系統
                
                認證要求：
                - 不需要登入（公開端點）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含 Email 和 Password
                
                必填欄位：
                - Email：電子郵件地址（必須已註冊並驗證）
                - Password：密碼
                
                回傳格式：
                - 200 OK：登入成功（設定 Cookie）
                - 400 Bad Request：密碼錯誤
                - 404 Not Found：帳號不存在
                
                Cookie 設定：
                - Cookie 名稱：account.cookie
                - Cookie 內容：包含用戶 ID 和角色的 Claims
                - Cookie 有效期：由伺服器設定
                
                使用範例：
                POST /api/signin
                {
                    ""Email"": ""user@example.com"",
                    ""Password"": ""password123""
                }
                
                注意事項：
                - 登入成功後會設定 Cookie，後續請求會自動攜帶
                - 密碼錯誤不會明確告知（防止列舉攻擊）
                - 建議使用 HTTPS 傳輸（防止密碼被截取）
            ")
            .WithTags("會員")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // GET /api/signin/line - 使用 Line 帳號登入
        // =========================================================================
        app.MapGet("/api/signin/line", LoginWithLineAsync)
            .WithName("SigninWithLine")
            .WithSummary("使用 Line 帳號登入")
            .WithDescription(@"
                使用 Line 帳號登入系統
                
                認證要求：
                - 不需要登入（公開端點）
                
                請求格式：
                - Query String 參數
                
                可選參數：
                - redirectUri：登入成功後的重定向目標（預設為 /shop）
                
                回應格式：
                - 302 Found：重定向到 Line 授權頁面
                
                Line 登入流程：
                1. 系統重定向到 Line 授權頁面
                2. 用戶在 Line 授權頁面確認授權
                3. Line 重定向回目標 URI，並攜帶授權碼
                4. 系統使用授權碼換取用戶資訊
                5. 系統檢查用戶是否已註冊
                - 已註冊：直接登入
                - 未註冊：自動創建帳號並登入
                6. 系統設定認證 Cookie
                7. 用戶被重定向到目標頁面
                
                使用範例：
                - GET /api/signin/line
                - GET /api/signin/line?redirectUri=/profile
                
                注意事項：
                - 必須在 Program.cs 中設定 Line 認證服務
                - 必須在 Line Developers 平台註冊應用程式
                - 必須設定正確的回調 URI（Callback URL）
            ")
            .WithTags("會員")
            .AllowAnonymous();

        // =========================================================================
        // GET /api/siginup - 發送註冊驗證碼
        // =========================================================================
        app.MapGet("/api/siginup", SignupAsync)
            .WithName("Signup")
            .WithSummary("發送註冊驗證碼")
            .WithDescription(@"
                發送註冊驗證碼到指定的電子郵件
                
                認證要求：
                - 不需要登入（公開端點）
                
                請求格式：
                - Query String 參數
                
                必填參數：
                - email：電子郵件地址（必須尚未註冊）
                
                回傳格式：
                - 200 OK：驗證碼已發送
                - 400 Bad Request：Email 已被註冊
                
                驗證碼說明：
                - 驗證碼會發送到指定的 Email
                - 驗證碼有效期：由伺服器設定（通常 10-30 分鐘）
                - 驗證碼格式：6 位數字
                
                使用範例：
                - GET /api/siginup?email=user@example.com
                
                注意事項：
                - 驗證碼會發送到 Email，請檢查收件箱和垃圾郵件
                - 驗證碼有效期有限，請盡快完成註冊
                - 每個 Email 只能發送一次驗證碼（在有效期內）
            ")
            .WithTags("會員")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // POST /api/signup/confirmed - 完成註冊
        // =========================================================================
        app.MapPost("/api/signup/confirmed", SignupConfirmedAsync)
            .WithName("SignupConfirmed")
            .WithSummary("完成註冊")
            .WithDescription(@"
                使用驗證碼完成註冊流程
                
                認證要求：
                - 不需要登入（公開端點）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含 Email、Password 和 Code
                
                必填欄位：
                - Email：電子郵件地址（必須已發送驗證碼）
                - Password：密碼（至少 8 字元，建議包含大小寫、數字、符號）
                - Code：驗證碼（6 位數字）
                
                回傳格式：
                - 200 OK：註冊成功
                - 400 Bad Request：驗證碼錯誤或 Email 已被註冊
                
                使用範例：
                POST /api/signup/confirmed
                {
                    ""Email"": ""user@example.com"",
                    ""Password"": ""password123"",
                    ""Code"": ""123456""
                }
                
                注意事項：
                - 驗證碼錯誤次數過多會失效
                - 驗證碼有效期有限，過期後需重新發送
                - 註冊成功後會自動設定 EmailVerified = true
                - 註冊成功後不會自動登入，需要再呼叫登入端點
            ")
            .WithTags("會員")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // =========================================================================
        // GET /api/reset-password - 發送重設密碼驗證碼
        // =========================================================================
        app.MapGet("/api/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithSummary("發送重設密碼驗證碼")
            .WithDescription(@"
                發送重設密碼驗證碼到指定的電子郵件
                
                認證要求：
                - 不需要登入（公開端點）
                
                請求格式：
                - Query String 參數
                
                必填參數：
                - email：電子郵件地址（必須已註冊）
                
                回傳格式：
                - 200 OK：驗證碼已發送
                - 404 Not Found：Email 未註冊
                
                驗證碼說明：
                - 驗證碼會發送到指定的 Email
                - 驗證碼有效期：由伺服器設定（通常 10-30 分鐘）
                - 驗證碼格式：6 位數字
                
                使用範例：
                - GET /api/reset-password?email=user@example.com
                
                注意事項：
                - 驗證碼會發送到 Email，請檢查收件箱和垃圾郵件
                - 驗證碼有效期有限，請盡快完成密碼重設
                - 每個 Email 只能發送一次驗證碼（在有效期內）
                - 為了安全，不會明確告知 Email 是否已註冊（建議改進）
            ")
            .WithTags("會員")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // POST /api/reset-password/confirmed - 完成密碼重設
        // =========================================================================
        app.MapPost("/api/reset-password/confirmed", ResetPasswordConfirmedAsync)
            .WithName("ResetPasswordConfirmed")
            .WithSummary("完成密碼重設")
            .WithDescription(@"
                使用驗證碼完成密碼重設流程
                
                認證要求：
                - 不需要登入（公開端點）
                
                請求格式：
                - Content-Type: application/json
                - 請求主體包含 Email、Password、PasswordConfirmed 和 Code
                
                必填欄位：
                - Email：電子郵件地址（必須已發送驗證碼）
                - Password：新密碼（至少 8 字元，建議包含大小寫、數字、符號）
                - PasswordConfirmed：確認密碼（必須與 Password 相同）
                - Code：驗證碼（6 位數字）
                
                回傳格式：
                - 200 OK：密碼重設成功
                - 400 Bad Request：驗證碼錯誤或密碼不一致
                - 404 Not Found：Email 未註冊
                
                使用範例：
                POST /api/reset-password/confirmed
                {
                    ""Email"": ""user@example.com"",
                    ""Password"": ""newpassword123"",
                    ""PasswordConfirmed"": ""newpassword123"",
                    ""Code"": ""123456""
                }
                
                注意事項：
                - 驗證碼錯誤次數過多會失效
                - 驗證碼有效期有限，過期後需重新發送
                - 密碼重設成功後不會自動登入，需要再呼叫登入端點
                - 密碼重設後，舊密碼會失效
            ")
            .WithTags("會員")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // =========================================================================
        // POST /api/signout - 登出
        // =========================================================================
        app.MapPost("/api/signout", SignoutAsync)
            .WithName("Signout")
            .WithSummary("登出")
            .WithDescription(@"
                登出當前用戶，清除認證 Cookie
                
                認證要求：
                - 必須登入（通過 Cookie）
                
                請求格式：
                - 不需要請求主體
                - 認證資訊從 Cookie 中獲取
                
                回傳格式：
                - 200 OK：登出成功
                - 401 Unauthorized：未登入
                
                Cookie 處理：
                - 清除名為 account.cookie 的認證 Cookie
                - Cookie 會從瀏覽器中移除
                - 後續請求將不再攜帶認證資訊
                
                使用範例：
                - POST /api/signout
                
                注意事項：
                - 登出後需要重新登入才能訪問需要認證的端點
                - 登出操作是安全的，即使未登入也不會造成錯誤
                - 建議在前端調用此端點後重定向到登入頁面
            ")
            .WithTags("會員")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    

    // =========================================================================
    // PUT /api/profile - 更新會員個人資料
    // =========================================================================
    app.MapPut("/api/profile", UpdateProfileAsync)
        .WithSummary("更新會員個人資料")
        .WithDescription(@"
            更新當前登入用戶的個人資料
            
            請求內容：
            - MemberUpdateProfileCommand：要更新的欄位（可選）
            
            回傳格式：
            - 200 OK：更新後的用戶資料（ProfileResponse）
            - 401 Unauthorized：未登入
            - 404 Not Found：用戶不存在
            - 400 Bad Request：請求內容錯誤
            
            注意事項：
            - 只更新非 null 的欄位
            - 不允許更新 Email、密碼、點數等敏感欄位
            - 頭像 URL 應該是已上傳的資產 URL
        ")
        .WithTags("會員")
        .Produces<ProfileResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
        
    }

    /// <summary>
    /// 獲取當前登入用戶的個人資料
    /// 
    /// 請求方式：GET /api/profile
    /// 認證要求：需要登入（通過 Cookie）
    /// 回應格式：JSON 格式的 ProfileResponse
    /// 
    /// 執行流程：
    /// 1. 從 HTTP 請求中獲取當前登入用戶的 ID（通過 Cookie）
    /// 2. 使用 ProfileQuery 查詢用戶資料
    /// 3. 回傳用戶資料（包含基本資料和點數資訊）
    /// 
    /// 錯誤處理：
    /// - 用戶未登入：回傳 401 Unauthorized
    /// - 用戶不存在：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送查詢命令</param>
    /// <param name="query">ProfileQuery 查詢物件（不包含任何屬性）</param>
    /// <returns>包含用戶資料的 JSON 回應</returns>
    private static async Task<IResult> GetProfileAsync(
        [FromServices]IMediator mediator,
        [AsParameters] ProfileQuery query)
    {
        // 使用 Mediator 發送 ProfileQuery 查詢
        // Mediator 會自動找到對應的 Handler (ProfileQueryHandler)
        // Handler 會從資料庫查詢用戶資料並映射為 ProfileResponse
        var result = await mediator.SendAsync(query);
        
        // 回傳 200 OK 狀態碼和用戶資料
        return Results.Ok(result);
    }

    /// <summary>
    /// 使用密碼登入
    /// 
    /// 請求方式：POST /api/signin
    /// 認證要求：不需要登入
    /// 請求格式：JSON 格式的 SigninWithPasswordCommand（包含 Email 和 Password）
    /// 回應格式：設定 Cookie（認證憑證）
    /// 
    /// 執行流程：
    /// 1. 驗證 Email 和 Password 是否正確
    /// 2. 建立包含用戶 ID 和角色的 Claims
    /// 3. 使用 Claims 建立身份和主體
    /// 4. 將身份主體寫入 Cookie
    /// 
    /// 錯誤處理：
    /// - Email 或 Password 錯誤：回傳 400 Bad Request 或 404 Not Found
    /// </summary>
    /// <param name="context">HTTP 上下文，用於設定 Cookie</param>
    /// <param name="mediator">Mediator 實例，用於發送登入命令</param>
    /// <param name="command">SigninWithPasswordCommand 命令物件（包含 Email 和 Password）</param>
    /// <returns>無直接回應（通過 Cookie 設定認證狀態）</returns>
    private static async Task SigninAsync(
        HttpContext context,
        [FromServices]IMediator mediator,
        [FromBody]SigninWithPasswordCommand command)
    {
        // ========== 第一步：驗證 Email 和 Password ==========
        // 使用 Mediator 發送 SigninWithPasswordCommand 命令
        // Mediator 會自動找到對應的 Handler (SigninWithPasswordCommandHandler)
        // Handler 會驗證 Email 和 Password，並回傳驗證通過的 User 實體
        var profile = await mediator.SendAsync(command);
        
        // ========== 第二步：建立 Claims（身份聲明）==========
        // Claims 是關於用戶的一組鍵值對，用於識別和授權
        // 這裡建立一個包含用戶 ID 和所有角色的 Claims 列表
        var claims = new List<Claim> { 
            // "sub" (subject) 是 JWT 標準欄位，代表用戶的唯一識別碼
            new Claim("sub", profile.Id.ToString()) 
        };
        
        // 將用戶的所有角色加入 Claims
        // 這樣後續可以通過 [Authorize(Roles = "admin")] 來進行角色授權
        foreach (var role in profile.Roles)
            claims.Add(new Claim("roles", role.Code));

        // ========== 第三步：建立 ClaimsIdentity ==========
        // ClaimsIdentity 是 Claims 的集合，代表一個身份
        // 第二個參數 "cookie" 是身份驗證方案的名稱
        // 這個名稱必須與 Program.cs 中 AddAuthentication("cookie") 設定的名稱一致
        var identity = new ClaimsIdentity(claims, "cookie");

        // ========== 第四步：建立 ClaimsPrincipal ==========
        // ClaimsPrincipal 是身份的容器，可以包含多個身份
        // 在這個簡單的例子中，只包含一個身份
        var principal = new ClaimsPrincipal(identity);

        // ========== 第五步：登入 ==========
        // 將 ClaimsPrincipal 寫入 Cookie
        // 這會在瀏覽器中設定一個名為 "account.cookie" 的 Cookie
        // 後續請求會自動攜帶這個 Cookie，系統可以識別用戶身份
        await context.SignInAsync("cookie", principal);
    }

    /// <summary>
    /// 使用 Line 帳號登入
    /// 
    /// 請求方式：GET /api/signin/line
    /// 認證要求：不需要登入
    /// 請求格式：Query String 中的 redirectUri 參數（可選）
    /// 回應格式：重定向到 Line 授權頁面
    /// 
    /// 執行流程：
    /// 1. 構建登入成功後的目標 URI（預設為 /shop）
    /// 2. 重定向到 Line 授權頁面
    /// 3. 用戶在 Line 授權頁面完成登入
    /// 4. Line 重定向回目標 URI，並攜帶授權碼
    /// 5. 系統使用授權碼換取用戶資訊並完成登入
    /// 
    /// 錯誤處理：
    /// - 用戶取消授權：Line 會重定向回目標 URI，但帶有錯誤參數
    /// - 授權失敗：由 Line 處理，系統會收到錯誤回應
    /// 
    /// Line 登入流程：
    /// 1. 用戶點擊「使用 Line 登入」按鈕
    /// 2. 系統重定向到 Line 授權頁面
    /// 3. 用戶在 Line 授權頁面確認授權
    /// 4. Line 重定向回目標 URI，並攜帶授權碼
    /// 5. 系統使用授權碼換取用戶資訊
    /// 6. 系統檢查用戶是否已註冊
    ///    - 已註冊：直接登入
    ///    - 未註冊：自動創建帳號並登入
    /// 7. 系統設定認證 Cookie
    /// 8. 用戶被重定向到目標頁面（/shop）
    /// 
    /// 使用範例：
    /// - GET /api/signin/line
    /// - GET /api/signin/line?redirectUri=/profile
    /// 
    /// 注意事項：
    /// - 必須在 Program.cs 中設定 Line 認證服務
    /// - 必須在 Line Developers 平台註冊應用程式
    /// - 必須設定正確的回調 URI（Callback URL）
    /// - 目標 URI 必須是應用程式內的有效路徑
    /// </summary>
    /// <param name="context">HTTP 上下文，用於構建目標 URI 和重定向</param>
    /// <param name="redirectUri">登入成功後的重定向目標（可選），預設為 /shop</param>
    /// <returns>重定向到 Line 授權頁面</returns>
    private static async Task LoginWithLineAsync(HttpContext context)
    {
        // ========== 第一步：構建目標 URI ==========
        string targetUri;
        
        // 檢查當前環境是否為開發環境
        if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            // 開發環境：直接使用固定的開發伺服器地址
            // 這樣可以確保在開發過程中，登入後總是重定向到正確的前端開發伺服器
            targetUri = "http://localhost:3000/shop/";
        }
        else
        {
            // 生產/測試環境：使用當前請求的協議和主機
            // 使用 context.Request.Scheme 取得當前請求的協議（http 或 https）
            // 使用 context.Request.Host 取得當前請求的主機名稱和連接埠
            // 組合成完整的 URL，如 https://yourdomain.com/shop
            // 
            // 為什麼要這樣做？
            // - 確保重定向 URI 使用與當前請求相同的協議和主機
            // - 避免硬編碼 URL，提高可移植性
            // - 支援多環境部署（開發、測試、生產）
            targetUri = $"{context.Request.Scheme}://{context.Request.Host}/shop/";
        }

        // ========== 第二步：發起 Line 認證挑戰 ==========
        // 使用 context.ChallengeAsync() 方法發起 OAuth 2.0 認證流程
        // 第一個參數 "line" 是認證方案的名稱
        // 這個名稱必須與 Program.cs 中 AddLineLogin() 設定的名稱一致
        // 
        // AuthenticationProperties 用於設定認證行為：
        // - RedirectUri：認證成功後的重定向目標
        // - 其他屬性：如 IsPersistent（是否持久化 Cookie）等
        // 
        // 這個方法會：
        // 1. 生成 Line 授權 URL
        // 2. 重定向用戶到 Line 授權頁面
        // 3. 用戶完成授權後，Line 會重定向回 RedirectUri
        // 4. 系統會自動處理回調並完成登入
        await context.ChallengeAsync("line", new AuthenticationProperties()
        {
            RedirectUri = targetUri
        });
    }

    /// <summary>
    /// 發送註冊驗證碼
    /// 
    /// 請求方式：GET /api/siginup
    /// 認證要求：不需要登入
    /// 請求格式：Query String 中的 Email 參數
    /// 回應格式：200 OK（驗證碼會發送到 Email）
    /// 
    /// 執行流程：
    /// 1. 檢查 Email 是否已被註冊
    /// 2. 生成驗證碼並發送到 Email
    /// 
    /// 錯誤處理：
    /// - Email 已被註冊：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送註冊命令</param>
    /// <param name="command">SignupCommand 命令物件（包含 Email）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> SignupAsync(
        [FromServices]IMediator mediator, 
        [AsParameters]SignupCommand command)
    {
        // 使用 Mediator 發送 SignupCommand 命令
        // Mediator 會自動找到對應的 Handler (SignupWithEmailCommandHandler)
        // Handler 會檢查 Email 是否已被註冊，並發送驗證碼
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 完成註冊
    /// 
    /// 請求方式：POST /api/signup/confirmed
    /// 認證要求：不需要登入
    /// 請求格式：JSON 格式的 SignupConfirmedCommand（包含 Email、Password、Code）
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 驗證 Email、Password 和驗證碼
    /// 2. 創建新用戶
    /// 
    /// 錯誤處理：
    /// - 驗證碼錯誤：回傳 400 Bad Request
    /// - Email 已被註冊：回傳 400 Bad Request
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送註冊確認命令</param>
    /// <param name="command">SignupConfirmedCommand 命令物件（包含 Email、Password、Code）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> SignupConfirmedAsync(
        [FromServices]IMediator mediator, 
        [FromBody]SignupConfirmedCommand command)
    {
        // 使用 Mediator 發送 SignupConfirmedCommand 命令
        // Mediator 會自動找到對應的 Handler
        // Handler 會驗證驗證碼並創建新用戶
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 發送重設密碼驗證碼
    /// 
    /// 請求方式：GET /api/reset-password
    /// 認證要求：不需要登入
    /// 請求格式：Query String 中的 Email 參數
    /// 回應格式：200 OK（驗證碼會發送到 Email）
    /// 
    /// 執行流程：
    /// 1. 檢查 Email 是否已註冊
    /// 2. 生成驗證碼並發送到 Email
    /// 
    /// 錯誤處理：
    /// - Email 未註冊：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送重設密碼命令</param>
    /// <param name="command">ResetPasswordCommand 命令物件（包含 Email）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> ResetPasswordAsync(
        [FromServices]IMediator mediator, 
        [AsParameters]ResetPasswordCommand command)
    {
        // 使用 Mediator 發送 ResetPasswordCommand 命令
        // Mediator 會自動找到對應的 Handler
        // Handler 會檢查 Email 是否已註冊，並發送驗證碼
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 完成密碼重設
    /// 
    /// 請求方式：POST /api/reset-password/confirmed
    /// 認證要求：不需要登入
    /// 請求格式：JSON 格式的 ResetPasswordConfirmedCommand（包含 Email、Password、PasswordConfirmed、Code）
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 驗證 Email、Password 和驗證碼
    /// 2. 更新用戶密碼
    /// 
    /// 錯誤處理：
    /// - 驗證碼錯誤：回傳 400 Bad Request
    /// - Email 未註冊：回傳 404 Not Found
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送重設密碼確認命令</param>
    /// <param name="command">ResetPasswordConfirmedCommand 命令物件（包含 Email、Password、PasswordConfirmed、Code）</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> ResetPasswordConfirmedAsync(
        [FromServices]IMediator mediator, 
        [AsParameters]ResetPasswordConfirmedCommand command)
    {
        // 使用 Mediator 發送 ResetPasswordConfirmedCommand 命令
        // Mediator 會自動找到對應的 Handler
        // Handler 會驗證驗證碼並更新用戶密碼
        await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 登出當前用戶
    /// 
    /// 請求方式：POST /api/signout
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：不需要請求主體
    /// 回應格式：200 OK
    /// 
    /// 執行流程：
    /// 1. 使用 Mediator 處理登出命令（預留給未來的業務邏輯）
    /// 2. 清除認證 Cookie
    /// 
    /// 錯誤處理：
    /// - 用戶未登入：回傳 401 Unauthorized
    /// </summary>
    /// <param name="context">HTTP 上下文，用於清除 Cookie</param>
    /// <param name="mediator">Mediator 實例，用於發送登出命令</param>
    /// <returns>200 OK 狀態碼</returns>
    private static async Task<IResult> SignoutAsync(
        HttpContext context,
        [FromServices]IMediator mediator)
    {
        // 使用 Mediator 發送 SignoutCommand 命令
        // 目前這個命令不執行任何業務邏輯，但預留給未來可能需要的功能
        // 例如：記錄登出日誌、更新使用者最後登出時間等
        await mediator.SendAsync(new SignoutCommand());
        
        // 清除認證 Cookie
        // 這會從瀏覽器中移除名為 "account.cookie" 的 Cookie
        // 後續請求將不再攜帶認證資訊，用戶需要重新登入
        await context.SignOutAsync("cookie");
        
        // 回傳 200 OK 狀態碼
        return Results.Ok();
    }

    /// <summary>
    /// 更新會員個人資料
    /// 
    /// 請求方式：PUT /api/profile
    /// 認證要求：需要登入（通過 Cookie）
    /// 請求格式：JSON 格式的 MemberUpdateProfileCommand
    /// 回應格式：200 OK（回傳更新後的 ProfileResponse）
    /// 
    /// 執行流程：
    /// 1. 接收請求主體（MemberUpdateProfileCommand）
    /// 2. 使用 Mediator 發送 MemberUpdateProfileCommand 命令
    /// 3. 回傳更新後的用戶資料
    /// 
    /// 錯誤處理：
    /// - 未登入：回傳 401 Unauthorized
    /// - 用戶不存在：回傳 404 Not Found
    /// - 資料格式錯誤：回傳 400 Bad Request
    /// 
    /// 使用範例：
    /// PUT /api/profile
    /// {
    ///     "displayName": "新暱稱",
    ///     "fullName": "王小明",
    ///     "birthDate": "1990-01-01",
    ///     "gender": "male",
    ///     "avatar": "/assets/123.jpg"
    /// }
    /// </summary>
    /// <param name="mediator">Mediator 實例，用於發送更新命令</param>
    /// <param name="command">MemberUpdateProfileCommand 命令物件（包含要更新的欄位）</param>
    /// <returns>200 OK 狀態碼和更新後的 ProfileResponse</returns>
    private static async Task<IResult> UpdateProfileAsync(
        [FromServices] IMediator mediator,
        [FromBody] MemberUpdateProfileCommand command)
    {
        // 使用 Mediator 發送 MemberUpdateProfileCommand 命令
        // Mediator 會自動找到對應的 Handler (MemberUpdateProfileCommandHandler)
        // Handler 會更新用戶資料並回傳更新後的 ProfileResponse
        var profile = await mediator.SendAsync(command);
        
        // 回傳 200 OK 狀態碼和更新後的用戶資料
        return Results.Ok(profile);
    }

}
