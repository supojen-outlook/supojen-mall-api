namespace Manian.Presentation.Extensions;

/// <summary>
/// CORS 服務的擴充方法
/// 
/// CORS (Cross-Origin Resource Sharing，跨域資源共享)
/// 是一種瀏覽器安全機制，用來控制網頁應用程式能否存取不同來源（網域、通訊協定、連接埠）的資源
/// 
/// 為什麼需要 CORS？
/// 瀏覽器的同源政策 (Same-Origin Policy) 預設禁止跨域請求
/// 但如果前端（如 localhost:5173）和後端（不同連接埠）分離開發，就需要 CORS 來允許跨域存取
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// 新增 Web 應用的 CORS 原則
    /// 
    /// 這個方法定義了一個名為 "web" 的 CORS 原則
    /// 專門為 Web 前端（特別是 SPA 應用）設計
    /// </summary>
    /// <param name="services">IServiceCollection 服務集合，用於註冊 CORS 服務</param>
    /// <returns>IServiceCollection 用於鏈式呼叫，方便串接其他註冊</returns>
    /// <remarks>
    /// 使用範例：
    /// builder.Services.AddWebCors();
    /// </remarks>
    public static IServiceCollection AddWebCors(this IServiceCollection services)
    {
        // 呼叫 ASP.NET Core 內建的 AddCors 服務
        services.AddCors(options =>
        {
            // 定義一個名為 "web" 的 CORS 原則
            // 這個名稱會在後續 UseCors("web") 時使用
            options.AddPolicy("web", policy =>
            {
                // 設定 CORS 原則的詳細規則
                policy
                    // 允許所有來源（開發環境適用）
                    // SetIsOriginAllowed(origin => true) 表示接受任何來源的請求
                    // 注意：生產環境應該限制特定來源，而不是使用 true
                    .SetIsOriginAllowed(origin => true)
                    
                    // 明確指定允許的來源（白名單）
                    // 這裡允許 Vite 開發伺服器的預設連接埠 5173
                    // 如果前端部署在正式網域，需要在這裡加入
                    .WithOrigins(
                        "http://localhost:5173"  // Vite 預設開發伺服器
                    )
                    
                    // 允許所有 HTTP 標頭
                    // 例如：Content-Type, Authorization, X-Requested-With 等
                    .AllowAnyHeader()
                    
                    // 允許所有 HTTP 方法
                    // 例如：GET, POST, PUT, DELETE, PATCH 等
                    .AllowAnyMethod()
                    
                    // 允許攜帶認證資訊（Cookie、Authorization 標頭）
                    // 這對需要登入狀態的 API 非常重要
                    // 注意：AllowCredentials() 不能與 AllowAnyOrigin() 並用
                    // 這裡雖然有 SetIsOriginAllowed(true)，但沒有用 AllowAnyOrigin()，所以沒衝突
                    .AllowCredentials();
            });
        });
        
        return services;
    }
    
    /// <summary>
    /// 使用 Web CORS 原則
    /// 
    /// 這個方法啟用先前註冊的 CORS 中間件
    /// 必須放在 UseRouting() 之後，UseAuthorization() 之前
    /// </summary>
    /// <param name="app">IApplicationBuilder 應用程式建置器</param>
    /// <returns>IApplicationBuilder 用於鏈式呼叫</returns>
    /// <remarks>
    /// 使用範例（在 Program.cs 中）：
    /// app.UseWebCors();  // 必須在 UseAuthorization 之前
    /// app.UseAuthorization();
    /// 
    /// 中間件順序非常重要：
    /// UseRouting() → UseCors() → UseAuthentication() → UseAuthorization() → UseEndpoints()
    /// </remarks>
    public static IApplicationBuilder UseWebCors(this IApplicationBuilder app)
    {
        // 啟用名為 "web" 的 CORS 原則
        // 這個名稱必須與 AddPolicy 時使用的名稱一致
        app.UseCors("web");
        return app;
    }
}