using System;
using Microsoft.AspNetCore.Antiforgery;

namespace Manian.Presentation.Endpoints;

public static class SystemEndpoint
{
    public static void MapSystems(this IEndpointRouteBuilder app)
    {
        // ---------------------------------------------------------------------------
        // GET /api/antiforgery/token - 獲取防偽令牌
        // ---------------------------------------------------------------------------
        // 功能說明：
        // - 提供一個公開端點供前端獲取防偽令牌
        // - 執行時會自動將令牌寫入瀏覽器的 Cookie
        // - 同時在回應中返回令牌值，方便前端直接使用
        // 
        // 使用場景：
        // - 前端應用 (SPA) 初始化時調用
        // - 用戶登入前調用 (因為設置了 AllowAnonymous)
        // - 需要執行 POST/PUT/DELETE 操作前調用
        // 
        // 安全性考量：
        // - 雖然設置了 AllowAnonymous，但生成的令牌與 Session 綁定
        // - 攻擊者無法利用此端點獲取他人的有效令牌
        // - HttpOnly 設為 false 時，前端需確保妥善保管令牌
        // 
        // 前端整合範例：
        // 1. 前端發送 GET 請求至 /api/antiforgery/token
        // 2. 從回應 JSON 中取得 "token" 欄位
        // 3. 將此 token 存入記憶體或 Store (如 Redux/Pinia)
        // 4. 在發送 POST/PUT/DELETE 請求時，將此 token 放入 Header "X-CSRF-TOKEN"
        // 
        // 注意事項：
        // - 此端點必須在 app.UseAntiforgery() 之後註冊
        // - 如果 Cookie 設置了 HttpOnly = true，前端無法讀取 Cookie，只能依賴回應中的 token
        app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
        {
            // 1. 生成並儲存防偽令牌
            // GetAndStoreTokens 方法會做兩件事：
            //   a. 生成一組新的請求令牌
            //   b. 將令牌寫入回應的 Set-Cookie 標頭中 (瀏覽器會自動儲存)
            var tokens = antiforgery.GetAndStoreTokens(context);
            
            // 2. 將令牌回傳給前端
            // 雖然 Cookie 已經發送，但直接回傳 Token 值可以方便前端直接使用
            // 避免前端需要解析 Cookie 的麻煩
            return Results.Ok(new 
            { 
                requestToken = tokens.RequestToken, // 實際的防偽令牌字串
            });
        })
        .WithSummary("獲取防偽令牌")
        .WithDescription(@"
            獲取用於 CSRF 防護的防偽令牌
            
            功能說明：
            - 生成並返回一組新的防偽令牌
            - 令牌會同時寫入 Cookie 和回應主體
            - 用於保護 POST/PUT/DELETE 等狀態改變的操作
            
            使用場景：
            - 前端應用初始化時調用
            - 用戶登入前調用
            - 需要執行寫入操作前調用
            
            回應格式：
            - 200 OK：包含防偽令牌的 JSON 物件
            
            使用範例：
            - GET /api/antiforgery/token
            
            前端整合：
            1. 發送 GET 請求至此端點
            2. 從回應中取得 requestToken
            3. 在後續 POST/PUT/DELETE 請求中將此令牌放入 X-CSRF-TOKEN 標頭
            
            安全性說明：
            - 令牌與當前請求上下文綁定
            - 攻擊者無法利用此端點獲取他人的有效令牌
            - 即使允許匿名訪問，也不會降低安全性
        ")
        .WithTags("系統")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous(); 
    
        // 映射一個 GET 請求處理器到路徑 "/health"
        // 這是標準的健康檢查端點，用於容器編排平台（如 Kubernetes、Docker Swarm）
        // 回傳 200 OK 狀態碼和 JSON 格式的狀態資訊，表示應用程式正常運行
        // AllowAnonymous 屬性允許未經認證的訪問，因為健康檢查不應該需要登入
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",            // 服務狀態：healthy（正常）、degraded（降級）、unhealthy（異常）
            timestamp = DateTime.UtcNow,   // 目前 UTC 時間，用於檢查服務響應時間
            service = "Manian API",        // 服務名稱，便於區分不同服務
            version = "1.0.0"              // 服務版本，便於追蹤部署版本
        }))
        .WithSummary("健康檢查")
        .WithDescription(@"
            檢查 API 服務的運行狀態
            
            功能說明：
            - 提供一個簡單的端點用於檢查服務是否正常運行
            - 回傳服務狀態、時間戳、服務名稱和版本資訊
            - 不需要身份驗證，允許匿名訪問
            
            使用場景：
            - 容器編排平台（Kubernetes、Docker Swarm）的健康檢查
            - 負載均衡器的健康探測
            - 監控系統的服務可用性檢查
            - CI/CD 流程中的部署驗證
            
            回應格式：
            - 200 OK：服務正常運行
            - 回應內容包含：
            * status：服務狀態（healthy/degraded/unhealthy）
            * timestamp：UTC 時間戳
            * service：服務名稱
            * version：服務版本
            
            使用範例：
            - GET /health
            
            回應範例：
            ```json
            {
                ""status"": ""healthy"",
                ""timestamp"": ""2024-01-01T00:00:00Z"",
                ""service"": ""Manian API"",
                ""version"": ""1.0.0""
            }
            ```
            
            設計考量：
            - 端點路徑簡短，符合健康檢查的慣例
            - 回應資訊簡潔，便於自動化工具解析
            - 允許匿名訪問，確保在認證服務異常時仍可檢查
            - 使用 UTC 時間，避免時區問題
            
            擴展建議：
            - 未來可加入資料庫連接狀態檢查
            - 可加入外部服務依賴的可用性檢查
            - 可加入資源使用情況（CPU、記憶體）的報告
        ")
        .WithTags("系統")
        .Produces<object>(StatusCodes.Status200OK)
        .AllowAnonymous();    
    }
}
