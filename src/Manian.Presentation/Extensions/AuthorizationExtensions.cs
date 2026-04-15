using Microsoft.AspNetCore.Authorization;

namespace Manian.Presentation.Extensions;

/// <summary>
/// AuthorizationOptions 的擴充方法，用於設定全域授權原則
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// 設定備用原則 (Fallback Policy)
    /// 當端點上完全沒有任何 [Authorize] 屬性時，會自動套用此原則
    /// </summary>
    /// <param name="options">AuthorizationOptions 配置物件</param>
    /// <remarks>
    /// 觸發時機：端點沒有 [Authorize]、[AllowAnonymous] 等任何授權屬性
    /// 作用範圍：所有未明確標註授權狀態的端點（如靜態檔案、健康檢查等）
    /// 注意事項：如果設定了 FallbackPolicy，它會套用到「所有」未標註的端點
    /// </remarks>
    public static void Fallback(this AuthorizationOptions options)
    {
        // 建立一個新的授權原則建置器
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            // 指定使用的驗證方案名稱，這裡使用自定義的 "cookie"
            // 告訴系統要用哪個 Authentication Scheme 來驗證使用者
            .AddAuthenticationSchemes("cookie")
            
            // 要求使用者必須通過驗證（已登入）
            // 這是最基本的授權要求
            .RequireAuthenticatedUser()
            
            // 要求使用者的 Claims 中必須包含 "sub" (subject) 聲明
            // "sub" 通常是使用者的唯一識別碼（如 UserId）
            // 這確保了不只是已登入，還要是一個有效的使用者
            .RequireClaim("sub")
            
            // 建置原則
            .Build();
        
        // 這段程式碼的效果：
        // 如果開發者「忘記」在 Controller 或 Action 上加 [Authorize]
        // 系統會自動要求：
        // 1. 必須使用 cookie 驗證
        // 2. 必須是已驗證的使用者
        // 3. 必須有 sub claim
    }
    
    /// <summary>
    /// 設定預設原則 (Default Policy)
    /// 當端點有 [Authorize] 但未指定 Policy 名稱時，會自動套用此原則
    /// </summary>
    /// <param name="options">AuthorizationOptions 配置物件</param>
    /// <remarks>
    /// 觸發時機：使用 [Authorize] 屬性但沒有指定 Policy，例如 [Authorize] 而不是 [Authorize(Policy = "MyPolicy")]
    /// 作用範圍：所有使用簡寫 [Authorize] 的端點
    /// 預設行為：如果不安裝，預設是 RequireAuthenticatedUser()
    /// </remarks>
    public static void Default(this AuthorizationOptions options)
    {
        // 建立一個新的授權原則建置器
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            // 指定使用的驗證方案名稱 "cookie"
            // 這會覆蓋預設的驗證方案
            .AddAuthenticationSchemes("cookie")
            
            // 要求使用者必須通過驗證
            .RequireAuthenticatedUser()
            
            // 要求必須有 sub claim
            .RequireClaim("sub")
            
            // 建置原則
            .Build();
        
        // 這段程式碼的效果：
        // 當開發者寫 [Authorize] 時，相當於：
        // [Authorize(Policy = "預設原則")]
        // 系統會要求：
        // 1. 必須使用 cookie 驗證
        // 2. 必須是已驗證的使用者
        // 3. 必須有 sub claim
    }
}