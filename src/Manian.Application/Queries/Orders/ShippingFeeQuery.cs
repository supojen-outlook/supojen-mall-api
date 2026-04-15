using Manian.Application.Services;
using Manian.Domain.Repositories.Carts;
using Manian.Domain.Services;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢訂單運費請求物件
/// 
/// 用途：
/// - 根據訂單項目計算運費
/// - 支援購物車和訂單頁面的運費計算
/// - 應用運費規則和免運條件
/// 
/// 設計模式：
/// - 實作 IRequest<decimal>，表示這是一個查詢請求
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ShippingFeeQueryHandler 配合使用
/// 
/// 使用場景：
/// - 購物車頁面顯示運費
/// - 結帳頁面計算最終金額
/// - 訂單建立時計算運費
/// </summary>
public class ShippingFeeQuery : IRequest<decimal>;

/// <summary>
/// 運費查詢處理器
/// 
/// 職責：
/// - 接收 ShippingFeeQuery 請求
/// - 呼叫 ShippingFeeCalculationService 計算運費
/// - 回傳計算結果
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ShippingFeeQuery, decimal> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ShippingFeeCalculationService
/// - 邏輯清晰，方便單元測試
/// </summary>
public class ShippingFeeQueryHandler : IRequestHandler<ShippingFeeQuery, decimal>
{
    /// <summary>
    /// 運費計算服務
    /// 
    /// 用途：
    /// - 根據訂單項目計算運費
    /// - 應用運費規則和免運條件
    /// 
    /// 實作方式：
    /// - 見 Domain/Services/ShippingFeeCalculationService.cs
    /// - 已在 Domain/DI.cs 中註冊為 Scoped 服務
    /// </summary>
    private readonly ShippingFeeCalculationService _shippingFeeCalculationService;

    /// <summary>
    /// 用戶權限
    /// 
    /// 用途：
    /// - 取得當前用戶 ID
    /// - 用於查詢購物車資料
    /// 
    /// 實作方式：
    /// - 見 Shared/Security/UserClaim.cs
    /// - 已在 Shared/Security/SecurityService.cs 中註冊為 Singleton 服務
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 購物車項目資料庫存取
    /// 
    /// 用途：
    /// - 查詢用戶購物車資料
    /// - 取得訂單項目
    ///     
    /// 實作方式：
    /// - 見 Domain/Repositories/Carts/ICartItemRepository.cs
    /// - 已在 Domain/DI.cs 中註冊為 Scoped 服務
    /// </summary>
    private readonly ICartItemRepository _cartItemRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="shippingFeeCalculationService">運費計算服務</param>
    /// <param name="userClaim">用戶權限</param>
    /// <param name="cartItemRepository">購物車項目資料庫存取</param>
    public ShippingFeeQueryHandler(
        ShippingFeeCalculationService shippingFeeCalculationService,
        IUserClaim userClaim,
        ICartItemRepository cartItemRepository)
    {
        _shippingFeeCalculationService = shippingFeeCalculationService;
        _userClaim = userClaim;
        _cartItemRepository = cartItemRepository;
    }

    /// <summary>
    /// 處理運費查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證訂單項目不為空
    /// 2. 呼叫 ShippingFeeCalculationService 計算運費
    /// 3. 回傳計算結果
    /// 
    /// 錯誤處理：
    /// - 如果訂單項目為空，返回 0
    /// - 如果沒有符合條件的運費規則，返回預設運費（0）
    /// 
    /// 使用範例：
    /// <code>
    /// var query = new ShippingFeeQuery 
    /// { 
    ///     OrderItems = new List<OrderItem> 
    ///     { 
    ///         new OrderItem { SkuId = 1001, Quantity = 2, UnitPrice = 100 }
    ///     } 
    /// };
    /// var shippingFee = await _mediator.SendAsync(query);
    /// </code>
    /// </summary>
    /// <param name="request">運費查詢請求物件，包含 OrderItems</param>
    /// <returns>計算後的運費金額</returns>
    public async Task<decimal> HandleAsync(ShippingFeeQuery request)
    {   
        // 查詢用戶購物車資料
        var cartItems = await _cartItemRepository.GetAllAsync(
            q => q.Where(x => x.UserId == _userClaim.Id)
        );

        // 驗證訂單項目不為空
        if(cartItems == null || !cartItems.Any())
            return 0;

        // 呼叫運費計算服務計算運費
        return await _shippingFeeCalculationService.CalculateShippingFeeAsync(cartItems);
    }
}
