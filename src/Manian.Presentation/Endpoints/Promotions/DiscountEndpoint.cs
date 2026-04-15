using Manian.Application.Queries.Promotions;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Shared.Mediator.Interface;

namespace Manian.Presentation.Endpoints.Promotions;

/// <summary>
/// 折扣 API 端點
/// 
/// 職責：
/// - 定義折扣相關的 API 端點
/// - 處理折扣的查詢請求
/// - 處理可用優惠券的查詢請求
/// 
/// 設計模式：
/// - Minimal API：使用 ASP.NET Core 的 Minimal API 風格
/// - CQRS：透過 Mediator 分發查詢請求
/// - 依賴注入：注入所需的服務
/// 
/// 架構位置：
/// - 位於 Presentation 層（展示層）
/// - 負責處理 HTTP 請求和回應
/// - 不包含業務邏輯，只負責路由和參數處理
/// 
/// 路由設計：
/// - 基礎路徑：/api/discounts
/// - 支援的動作：POST（查詢可用折扣）、GET（查詢可用優惠券）
/// 
/// 使用場景：
/// - 購物車頁面顯示可用折扣
/// - 結帳頁面計算最終金額
/// - 促銷活動效果預覽
/// - 查詢用戶可用的優惠券
/// </summary>
public static class DiscountEndpoint
{
    /// <summary>
    /// 註冊折扣相關的 API 端點
    /// 
    /// 職責：
    /// - 定義 API 路由
    /// - 處理 HTTP 請求
    /// - 呼叫 Mediator 分發查詢
    /// 
    /// 設計考量：
    /// - 使用擴充方法模式，方便在 Program.cs 中呼叫
    /// - 所有端點都使用 Mediator 分發請求
    /// - 遵循 RESTful API 設計原則
    /// 
    /// 註冊方式：
    /// 在 Program.cs 中呼叫：
    /// <code>
    /// app.MapDiscountEndpoints();
    /// </code>
    /// </summary>
    /// <param name="app">
    /// WebApplication 實例
    /// - ASP.NET Core 應用程式的入口點
    /// - 用於註冊路由和中介軟體
    /// </param>
    public static void MapDiscounts(this WebApplication app)
    {
        // =========================================================================
        // POST /api/discounts - 查詢可用折扣
        // =========================================================================
        
        // 定義 POST 端點，路由為 /api/discounts/available
        app.MapGet("/api/available-discounts", HandleGetAvailableDiscountsAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("查詢可用折扣")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            查詢目前可用的促銷折扣
            
            請求內容：
            - cartItems：購物車項目集合（必填）
            
            回傳格式：
            - 200 OK：可用折扣集合
            
            使用範例：
            - POST /api/discounts/available
            {
                ""cartItems"": [
                    {
                        ""productId"": 1001,
                        ""quantity"": 2,
                        ""unitPrice"": 100
                    },
                    {
                        ""productId"": 1002,
                        ""quantity"": 1,
                        ""unitPrice"": 200
                    }
                ]
            }
            
            說明：
            - 只回傳當前時間有效的促銷活動
            - 只回傳啟用狀態的促銷活動
            - 只回傳未超過總使用次數限制的促銷活動
            - 使用 PromotionCalculationService 計算折扣
            - 不使用分頁，直接回傳所有可用折扣
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("促銷管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<IEnumerable<Discount>>(StatusCodes.Status200OK);

        // =========================================================================
        // GET /api/available-coupons - 查詢可用優惠券
        // =========================================================================
        
        // 定義 GET 端點，路由為 /api/available-coupons
        app.MapGet("/api/available-coupons", HandleGetAvailableCouponsAsync)
        
        // 設定端點摘要，顯示在 Swagger UI 中
        .WithSummary("查詢可用優惠券")
        
        // 設定端點描述，提供詳細的使用說明
        .WithDescription(@"
            查詢當前用戶購物車中所有商品可用的優惠券
            
            查詢參數：
            - cartType：購物車類型（可選），預設為 'shopping'
              - 'shopping'：購物車
              - 'wishlist'：願望清單
            
            回傳格式：
            - 200 OK：可用優惠券集合
            
            使用範例：
            - GET /api/available-coupons
            - GET /api/available-coupons?cartType=shopping
            - GET /api/available-coupons?cartType=wishlist
            
            說明：
            - 只回傳當前用戶的優惠券（包括全局優惠券和用戶專屬優惠券）
            - 只回傳未使用的優惠券
            - 只回傳在有效期內的優惠券
            - 只回傳適用於購物車中至少一個商品的優惠券
            - 不使用分頁，直接回傳所有可用優惠券
            - 優惠券適用範圍包括：
              - 'all'：全部商品可用
              - 'product'：特定商品可用
              - 'category'：特定類別可用
              - 'brand'：特定品牌可用
        ")
        
        // 設定端點標籤，用於 Swagger UI 分組
        .WithTags("促銷管理")
        
        // 產生 OpenAPI 回應定義
        .Produces<IEnumerable<Coupon>>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// 處理查詢可用折扣請求的私有方法
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
    /// <param name="query">可用折扣查詢請求物件，包含 CartItems</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：可用折扣集合
    /// </returns>
    private static async Task<IResult> HandleGetAvailableDiscountsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AvailableDiscountsQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（AvailableDiscountsQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和可用折扣集合
        return Results.Ok(result);
    }

    /// <summary>
    /// 處理查詢可用優惠券請求的私有方法
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
    /// <param name="query">可用優惠券查詢請求物件，包含 CartType</param>
    /// <returns>
    /// IResult：ASP.NET Core 的結果物件
    /// - 200 OK：可用優惠券集合
    /// </returns>
    private static async Task<IResult> HandleGetAvailableCouponsAsync(
        [FromServices] IMediator mediator,
        [AsParameters] AvailableCouponsQuery query)
    {
        // ========== 第一步：透過 Mediator 分發查詢 ==========
        // Mediator 會找到對應的 Handler（AvailableCouponsQueryHandler）
        // Handler 會執行查詢並回傳結果
        var result = await mediator.SendAsync(query);
        
        // ========== 第二步：回傳查詢結果 ==========
        // 回傳 200 OK 狀態碼和可用優惠券集合
        return Results.Ok(result);
    }
}
