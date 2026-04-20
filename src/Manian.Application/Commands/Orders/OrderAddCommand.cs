using Manian.Application.Models.Products;
using Manian.Application.Queries.Products;
using Manian.Application.Queries.Warehouses;
using Manian.Application.Services;
using Manian.Domain.Entities.Carts;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Entities.Products;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Carts;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Repositories.Warehouses;
using Manian.Domain.Services;
using Manian.Domain.ValueObjects;
using Manian.Domain.ValueObjects.Order;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 新增訂單命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增訂單所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Order>，表示這是一個會回傳 Order 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 OrderAddHandler 配合使用，完成新增訂單的業務邏輯
/// 
/// 使用場景：
/// - 使用者從購物車結帳
/// - 系統自動建立訂單（如定期訂閱）
/// - API 端點接收訂單新增請求
/// 
/// 設計特點：
/// - 從使用者的購物車項目建立訂單
/// - 自動計算促銷優惠
/// - 自動建立付款記錄
/// - 支援多種付款方式
/// 
/// 注意事項：
/// - 只處理購物車項目（CartType = "shopping"）
/// - 不處理願望清單項目（CartType = "wishlist"）
/// - 會清空已結帳的購物車項目
/// </summary>
public class OrderAddCommand : IRequest<Order>
{
    /// <summary>
    /// 付款方式
    /// 
    /// 用途：
    /// - 指定訂單的付款方式
    /// - 影響訂單總金額計算
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 必須是資料庫中存在的付款方式
    /// 
    /// 預設值：
    /// - "credit_card"（信用卡）
    /// </summary>
    public string PaymentMethod { get; set; }

    /// <summary>
    /// 配送方式
    /// 
    /// 用途：
    /// - 指定訂單的配送方式
    /// - 影響訂單總金額計算
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 必須是資料庫中存在的配送方式
    /// 
    /// 預設值：
    /// - "standard"（標準配送）
    /// </summary>
    public string ShippingMethod { get; set; }

    /// <summary>
    /// 優惠券代碼（可選）
    /// 
    /// 用途：
    /// - 指定使用的優惠券
    /// - 影響訂單總金額計算
    /// 
    /// 預設值：
    /// - null（不使用優惠券）
    /// 
    /// 驗證規則：
    /// - 如果提供，必須對應資料庫中存在的優惠券
    /// - 優惠券必須在有效期內
    /// - 優惠券必須符合使用條件
    /// </summary>
    public string? CouponCode { get; set; }

    /// <summary>
    /// 收件人姓名
    /// 
    /// 用途：
    /// - 指定訂單的收件人
    /// - 用於物流配送
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-100 字元
    /// </summary>
    public string RecipientName { get; set; }

    /// <summary>
    /// 收件人電話
    /// 
    /// 用途：
    /// - 指定收件人的聯絡電話
    /// - 用於物流配送
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 必須是有效的電話號碼格式
    /// </summary>
    public string RecipientPhone { get; set; }

    /// <summary>
    /// 收件地址
    /// 
    /// 用途：
    /// - 指定訂單的配送地址
    /// - 用於物流配送
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：10-500 字元
    /// </summary>
    public string ShippingAddress { get; set; }

    /// <summary>
    /// 備註（可選）
    /// 
    /// 用途：
    /// - 提供訂單的備註資訊
    /// - 可用於特殊需求說明
    /// 
    /// 預設值：
    /// - null（可選屬性）
    /// 
    /// 驗證規則：
    /// - 建議長度限制：0-500 字元
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 新增訂單命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 OrderAddCommand 命令
/// - 查詢使用者的購物車項目
/// - 計算促銷優惠和訂單總金額
/// - 建立訂單和訂單項目
/// - 建立付款記錄
/// - 清空已結帳的購物車項目
/// - 回傳建立的訂單實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<OrderAddCommand, Order> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// </summary>
internal class OrderAddHandler : IRequestHandler<OrderAddCommand, Order>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 擴展了 AddOrderItem、GetOrderItemsAsync 等方法
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 購物車倉儲介面
    /// 
    /// 用途：
    /// - 存取購物車項目資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// </summary>
    private readonly ICartItemRepository _cartItemRepository;

    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品資料
    /// - 提供查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 等
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 唯一識別碼產生器服務
    /// 
    /// 用途：
    /// - 產生全域唯一的整數 ID
    /// - 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/Snowflake.cs
    /// - 註冊為單例模式 (Singleton)
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 促銷計算服務
    /// 
    /// 用途：
    /// - 計算促銷優惠
    /// - 計算訂單總金額
    /// 
    /// 實作方式：
    /// - 見 Domain/Services/PromotionCalculationService.cs
    /// - 註冊為 Scoped
    /// </summary>
    private readonly PromotionCalculationService _promotionCalculationService;

    /// <summary>
    /// 使用者宣告介面
    /// 
    /// 用途：
    /// - 取得當前登入使用者的 ID
    /// - 取得當前使用者的 Session ID
    /// 
    /// 實作方式：
    /// - 見 Application/Services/UserClaim.cs
    /// - 從 HTTP Context 中取得使用者資訊
    /// </summary>
    private readonly IUserClaim _userClaim;

    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 查詢 SKU 的庫存記錄
    /// - 驗證商品庫存是否足夠
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 擴展了 GetInventoriesBySkuIdAsync 方法
    /// </summary>
    private readonly ILocationRepository _locationRepository;

    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 查詢有效的促銷活動
    /// - 取得促銷規則和範圍
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// </summary>
    private readonly IPromotionRepository _promotionRepository;

    /// <summary>
    /// 優惠券倉儲介面
    /// 
    /// 用途：
    /// - 查詢使用者的優惠券
    /// - 驗證優惠券有效性
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/CouponRepository.cs）
    /// </summary>
    private readonly ICouponRepository _couponRepository;

    /// <summary>
    /// 優惠券計算服務
    /// 
    /// 用途：
    /// - 計算優惠券折扣
    /// - 驗證優惠券是否可用
    /// 
    /// 實作方式：
    /// - 見 Domain/Services/CouponCalculationService.cs
    /// </summary>
    private readonly CouponCalculationService _couponCalculationService;

    /// <summary>
    /// 訂單運費計算服務
    /// 
    /// 用途：
    /// - 計算訂單運費
    /// - 驗證運費計算規則
    /// 
    /// 實作方式：
    /// - 見 Domain/Services/ShippingFeeCalculationService.cs
    /// </summary>
    private readonly ShippingFeeCalculationService _shippingFeeCalculationService;

    /// <summary>
    /// 中介者服務 (Mediator Pattern)
    /// 
    /// 用途：
    /// - 傳遞命令和事件
    /// - 處理跨邊界邏輯
    /// - 解耦命令發送者與處理者
    /// 
    /// 實作方式：
    /// - 使用 MediatR 框架（見 Infrastructure/MediatR/MediatorExtensions.cs）
    /// - 提供 SendAsync 方法傳遞命令和事件
    /// 
    /// 在 OrderAddHandler 中的使用：
    /// - 批次查詢商品資訊（ProductsQuery）
    /// - 批次查詢 SKU 資訊（SkusQuery）
    /// - 查詢庫存資訊（InventoriesQuery）
    /// - 建立揀貨項目（PickItemAddCommand）
    /// - 建立付款記錄（PaymentAddCommand）
    /// - 建立物流記錄（ShipmentAddCommand）
    /// </summary>
    private readonly IMediator _mediator;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於新增訂單</param>
    /// <param name="cartItemRepository">購物車倉儲，用於查詢和刪除購物車項目</param>
    /// <param name="productRepository">商品倉儲，用於查詢商品資訊</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生訂單 ID</param>
    /// <param name="promotionCalculationService">促銷計算服務，用於計算促銷優惠</param>
    /// <param name="userClaim">使用者宣告，用於取得使用者 ID</param>
    /// <param name="locationRepository">儲位倉儲，用於查詢庫存</param>
    /// <param name="promotionRepository">促銷活動倉儲，用於查詢促銷活動</param>
    /// <param name="couponRepository">優惠券倉儲，用於查詢優惠券</param>
    /// <param name="couponCalculationService">優惠券計算服務，用於計算優惠券折扣</param>
    /// <param name="shippingFeeCalculationService">訂單運費計算服務，用於計算運費</param>
    public OrderAddHandler(
        IOrderRepository orderRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IUniqueIdentifier uniqueIdentifier,
        PromotionCalculationService promotionCalculationService,
        IUserClaim userClaim,
        ILocationRepository locationRepository,
        IPromotionRepository promotionRepository,
        ICouponRepository couponRepository,
        CouponCalculationService couponCalculationService,
        ShippingFeeCalculationService shippingFeeCalculationService,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _uniqueIdentifier = uniqueIdentifier;
        _promotionCalculationService = promotionCalculationService;
        _userClaim = userClaim;
        _locationRepository = locationRepository;
        _promotionRepository = promotionRepository;
        _couponRepository = couponRepository;
        _couponCalculationService = couponCalculationService;
        _shippingFeeCalculationService = shippingFeeCalculationService;
        _mediator = mediator;
    }

    /// <summary>
    /// 處理新增訂單命令的公開方法（事務管理層）
    /// 
    /// 職責：
    /// - 管理資料庫事務的生命週期
    /// - 協調訂單建立流程的執行
    /// - 確保資料一致性
    /// 
    /// 設計模式：
    /// - 模板方法模式 (Template Method Pattern)
    /// - 將事務管理與業務邏輯分離
    /// - RunAsync 方法包含實際業務邏輯
    /// 
    /// 事務特性：
    /// - 使用 using 語句確保資源釋放
    /// - 明確的 Commit 和 Rollback 控制
    /// - 統一的錯誤處理機制
    /// 
    /// 執行流程：
    /// 1. 開啟資料庫事務
    /// 2. 執行訂單建立邏輯
    /// 3. 成功則提交事務
    /// 4. 失敗則回滾事務
    /// 
    /// 與 RunAsync 的關係：
    /// - HandleAsync：負責事務管理
    /// - RunAsync：負責業務邏輯
    /// - 分離關注點，提升可測試性
    /// </summary>
    public async Task<Order> HandleAsync(OrderAddCommand request)
    {
        // ========== 第一步：開啟資料庫事務 ==========
        // 使用 using 語句確保事務資源會被正確釋放
        // Begin() 方法會建立一個新的資料庫事務
        // 所有後續的資料庫操作都會在這個事務中執行
        // 任一操作失敗，所有操作都會被回滾
        using var transaction = _orderRepository.Begin();

        try
        {
            // ========== 第二步：執行訂單建立邏輯 ==========
            // 呼叫 RunAsync 方法執行實際的訂單建立邏輯
            // RunAsync 包含所有業務邏輯：
            // - 查詢購物車項目
            // - 驗證庫存
            // - 計算金額
            // - 建立訂單和訂單項目
            // - 建立揀貨、付款、物流記錄
            // - 清空購物車
            // 注意：此時所有資料庫操作都還未實際提交
            var order = await RunAsync(request);
            
            // ========== 第三步：提交事務 ==========
            // 如果 RunAsync 成功執行完成，提交事務
            // Commit() 會將所有變更寫入資料庫
            // 包括：
            // - 訂單和訂單項目的新增
            // - 揀貨項目的新增
            // - 付款記錄的新增
            // - 物流記錄的新增
            // - 購物車項目的刪除
            // - 優惠券狀態的更新
            transaction.Commit();
            
            // ========== 第四步：回傳建立的訂單實體 ==========
            return order;
        }   
        catch (Exception)
        {
            // ========== 第五步：回滾事務 ==========
            // 如果 RunAsync 執行過程中發生任何例外
            // Rollback() 會撤銷所有未提交的變更
            // 確保資料庫狀態保持一致
            // 包括：
            // - 訂單和訂單項目的新增
            // - 揀貨項目的新增
            // - 付款記錄的新增
            // - 物流記錄的新增
            // - 購物車項目的刪除
            // - 優惠券狀態的更新
            transaction.Rollback();
            
            // ========== 第六步：拋出使用者友善的錯誤訊息 ==========
            throw;
        }
    }

    /// <summary>
    /// 處理新增訂單命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 查詢使用者的購物車項目
    /// 2. 批次查詢商品和 SKU 資訊
    /// 3. 驗證庫存是否足夠
    /// 4. 計算促銷優惠和訂單總金額
    /// 5. 建立訂單和訂單項目
    /// 6. 建立付款記錄
    /// 7. 建立物流記錄
    /// 8. 清空已結帳的購物車項目
    /// 9. 回傳建立的訂單實體
    /// 
    /// 錯誤處理：
    /// - 購物車為空：拋出 Failure.BadRequest("購物車為空，請先加入商品")
    /// - 庫存不足：拋出 Failure.BadRequest("商品名稱：{商品名稱}，庫存不足")
    /// - 贈品不存在：拋出 Failure.NotFound("贈品不存在")
    /// 
    /// 注意事項：
    /// - 所有資料庫操作在同一事務中執行
    /// - 任一失敗則全部回滾
    /// - 確保資料一致性
    /// </summary>
    /// <param name="request">新增訂單命令物件，包含訂單的所有資訊</param>
    /// <returns>建立的訂單實體</returns>
    private async Task<Order> RunAsync(OrderAddCommand request)
    {
        // =========================================================================
        // 區域變數宣告
        // =========================================================================
        
        int userId;
        IEnumerable<CartItem> cartItems;
        IEnumerable<ProductBase> products;
        IEnumerable<Sku> skus;
        Dictionary<int, IEnumerable<Inventory>> inventories;
        decimal totalAmount = 0, subtotal = 0, shippingFee = 0, discountAmount = 0, couponDiscountAmount = 0;
        IEnumerable<Promotion> promotions;
        IEnumerable<Discount> discounts;
        Coupon? coupon;
        Order order;
        List<OrderItem> orderItems = new();
        Discount? giftRule;

        // =========================================================================
        // 第一階段：資料預載（批次查詢以提升效能）
        // =========================================================================

        // ========== 第一步：查詢使用者的購物車項目 ==========
        userId = _userClaim.Id;
        
        // 查詢使用者的購物車項目（只查詢購物車，不查詢願望清單）
        cartItems = await _cartItemRepository.GetAllAsync(q => 
            q.Where(x => x.UserId == userId && x.CartType == "shopping")
        );

        // ========== 第二步：驗證購物車項目是否存在 ==========
        if (cartItems == null || !cartItems.Any())
            throw Failure.BadRequest("購物車為空，請先加入商品");

        // ========== 第三步：批次查詢購物車項目對應的商品資訊 ==========
        // 使用 ProductsQuery 批次查詢所有購物車項目對應的商品
        // 傳入 cartItems 中所有的 ProductId 集合
        // 使用中介者模式 (Mediator Pattern) 執行查詢
        // 回傳結果包裝在 Pagination<Product> 中
        var productsSet = await _mediator.SendAsync(new ProductsQuery()
        {
            Ids = cartItems.Select(x => x.ProductId).ToList()
        });

        // 取得商品實體集合
        products = productsSet.List;

        // ========== 第四步：批次查詢購物車項目對應的 SKU 資訊 ==========
        // 使用 SkusQuery 批次查詢所有購物車項目對應的 SKU
        // 傳入 cartItems 中所有的 SkuId 集合
        // 使用中介者模式 (Mediator Pattern) 執行查詢
        // 回傳結果包裝在 Pagination<Sku> 中
        var skusSet = await _mediator.SendAsync(new SkusQuery()
        {
            Ids = cartItems.Select(x => x.SkuId).ToList()
        });

        // 取得 SKU 實體集合
        skus = skusSet.List;

        // =========================================================================
        // 第二階段：庫存驗證（逐一檢查每個 SKU 的庫存）
        // =========================================================================

        // 建立庫存快取字典，Key 為 SkuId，Value 為該 SKU 的所有庫存記錄
        // 用於後續揀貨邏輯，避免重複查詢
        inventories = new Dictionary<int, IEnumerable<Inventory>>();

        // 遍歷所有 SKU，逐一驗證庫存
        foreach(var sku in skusSet.List)
        {
            // ========== 第五步：找到對應的購物車項目 ==========
            // 根據 SkuId 找到購物車中對應的項目
            // FirstOrDefault 返回第一個匹配項目，若無則返回 null
            var cartItem = cartItems.FirstOrDefault(x => x.SkuId == sku.Id);

            // ========== 第六步：查詢該 SKU 的庫存記錄 ==========
            // 使用 LocationRepository 批次查詢該 SKU 的所有庫存記錄
            // 傳入 SkuId 集合
            var inventoriesCache = await _locationRepository.GetInventoriesBySkuIdsync(sku.Id);

            // ========== 第七步：驗證庫存記錄是否存在 ==========
            // 如果庫存記錄為 null 或空集合，表示該 SKU 沒有庫存
            if(inventoriesCache == null || !inventoriesCache.Any())
            {
                // 從購物車中刪除該項目
                _cartItemRepository.Delete(cartItem!);
                
                // 儲存變更到資料庫
                await _cartItemRepository.SaveChangeAsync();
                
                // 取得該 SKU 對應的商品資訊
                var product = products.FirstOrDefault(x => x.Id == sku.ProductId);

                // 拋出例外，告知使用者該商品沒有庫存記錄
                throw Failure.BadRequest($"商品沒有庫存記錄，商品名稱：{product!.Name}");   
            }

            // ========== 第八步：計算總可銷售庫存 ==========
            // 計算該 SKU 在所有儲位的可銷售庫存總和
            // QuantityAvailable = QuantityOnHand - QuantityReserved
            var totalQuantityAvailable = inventoriesCache.Sum(i => i.QuantityAvailable);

            // ========== 第九步：驗證庫存是否足夠 ==========
            // 比較總可銷售庫存與購物車項目數量
            // 如果庫存不足，拋出例外
            if (totalQuantityAvailable < cartItem!.Quantity)
                throw Failure.BadRequest($"商品名稱：{cartItem.ProductName}，庫存不足");

            // ========== 第十步：將庫存資訊加入快取 ==========
            // 將驗證通過的庫存記錄加入快取字典
            // Key 為 SkuId，Value 為該 SKU 的所有庫存記錄
            // 後續揀貨邏輯會使用此快取
            inventories.Add(sku.Id, inventoriesCache);
        }

        // =========================================================================
        // 第三階段：訂單金額計算（促銷、優惠券、運費、總金額）
        // =========================================================================

        // =========================================================================
        // 促銷活動計算
        // =========================================================================

        // ========== 第十一步：查詢所有有效的促銷活動 ==========
        // 過濾條件：
        // 1. Status == "active"：只查詢啟用的促銷活動
        // 2. StartDate <= DateTimeOffset.UtcNow：促銷活動已開始
        // 3. EndDate >= DateTimeOffset.UtcNow：促銷活動未結束
        promotions = await _promotionRepository.GetAllAsync(q => 
            q.Where(p => 
                p.Status == "active" &&  // 只查詢啟用的促銷活動
                p.StartDate <= DateTimeOffset.UtcNow &&  // 已開始
                p.EndDate >= DateTimeOffset.UtcNow   // 未結束
            )
        );

        // ========== 第十二步：使用促銷計算服務計算優惠 ==========
        // 呼叫 PromotionCalculationService.CalculateDiscountsAsync 方法
        // 傳入參數：
        // 1. cartItems：購物車項目集合
        // 2. promotions：有效的促銷活動集合
        // 回傳值：所有適用的促銷折扣集合（IEnumerable<Discount>）
        discounts = await _promotionCalculationService.CalculateDiscountsAsync(
            cartItems, // 購物車項目集合
            promotions // 有效的促銷活動集合
        );

        // ========== 第十三步：選擇折扣力度最大的促銷活動 ==========
        // 使用 OrderByDescending 按 DiscountAmount 降序排序
        // 使用 FirstOrDefault 取得折扣金額最大的促銷活動
        // 如果沒有適用的促銷活動，bestDiscount 為 null
        var bestDiscount = discounts.OrderByDescending(d => d.DiscountAmount).FirstOrDefault();

        // ========== 第十四步：設定促銷活動折扣金額 ==========
        // 如果存在有效的促銷活動，設定折扣金額
        if(bestDiscount != null)
        {
            discountAmount = bestDiscount.DiscountAmount;
        }

        // =========================================================================
        // 優惠券計算
        // =========================================================================

        // ========== 第十五步：查詢使用者輸入的優惠券 ==========
        // 查詢條件：
        // 1. CouponCode == request.CouponCode：優惠券代碼匹配
        // 2. UserId == userId：優惠券屬於當前使用者
        // 注意：優惠券的有效性驗證（是否已使用、是否在有效期內）由 CouponCalculationService 處理
        coupon = await _couponRepository.GetAsync(
            q => q.Where(x => x.CouponCode == request.CouponCode && x.UserId == userId)
        );

        // ========== 第十六步：計算優惠券折扣金額 ==========
        // 如果優惠券存在，呼叫 CouponCalculationService.CalculateDiscountAsync 方法
        // 傳入參數：
        // 1. coupon：優惠券實體
        // 2. cartItems：購物車項目集合
        // 回傳值：優惠券折扣金額（decimal）
        if(coupon != null)
        {
            couponDiscountAmount = await _couponCalculationService.CalculateDiscountAsync(
                coupon,
                cartItems
            );

            // ========== 第十七步：標記優惠券為已使用 ==========
            // 如果優惠券折扣金額大於 0，表示優惠券已成功使用
            // 設定 IsUsed = true 和 UsedAt = DateTimeOffset.UtcNow
            // 注意：這些變更會在 SaveChangeAsync 時寫入資料庫
            if(couponDiscountAmount > 0)
            {
                coupon.IsUsed = true;
                coupon.UsedAt = DateTimeOffset.UtcNow;
            }
        }

        // =========================================================================
        // 運費計算
        // =========================================================================

        // ========== 第十八步：計算運費 ==========
        // 呼叫 ShippingFeeCalculationService.CalculateShippingFeeAsync 方法
        // 傳入參數：
        // 1. cartItems：購物車項目集合
        // 回傳值：運費金額（decimal）
        // 注意：運費計算會考慮運費規則、免運條件等
        shippingFee = await _shippingFeeCalculationService.CalculateShippingFeeAsync(
            cartItems
        );

        // =========================================================================
        // 商品小計計算
        // =========================================================================

        // ========== 第十九步：計算商品小計金額 ==========
        // 計算方式：所有購物車項目的單價乘以數量後相加
        // 公式：Σ (UnitPrice × Quantity)
        subtotal = cartItems.Sum(x => x.UnitPrice * x.Quantity);

        // =========================================================================
        // 訂單總金額計算
        // =========================================================================

        // ========== 第二十步：計算訂單總金額 ==========
        // 計算公式：總金額 = 商品小計 - 促銷折扣 - 優惠券折扣 - 運費
        // 注意：運費是減去，因為 shippingFee 可能包含負值（如免運優惠）
        totalAmount = subtotal - discountAmount - couponDiscountAmount - shippingFee;

        // =========================================================================
        // 第四階段：訂單實體與項目建立（訂單、訂單項目、贈品）
        // =========================================================================

        // =========================================================================
        // 訂單實體建立
        // =========================================================================

        // ========== 第二十一步：建立訂單實體 ==========
        // 使用雪花演算法產生唯一訂單 ID
        // 設定訂單編號格式：ORD-YYYYMMDD-XXXXXX
        // 計算訂單總金額（包含促銷折扣、優惠券折扣、運費）
        order = new Order
        {
            // 產生全域唯一的訂單 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定使用者 ID
            UserId = userId,
            
            // 設定訂單編號（格式：ORD-YYYYMMDD-XXXXXX）
            // 使用當前 UTC 時間的日期部分和隨機 6 位數字
            OrderNumber = $"ORD_{DateTimeOffset.UtcNow:yyyyMMdd}_{_uniqueIdentifier.NextInt():D6}",
            
            // 設定折扣金額（促銷折扣 + 優惠券折扣）
            DiscountAmount = discountAmount + couponDiscountAmount,
            
            // 設定訂單總金額（商品小計 - 折扣 - 運費）
            TotalAmount = totalAmount,
            
            // 設定運費金額
            ShippingAmount = shippingFee,
            
            // 設定訂單狀態為已建立
            Status = "created",
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 將訂單加入倉儲
        _orderRepository.Add(order);

        // 將訂單加入集合，供後續使用（如建立訂單項目）
        await _orderRepository.SaveChangeAsync();

        // =========================================================================
        // 訂單項目建立
        // =========================================================================

        // ========== 第二十二步：遍歷購物車項目，建立訂單項目 ==========
        // 將購物車中的每個項目轉換為訂單項目
        // 保留商品名稱、單價、數量等資訊作為快照
        foreach (var cartItem in cartItems)
        {
            // ========== 第二十三步：建立訂單項目實體 ==========
            var orderItem = new OrderItem
            {
                Id = _uniqueIdentifier.NextInt(),        // 產生全域唯一的訂單項目 ID
                OrderId = order.Id,                      // 關聯到訂單
                ProductId = cartItem.ProductId,          // 保留商品 ID（用於關聯）
                SkuId = cartItem.SkuId,                  // 保留 SKU ID（用於關聯）
                ProductName = cartItem.ProductName,      // 保留商品名稱（快照
                ProductImageUrl = cartItem.ProductImage, // 保留商品圖片（快照）
                UnitPrice = cartItem.UnitPrice,          // 保留單價（快照）
                Quantity = cartItem.Quantity,            // 保留購買數量
                ReturnedQuantity = 0,                    // 初始化退貨數量為 0
                Status = "pending",                      // 設定訂單項目狀態為待處理
                CreatedAt = DateTimeOffset.UtcNow        // 設定建立時間為目前 UTC 時間
            };

            // ========== 第二十四步：將訂單項目加入倉儲和集合 ==========
            // 將訂單項目加入 EF Core 變更追蹤
            // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync 才會實際執行 INSERT
            _orderRepository.AddOrderItem(orderItem);
            
            // 將訂單項目加入集合，供後續使用（如建立揀貨項目）
            orderItems.Add(orderItem);

            // 將訂單項目加入訂單的訂單項目集合
            await _orderRepository.SaveChangeAsync();
        }

        // =========================================================================
        // 贈品項目處理
        // =========================================================================

        // ========== 第二十五步：檢查是否有贈品促銷規則 ==========
        // 從折扣集合中查找規則類型為 "gift" 的促銷規則
        // 贈品規則通常包含 GiftItemId 屬性，指定贈品的 SKU ID
        giftRule = discounts.FirstOrDefault(x => x.RuleType == "gift");

        // ========== 第二十六步：處理贈品項目 ==========
        if(giftRule != null && giftRule.GiftItemId != null)
        {
            // ========== 第二十七步：查詢贈品 SKU 資訊 ==========
            // 根據 GiftItemId 查詢贈品的 SKU 實體
            var giftSku = await _productRepository.GetSkuAsync(giftRule.GiftItemId.Value);

            // 驗證贈品 SKU 是否存在
            if(giftSku == null)
                throw Failure.NotFound("贈品不存在");

            // ========== 第二十八步：查詢贈品商品資訊 ==========
            // 根據 SKU 的 ProductId 查詢贈品的商品實體
            var giftProduct = await _productRepository.GetByIdAsync(giftSku.ProductId);

            // 驗證贈品商品是否存在
            if(giftProduct == null)
                throw Failure.NotFound("贈品不存在");

            // ========== 第二十九步：建立贈品訂單項目 ==========
            // 贈品作為訂單項目加入訂單，價格為 0
            if(giftSku != null)
            {
                var giftItem = new OrderItem
                {
                    // 產生全域唯一的訂單項目 ID
                    Id = _uniqueIdentifier.NextInt(),
                    
                    // 關聯到訂單
                    OrderId = order.Id,
                    
                    // 設定贈品商品 ID
                    ProductId = giftSku.ProductId,
                    
                    // 設定贈品 SKU ID
                    SkuId = giftSku.Id,
                    
                    // 設定贈品商品名稱
                    ProductName = giftProduct.Name,
                    
                    // 贈品價格為 0
                    UnitPrice = 0,
                    
                    // 贈品數量固定為 1
                    Quantity = 1,
                    
                    // 初始化退貨數量為 0
                    ReturnedQuantity = 0,
                    
                    // 設定訂單項目狀態為待處理
                    Status = "pending",
                    
                    // 設定建立時間為目前 UTC 時間
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // ========== 第三十步：將贈品項目加入倉儲和集合 ==========
                // 將贈品項目加入 EF Core 變更追蹤
                _orderRepository.AddOrderItem(giftItem);
                
                // 將贈品項目加入集合，供後續使用（如建立揀貨項目）
                orderItems.Add(giftItem);

                // 贈品數量加 1
                await _orderRepository.SaveChangeAsync();
            }
        }

        // =========================================================================
        // 第五階段：揀貨、付款、物流、快照處理
        // =========================================================================

        // ========== 第三十一步：建立揀貨項目 ==========
        addPickItems(orderItems,inventories);

        // ========== 第三十二步：建立 OrderSnapshot ==========
        var orderSnapshot = new OrderSnapshot
        {
            // 商品價格快照
            ItemPrices = orderItems.ToDictionary(
                oi => oi.Id,
                oi => new ItemPriceSnapshot
                {
                    ProductName = oi.ProductName,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity
                }
            ),
            
            // 促銷規則快照
            PromotionRules = bestDiscount != null ? new List<PromotionRuleSnapshot>
            {
                new PromotionRuleSnapshot
                {
                    RuleType = bestDiscount.RuleType,
                    TabName = bestDiscount.TabName,
                    DiscountAmount = bestDiscount.DiscountAmount,
                }
            } : new List<PromotionRuleSnapshot>(),
            
            // 優惠券快照
            Coupon = coupon != null && couponDiscountAmount > 0 ? new CouponSnapshot
            {
                Name = coupon.Name,
                DiscountAmount = couponDiscountAmount
            } : null,
            
            // 運費快照
            ShippingFee = shippingFee,
            
            // 稅金總額（目前為 0，可根據需求擴充）
            TotalTaxAmount = 0
        };

        // 將快照設定到訂單實體
        order.Snapshot = orderSnapshot;
        

        // ========== 第三十三步：建立物流記錄 ==========
        AddShipment(
            order, 
            request.ShippingAddress, 
            request.RecipientName, 
            request.RecipientPhone,
            request.ShippingMethod);


        // =========================================================================
        // 第六階段：清空購物車與回傳訂單
        // =========================================================================

        // ========== 第三十四步：清空已結帳的購物車項目 ==========
        foreach (var cartItem in cartItems)
        {
            _cartItemRepository.Delete(cartItem);
        }
        
        // 將訂單加入集合
        await _orderRepository.SaveChangeAsync();

        // ========== 第三十五步：回傳建立的訂單實體 ==========
        return order;        
    }

    /// <summary>
    /// 建立揀貨項目
    /// 
    /// 職責：
    /// - 為訂單項目分配揀貨儲位
    /// - 從多個儲位分配揀貨數量
    /// - 建立揀貨項目記錄
    /// 
    /// 設計考量：
    /// - 支援從多個儲位揀貨
    /// - 自動分配揀貨數量
    /// - 優先使用庫存充足的儲位
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// 
    /// 揀貨邏輯：
    /// - 遍歷所有訂單項目
    /// - 為每個項目查詢可用庫存
    /// - 從各儲位分配揀貨數量
    /// - 為每個分配建立揀貨項目
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - OrderItem 與 PickItem 是一對多關係
    /// - 一個訂單項目可以對應多個揀貨項目
    /// 
    /// 使用場景：
    /// - 訂單建立時分配揀貨任務
    /// - 產生揀貨清單
    /// </summary>
    /// <param name="orderItems">訂單項目集合</param>
    /// <param name="inventoriesDictionary">庫存字典，Key 為 SKU ID，Value 為該 SKU 的所有庫存記錄</param>
    private void addPickItems(IEnumerable<OrderItem> orderItems, Dictionary<int, IEnumerable<Inventory>> inventoriesDictionary)
    {
        // ========== 第一步：遍歷所有訂單項目 ==========
        // 為每個訂單項目建立揀貨項目
        foreach (var orderItem in orderItems)
        {
            // ========== 第二步：取得該 SKU 的所有可用庫存 ==========
            // 從字典中根據 SKU ID 查詢庫存集合
            var inventories = inventoriesDictionary[orderItem.SkuId];
            
            // 初始化需要揀貨的數量（等於訂單項目數量）
            var quantity = orderItem.Quantity;

            // ========== 第三步：遍歷所有庫存儲位 ==========
            // 從各儲位分配揀貨數量
            foreach (var inventory in inventories)
            {
                // ========== 第四步：計算該儲位應揀貨數量 ==========
                // 取訂單項目剩餘數量和該儲位可用數量的較小值
                // 這確保不會超過該儲位的實際可用庫存
                var quantityToPick = Math.Min(orderItem.Quantity, inventory.QuantityAvailable);
                
                // ========== 第五步：如果該儲位有可揀貨數量，則建立揀貨項目 ==========
                if (quantityToPick > 0)
                {
                    inventory.QuantityAvailable -= quantityToPick;
                    inventory.QuantityReserved += quantityToPick;

                    // 建立揀貨項目實體
                    var pickItem = new PickItem
                    {
                        Id = _uniqueIdentifier.NextInt(),            // 產生全域唯一的揀貨項目 ID
                        OrderId = orderItem.OrderId,                 // 關聯到訂單
                        OrderItemId = orderItem.Id,                  // 關聯到訂單項目
                        LocationId = inventory.LocationId,           // 關聯到儲位
                        ProductImageUrl = orderItem.ProductImageUrl, // 設定商品圖片
                        QuantityToPick = quantityToPick,             // 設定應揀貨數量
                        QuantityPicked = 0,                          // 初始化已揀貨數量為 0
                        Status = "allocated",                        // 設定揀貨狀態為已分配
                        CreatedAt = DateTimeOffset.UtcNow            // 設定建立時間為目前 UTC 時間
                    };

                    // ========== 第六步：將揀貨項目加入倉儲 ==========
                    // 將揀貨項目加入 EF Core 變更追蹤
                    // 不立即寫入資料庫，等待統一提交
                    // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
                    _orderRepository.AddPickItem(pickItem);
                    
                    // ========== 第七步：減少訂單項目需要揀貨的數量 ==========
                    // 更新剩餘需要揀貨的數量
                    quantity -= quantityToPick;
                    
                    // ========== 第八步：如果訂單項目已經全部揀貨，則跳出迴圈 ==========
                    // 如果數量已經分配完畢，不需要繼續檢查其他儲位
                    if (quantity <= 0)
                        break;
                }                
            }
        }        
    }

    /// <summary>
    /// 建立物流記錄
    /// 
    /// 職責：
    /// - 為訂單建立物流記錄
    /// - 設定統一的物流資訊（地址、收件人等）
    /// 
    /// 設計考量：
    /// - 一個訂單對應一筆物流記錄
    /// - 統一設定物流資訊，確保一致性
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 與 Shipment 是一對一關係
    /// - 一個訂單只有一筆物流記錄
    /// 
    /// 使用場景：
    /// - 訂單建立時初始化物流記錄
    /// - 設定配送資訊
    /// </summary>
    /// <param name="order">訂單實體</param>
    /// <param name="shippingAddress">寄送地址</param>
    /// <param name="recipientName">收件人姓名</param>
    /// <param name="recipientPhone">收件人電話</param>
    /// <param name="shipmentMethod">物流方式</param>
    private void AddShipment(
        Order order,
        string shippingAddress, 
        string recipientName, 
        string recipientPhone,
        string shipmentMethod)
    {
        // ========== 第一步：取得目前的 UTC 時間 ==========
        // 用於設定物流記錄的建立時間
        // 確保時間一致性，避免因迴圈執行時間差異導致時間不同
        var createdAt = DateTimeOffset.UtcNow;

        // ========== 第二步：建立物流記錄實體 ==========
        // 為訂單建立一筆物流記錄
        // 設定統一的物流資訊，確保一致性
        var shipment = new Shipment()
        {
            // 產生全域唯一的物流記錄 ID
            // 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
            Id = _uniqueIdentifier.NextInt(),
            
            // 關聯到訂單
            // OrderId 是外鍵，指向 Order 實體
            // 根據 OrderConfiguration.cs 的配置：
            // - Order 與 Shipment 是一對一關係
            // - 刪除 Order 時會級聯刪除 Shipment
            OrderId = order.Id,
            
            // 設定物流方式
            // 可選值：post中華郵政/seven-11/family全家/hilife萊爾富/ok Ok Mart/tcat黑貓/ecam宅配通
            // 根據 Shipment 實體的 Method 屬性驗證規則：
            // - 只能接受這七個值之一
            // - 可以為 null
            Method = shipmentMethod,
            
            // 設定寄送地址
            // 用於物流配送
            // 記錄包裹的寄送地址
            ShippingAddress = shippingAddress,
            
            // 設定收件人姓名
            // 用於物流配送
            // 記錄包裹的收件人姓名
            RecipientName = recipientName,
            
            // 設定收件人電話
            // 用於物流配送
            // 記錄包裹的收件人電話
            RecipientPhone = recipientPhone,
            
            // 設定建立時間為目前 UTC 時間
            // 使用協調世界時 (UTC) 記錄建立時間
            // 避免時區問題，便於跨時區系統使用
            CreatedAt = createdAt
        };

        // ========== 第三步：將物流記錄加入倉儲 ==========
        // 將物流記錄加入 EF Core 變更追蹤
        // 不立即寫入資料庫，等待統一提交
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        // 
        // 根據 OrderRepository.cs 的 AddShipment 方法實作：
        // - 使用 context.Set<Shipment>().Add(shipment) 將實體加入追蹤
        // - 實體狀態會被標記為 Added
        // - 需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        _orderRepository.AddShipment(shipment);    
    }

}
