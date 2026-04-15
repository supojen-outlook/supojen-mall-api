using Microsoft.AspNetCore.Http;
using Po.Api.Response;
using Manian.Application.Services;

namespace Manian.Infrastructure.Services;

/// <summary>
/// 當前請求的使用者身份服務
/// 負責從 HTTP Context 中解析出已認證使用者的 ID
/// 實現 IUserClaim 介面，供應用層和領域層存取目前登入使用者資訊
/// </summary>
public class UserClaim : IUserClaim
{
    /// <summary>
    /// 認證用戶的唯一識別碼
    /// 
    /// 這個屬性是從 JWT Token 中的 "sub" (subject) 宣告解析而來
    /// init 表示這個屬性只能在建構函式中設定，初始化後就無法修改
    /// 確保使用者身份在整個請求生命週期中保持一致
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 建構函式 - 從 HTTP Context 解析目前登入的使用者 ID
    /// </summary>
    /// <param name="httpContextAccessor">HTTP Context 存取器，由 DI 容器注入</param>
    /// <exception cref="Exception">當無法取得 HttpContext 時拋出</exception>
    /// <exception cref="Failure">當使用者未認證或 sub 格式錯誤時拋出 Unauthorized 例外</exception>
    public UserClaim(IHttpContextAccessor httpContextAccessor)
    {
        // 1. 從 IHttpContextAccessor 取得目前的 HttpContext
        //    IHttpContextAccessor 是 ASP.NET Core 提供的服務，可以在非 Controller 的類別中存取當前的 HTTP 請求資訊
        var context = httpContextAccessor.HttpContext;
        
        // 2. 檢查是否能取得 HttpContext
        //    如果 context 為 null，表示這個服務可能在背景任務或沒有 HTTP 請求的環境中被呼叫
        //    這種情況下不應該使用 UserClaim，所以拋出例外
        if (context == null) 
            throw new Exception("Cannot found HttpContext");
        
        // 3. 從使用者的 Claims 中尋找 "sub" (subject) 宣告
        //    sub 是 JWT 標準欄位，代表使用者的唯一識別碼
        //    FindFirst 會回傳第一個符合的 Claim，如果不存在則回傳 null
        //    ?.Value 是 null 條件運算子，如果 sub 為 null，則 Value 不會被存取，整個表達式結果為 null
        var sub = context.User.FindFirst("sub")?.Value;
        
        // 4. 檢查 sub 是否存在
        //    如果 sub 為 null，表示這個請求沒有攜帶有效的 JWT Token，或是 Token 中沒有 sub 宣告
        //    這代表使用者未通過認證，所以回傳 401 Unauthorized
        if(sub == null)
            throw Failure.Unauthorized();
    
        // 5. 將 sub 的字串值轉換為 long 型別
        //    JWT 中的 sub 通常是字串，但我們系統中使用者 ID 是 long 型別
        //    TryParse 會嘗試轉換，如果成功則回傳 true，並將轉換結果存入 userId
        //    如果轉換失敗（例如 sub 是 "abc123" 這種非數字字串），表示 Token 格式不正確
        if(!int.TryParse(sub, out var userId))
            throw Failure.Unauthorized();

        // 6. 將解析出的使用者 ID 存入唯讀屬性 Id
        //    這個值後續可以透過 IUserClaim.Id 在任何需要知道「目前是誰在操作」的地方使用
        Id = userId;
    }
}