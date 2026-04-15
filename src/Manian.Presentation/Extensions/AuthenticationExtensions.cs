using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Manian.Presentation.Extensions;

/// <summary>
/// 認證擴充方法 - 自訂 Cookie 認證行為
/// 
/// 這個擴充方法解決了傳統 Cookie 認證在 Web API 中的一個常見問題：
/// 當未授權的請求存取 API 時，預設行為是回傳 302 重定向到登入頁面
/// 但 API 客戶端期望的是 401 Unauthorized JSON 回應
/// 
/// 此擴充方法會根據請求類型智慧判斷：
/// - 一般網頁請求 → 302 重定向到登入頁（維持原本行為）
/// - API 請求 → 回傳 401/403 JSON 回應（符合 RESTful 規範）
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// 註冊自訂的 Cookie 認證方案
    /// 
    /// 使用方法：
    /// builder.Services.AddAuthentication(...)
    ///     .Cookie()  // 使用這個擴充方法取代原本的 AddCookie
    /// 
    /// 這樣寫的好處：
    /// 1. 封裝了複雜的設定邏輯
    /// 2. 確保 API 和 MVC 共用同一套認證但行為不同
    /// 3. 可以在不同環境（開發/生產）切換安全設定
    /// </summary>
    /// <param name="builder">AuthenticationBuilder，由 AddAuthentication 傳入</param>
    /// <returns>原 AuthenticationBuilder，支援鏈式呼叫</returns>
    public static AuthenticationBuilder Cookie(this AuthenticationBuilder builder)
    {
        // 註冊名為 "cookie" 的 Cookie 認證方案
        // 注意：這裡用 "cookie" 而非預設的 "Cookies"，可能是為了與預設方案區分
        builder.AddCookie("cookie", options =>
        {
            // ----- Cookie 基本設定 -----
            
            // Cookie 名稱：在瀏覽器中顯示為 "account.cookie"
            // 刻意不叫 ".AspNetCore.Cookies" 來避免被掃描工具識別
            options.Cookie.Name = "account.cookie";

            // HttpOnly = true：禁止 JavaScript 存取此 Cookie
            // 有效防禦 XSS 攻擊竊取 Cookie
            options.Cookie.HttpOnly = true;
            
            // SecurePolicy：決定是否只在 HTTPS 下傳送 Cookie
            // 開發環境設 None（允許 HTTP），生產環境應改為 Always
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            
            // SameSite：防禦 CSRF 攻擊的重要設定
            // Lax：允許頂層導航（點連結）帶 Cookie，但 POST 表單不帶
            // Strict：完全禁止跨站請求帶 Cookie（更安全，但可能影響使用體驗）
            options.Cookie.SameSite = SameSiteMode.Lax;
            
            // ----- 過期時間設定 -----
            
            // Cookie 絕對過期時間：30 分鐘
            // 意思是從登入開始算，30 分鐘後一定會過期
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            
            // 啟用滑動過期
            // 原理：每次請求如果還剩不到一半的時間（<15分鐘），就自動延長到 30 分鐘
            // 效果：只要使用者持續操作，就不會登出；停止操作 30 分鐘後才登出
            options.SlidingExpiration = true;

            // ----- 事件處理（核心邏輯）-----
            options.Events = new CookieAuthenticationEvents
            {
                /// <summary>
                /// 當使用者未登入（未授權）存取需要授權的資源時觸發
                /// 預設行為：回傳 302 Found 並導向登入頁
                /// 
                /// 我們覆寫這個行為：
                /// - 如果是一般網頁請求 → 維持 302 導向登入頁
                /// - 如果是 API 請求 → 回傳 401 Unauthorized JSON
                /// </summary>
                OnRedirectToLogin = context =>
                {
                    // 判斷是否為 API 請求
                    if (IsApiRequest(context.Request))
                    {
                        // API 請求：回傳 401 狀態碼 + JSON 錯誤訊息
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        
                        // 序列化 JSON 回應內容
                        var result = JsonSerializer.Serialize(new
                        {
                            code = 401,
                            title = "請先登入",
                        });
                        
                        // 設定內容類型為 JSON
                        context.Response.ContentType = "application/json";
                        
                        // 寫入回應並回傳 Task
                        return context.Response.WriteAsync(result);
                    }
                    
                    // 非 API 請求：維持原本的 302 重定向行為
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                },
            
                /// <summary>
                /// 當使用者已登入但權限不足時觸發
                /// 例如：一般使用者存取 Admin 專用的頁面
                /// 
                /// 類似上面邏輯：
                /// - 網頁請求 → 302 導向拒絕存取頁面
                /// - API 請求 → 回傳 403 Forbidden JSON
                /// </summary>
                OnRedirectToAccessDenied = context =>
                {
                    if (IsApiRequest(context.Request))
                    {
                        // API 請求：回傳 403 狀態碼
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        
                        var result = JsonSerializer.Serialize(new
                        {
                            code = 403,
                            title = "權限不足",
                        });
                        
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(result);
                    }
                    
                    // 網頁請求：重定向到拒絕存取頁面
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                }
            };
        });
        
        return builder;
    }
    
    /// <summary>
    /// 判斷當前請求是否為 API 請求
    /// 
    /// 判斷依據（滿足任一即視為 API）：
    /// 1. 路徑以 /api 開頭
    /// 2. Accept 標頭包含 application/json（要求 JSON 回應）
    /// 3. Content-Type 標頭包含 application/json（發送 JSON 資料）
    /// 4. X-Requested-With: XMLHttpRequest（Ajax 請求）
    /// </summary>
    /// <param name="request">HTTP 請求物件</param>
    /// <returns>true: 是 API 請求；false: 是一般網頁請求</returns>
    private static bool IsApiRequest(HttpRequest request)
    {
        // 路徑以 /api 開頭（最明確的判斷）
        return request.Path.StartsWithSegments("/api") ||
               // 要求 JSON 格式回應
               request.Headers["Accept"].ToString().Contains("application/json") ||
               // 發送 JSON 格式資料
               request.Headers["Content-Type"].ToString().Contains("application/json") ||
               // Ajax 請求（許多前端框架會自動加上）
               request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}