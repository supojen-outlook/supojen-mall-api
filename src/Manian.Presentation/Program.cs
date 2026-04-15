// ============================================
// 命名空間引用區域
// ============================================

// 引入 Scalar.AspNetCore 命名空間
// Scalar 是一個用於顯示 OpenAPI 規範的現代化 API 文檔 UI
// 提供比 Swagger 更美觀、更易用的 API 文檔介面
using Scalar.AspNetCore;

// 引入 ASP.NET Core 授權相關功能
// 用於控制用戶對 API 端點的訪問權限
using Microsoft.AspNetCore.Authorization;

// 引入自定義的 API 回應模型
// 統一 API 回應的格式和結構
using Po.Api.Response;

// 引入應用程式層
// 包含業務邏輯和服務接口的實現
using Manian.Application;

// 引入基礎設施層
// 包含資料庫訪問、外部服務等實現
using Manian.Infrastructure;

// 引入展示層的擴展方法
// 包含自定義的中間件和服務配置擴展
using Manian.Presentation.Extensions;
using Manian.Presentation.Endpoints;
using Manian.Domain;
using Manian.Presentation.Endpoints.Memberships;
using Manian.Presentation.Endpoints.Orders;
using Manian.Presentation.Endpoints.Products;
using Manian.Presentation.Endpoints.Promotions;
using Manian.Presentation.Endpoints.Warehouses;

// ============================================
// 應用程式建置階段 (Dependency Injection 配置)
// ============================================

// 創建 Web 應用程序的構建器
// args 是命令行參數，通常用於配置應用程序的啟動行為
// 例如指定監聽端口、運行環境等
var builder = WebApplication.CreateBuilder(args);

// 從環境變數中讀取應用程式目錄
// ASPNETCORE_DIRECTORY 可用於指定配置文件所在目錄
var dir = Environment.GetEnvironmentVariable("ASPNETCORE_DIRECTORY");

// 從環境變數中讀取運行環境
// ASPNETCORE_ENVIRONMENT 通常為 Development、Staging 或 Production
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

// 配置應用程式的設定來源
// 按優先順序依次從以下來源讀取配置：
// 1. appsettings.json - 基礎配置文件
// 2. appsettings.{env}.json - 環境特定配置文件（如 appsettings.Development.json）
// 3. 環境變數 - 最高優先級，適合容器化部署
builder.Configuration
    .SetBasePath(dir ?? Directory.GetCurrentDirectory())  // 設定配置文件基礎路徑
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)  // 讀取基礎配置
    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)  // 讀取環境特定配置
    .AddEnvironmentVariables();  // 讀取環境變數

// 添加 OpenAPI 支持到服務容器
// OpenAPI 是一種用於描述 REST API 的規範
// 讓 API 可以被自動化地文檔化、測試和生成客戶端代碼
// 這會生成一個 OpenAPI 規範端點，通常在 /openapi/v1.json
builder.Services.AddOpenApi();

// 註冊應用程式所需的核心服務
// AddHttpContextAccessor: 提供 HttpContext 的訪問能力
// AddDomain: 註冊 Domain 層的服務（實體、值物件、聚合根等）
// AddApplication: 註冊應用程式層的服務（業務邏輯）
// AddInfrastructure: 註冊基礎設施層的服務（資料庫、外部服務等）
builder.Services
    .AddHttpContextAccessor()
    .AddDomain()
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

// 配置身份驗證服務
// 使用 Cookie 身份驗證方案
// "cookie" 是此驗證方案的名稱，可用於區分多個驗證方案
builder.Services.AddAuthentication("cookie").Cookie();

// 配置授權服務
// o.Fallback(): 設定默認的授權策略
// 當端點未指定特定授權策略時，將使用此默認策略
builder.Services.AddAuthorization(o => {
    o.Fallback(); 
});

// 配置 CORS (跨來源資源共享) 策略
// "web" 是策略名稱，在 UseCors 中會使用
// 允許 Web 前端應用程式跨域訪問此 API
builder.Services.AddWebCors(); 

// 配置防偽 服務
// 
// 功能說明：
// - 註冊並配置防偽 相關服務
// - 設定 Token 的傳輸方式、Cookie 屬性與有效期
// - 必須搭配 app.UseAntiforgery() 中間件使用
// 
// 設定詳解：
// - HeaderName：指定前端在請求標頭 中攜帶 Token 的欄位名稱
// - Cookie.Name：指定瀏覽器儲存 Token 的 Cookie 名稱
// - Cookie.HttpOnly：設定為 false，允許 JavaScript (前端) 讀取 Cookie 以獲取 Token
// - Cookie.SameSite：設定為 Lax，允許跨站 GET 請求攜帶 Cookie
// - Cookie.SecurePolicy：設定為 None，允許在非 HTTPS (HTTP) 環境下傳輸 (僅用於開發環境)
// - Cookie.MaxAge：指定 Token 的有效期限
// 
// 前端整合：
// - 前端需透過 document.cookie 讀取名為 ".AspNetCore.Antiforgery" 的 Cookie
// - 將讀取到的 Token 放入請求標頭 "X-CSRF-TOKEN" 中
// - 在發送 POST/PUT/DELETE 請求時一併發送
// 
// 安全性警告：
// - HttpOnly = false 會增加 XSS 攻擊風險，因為 JS 可以讀取敏感 Cookie
// - SecurePolicy = None 僅適用於開發環境，生產環境必須使用 Always (HTTPS)
builder.Services.AddAntiforgery(options => 
{
    // 設置從請求頭 讀取令牌
    // 預設行為是從表單欄位 讀取
    // 設定為 HeaderName 後，前端需在 AJAX 請求中加入此 Header
    options.HeaderName = "X-CSRF-TOKEN";
    
    // 設置 Cookie 名稱
    // 預設名稱通常為 ".AspNetCore.Antiforgery.xxxxx"
    // 自訂名稱可避免與應用程式其他 Cookie 衝突
    options.Cookie.Name = ".AspNetCore.Antiforgery";
    
    // 重要：允許前端讀取 Cookie
    // 預設為 true (僅限 HTTP 存取)，這裡設為 false 是為了讓前端 JS 能讀取 Token
    // 注意：這會降低安全性，生產環境建議搭配 HttpOnly=true 與額外端點提供 Token
    options.Cookie.HttpOnly = false;
    
    // 開發環境設定
    // SameSite = Lax：允許跨站 GET 請求攜帶 Cookie
    // SecurePolicy = None：允許在 HTTP (非 HTTPS) 下傳輸 Cookie
    // 注意：生產環境應改為 SameSite.Strict 與 CookieSecurePolicy.Always
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // 開發環境用 None
    
    // 令牌有效期
    // 預設為 20 分鐘
    // 超過此時間未請求，Token 將失效，前端需重新獲取
    options.Cookie.MaxAge = TimeSpan.FromMinutes(20);

    options.FormFieldName = "__RequestVerificationToken_NotUsed";
});

// ============================================
// 應用程式建置完成，開始配置中間件管道
// ============================================

// 構建 Web 應用程序
// 這會完成依賴注入容器的配置，並創建應用程式實例
var app = builder.Build();

// ============================================
// 開發環境特定配置
// ============================================

// 檢查當前環境是否為開發環境
// 在開發環境中，我們通常需要額外的開發工具和調試功能
if (app.Environment.IsDevelopment())
{
    // 映射 OpenAPI 端點
    // 這會在應用程序中創建一個端點，提供 OpenAPI 規範文檔（通常是 JSON 格式）
    // 默認路徑為 /openapi/v1.json
    // WithMetadata(new AllowAnonymousAttribute()): 允許匿名訪問此端點，無需身份驗證
    app.MapOpenApi().WithMetadata(new AllowAnonymousAttribute());
    
    // 映射 Scalar API 參考 UI (/scalar/v1)
    // 這會創建一個可視化的 API 文檔界面，開發者可以在這裡瀏覽和測試 API
    // Scalar 提供比傳統 Swagger UI 更現代化的介面
    // WithMetadata(new AllowAnonymousAttribute()): 允許匿名訪問此文檔界面
    app.MapScalarApiReference().WithMetadata(new AllowAnonymousAttribute());
}

// ============================================
// HTTP 請求處理管道配置 (中間件順序很重要)
// ============================================

// 啟用路由功能
// 必須在其他中間件之前配置，以便後續中間件能夠使用路由資訊
app.UseRouting();          

// 啟用 CORS 中間件
// 應用名為 "web" 的 CORS 策略
// 必須在 UseAuthentication 和 UseAuthorization 之前
app.UseCors("web");        

// 啟用身份驗證中間件
// 根據配置的 Cookie 方案驗證用戶身份
// 必須在 UseAuthorization 之前
app.UseAuthentication();   

// 啟用授權中間件
// 檢查已驗證用戶是否有權限訪問請求的資源
app.UseAuthorization();    

// 啟用防偽 (Anti-Forgery) 中間件
// 
// 功能說明：
// - 防止跨站請求偽造 (CSRF) 攻擊
// - 自動生成並驗證防偽 Token
// - 保護狀態改變的操作 (POST, PUT, DELETE)
// 
// 執行順序要求：
// - 必須放在 app.UseRouting() 之後
// - 必須放在 app.UseEndpoints() (或 app.MapControllers()) 之前
// - 必須放在 app.UseAuthentication() 和 app.UseAuthorization() 之後
// 
// 注意事項：
// - 如果前端是 SPA (如 React, Vue)，需要配置相應的 Header (X-CSRF-TOKEN)
// - 如果使用 Cookie 認證，通常建議啟用
// - Minimal API 預設會對 POST/PUT/DELETE 啟用檢查
// 
// 錯誤處理：
// - 若未註冊此中介軟件但端點有 [ValidateAntiForgeryToken]，會拋出 500 錯誤
// - 若 Token 驗證失敗，會拋出 400 Bad Request
app.UseAntiforgery();

// 啟用自定義的異常處理中間件
// 統一處理應用程式中的異常，返回格式化的錯誤回應
// 應該放在管道的較後位置，以捕獲所有可能的異常
app.UseExceptionHandle();  

// ============================================
// 端點映射
// ============================================

// 註冊所有系統相關的 API 端點
app.MapSystems();

// 註冊所有產品類別相關的 API 端點
app.MapCategories();

// 註冊所有品牌相關的 API 端點
app.MapBrands();

// 註冊所有屬性相關的 API 端點
app.MapAttributes();

// 註冊所有商品相關的 API 端點
app.MapProducts();

// 註冊所有 SKU 相關的 API 端點
app.MapSkus();

// 註冊所有庫存相關的 API 端點
app.MapInventories();

// 註冊所有促銷活動相關的 API 端點
app.MapPromotions();

// 註冊所有優惠券相關的 API 端點
app.MapCoupons();

// 註冊折扣 API 端點
app.MapDiscounts();

// 註冊所有購物車相關的 API 端點
app.MapCartItems();

// 註冊所有資產 相關的 API 端點
app.MapAssets();

// 註冊所有計量單位相關的 API 端點
app.MapUnitOfMeasureEndpoints();

// 註冊所有標籤相關的 API 端點
app.MapTagEndpoints();

// 註冊所有倉庫儲位相關的 API 端點
app.MapLocations();

// 註冊所有運費規則相關的 API 端點
app.MapShippingRules();

// 註冊所有訂單相關的 API 端點
app.MapOrders();

// 註冊所有會員相關的 API 端點
app.MapMemberships();

// 註冊所有身份認證相關的 API 端點
app.MapIdentityEndpoints();

// 註冊所有點數交易相關的 API 端點
app.MapPointTransactionEndpoints();

// 註冊所有用戶管理相關的 API 端點
app.MapUserEndpoints();

// ============================================
// 應用程式啟動
// ============================================

// 運行應用程序
// 這會啟動 Web 服務器（通常是 Kestrel）並開始監聽 HTTP 請求
// 應用程式將持續運行，直到被手動停止或發生未處理的異常
app.Run();
