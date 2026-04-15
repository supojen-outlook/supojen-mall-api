using System.Text.Json;
using Manian.Application.Services;
using Manian.Domain.Entities.Carts;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Carts;
using Manian.Domain.Repositories.Products;
using Manian.Domain.Services;
using Manian.Domain.ValueObjects;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Carts;

/// <summary>
/// 新增購物車項目命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增購物車項目所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<CartItem>，表示這是一個會回傳 CartItem 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CartItemAddHandler 配合使用，完成新增購物車項目的業務邏輯
/// 
/// 使用場景：
/// - 使用者將商品加入購物車
/// - 使用者將商品加入願望清單
/// - API 端點接收購物車項目新增請求
/// 
/// 設計特點：
/// - 支援購物車(shopping)和願望清單(wishlist)兩種類型
/// - 自動從 SKU 取得商品快照資訊
/// - 支援數量累加（如果項目已存在）
/// </summary>
public class CartItemAddCommand : IRequest<CartItem>
{
    /// <summary>
    /// 購物車類型
    /// 
    /// 用途：
    /// - 識別購物車項目的類型
    /// - 支援購物車和願望清單兩種類型
    /// 
    /// 可選值：
    /// - "shopping"：購物車
    /// - "wishlist"：願望清單
    /// 
    /// 驗證規則：
    /// - 必須是 "shopping" 或 "wishlist"
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// SKU ID
    /// 
    /// 用途：
    /// - 識別要加入購物車的商品規格
    /// - 必須是資料庫中已存在的 SKU ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的 SKU
    /// 
    /// 錯誤處理：
    /// - 如果 SKU 不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int SkuId { get; set; }

    /// <summary>
    /// 數量
    /// 
    /// 用途：
    /// - 指定要加入購物車的商品數量
    /// - 如果項目已存在，則累加數量
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// 
    /// 錯誤處理：
    /// - 如果數量小於等於 0，會拋出 ArgumentException
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// 新增購物車項目命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CartItemAddCommand 命令
/// - 驗證 SKU 是否存在
/// - 建立或更新購物車項目
/// - 從 SKU 取得商品快照資訊
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 CartItem 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CartItemAddCommand, CartItem> 介面
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
/// 
/// 業務邏輯：
/// - 如果購物車項目已存在，則累加數量
/// - 如果購物車項目不存在，則建立新項目
/// - 從 SKU 取得商品快照資訊（名稱、價格、圖片等）
/// </summary>
internal class CartItemAddHandler : IRequestHandler<CartItemAddCommand, CartItem>
{
    /// <summary>
    /// 購物車倉儲介面
    /// 
    /// 用途：
    /// - 存取購物車項目資料
    /// - 提供新增、查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<CartItem>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Carts/ICartItemRepository.cs
    /// </summary>
    private readonly ICartItemRepository _repository;

    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品和 SKU 資料
    /// - 提供查詢 SKU 的操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、GetSkuAsync 等
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Products/IProductRepository.cs
    /// </summary>
    private readonly IProductRepository _productRepository;

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
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">購物車倉儲，用於新增購物車項目</param>
    /// <param name="productRepository">商品倉儲，用於查詢 SKU</param>
    /// <param name="userClaim">使用者宣告，用於取得使用者 ID</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生購物車項目 ID</param>
    public CartItemAddHandler(
        ICartItemRepository repository,
        IProductRepository productRepository,
        IUserClaim userClaim,
        IUniqueIdentifier uniqueIdentifier)
    {
        _repository = repository;
        _productRepository = productRepository;
        _userClaim = userClaim;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增購物車項目命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證購物車類型
    /// 2. 查詢 SKU 是否存在
    /// 3. 查詢購物車項目是否已存在
    /// 4. 如果項目已存在，累加數量
    /// 5. 如果項目不存在，建立新項目
    /// 6. 從 SKU 取得商品快照資訊
    /// 7. 將實體加入倉儲
    /// 8. 儲存變更到資料庫
    /// 9. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 購物車類型無效：拋出 ArgumentException
    /// - SKU 不存在：拋出 Failure.NotFound()
    /// - 數量小於等於 0：拋出 ArgumentException
    /// 
    /// 注意事項：
    /// - 商品快照資訊從 SKU 取得，確保歷史一致性
    /// - 如果項目已存在，則累加數量而非建立新項目
    /// - 使用 JsonDocument 儲存 SKU 屬性，需要正確處理資源釋放
    /// </summary>
    /// <param name="request">新增購物車項目命令物件，包含購物車項目的所有資訊</param>
    /// <returns>儲存後的購物車項目實體，包含自動生成的 ID</returns>
    public async Task<CartItem> HandleAsync(CartItemAddCommand request)
    {
        // ========== 第一步：驗證購物車類型 ==========
        if (request.Type != "shopping" && request.Type != "wishlist")
            throw new ArgumentException("購物車類型必須是 'shopping' 或 'wishlist'");

        // ========== 第二步：驗證數量 ==========
        if (request.Quantity <= 0)
            throw new ArgumentException("數量必須大於 0");

        // ========== 第三步：查詢 SKU 是否存在 ==========
        var sku = await _productRepository.GetSkuAsync(request.SkuId);
        if (sku == null)
            throw Failure.NotFound($"SKU 不存在，ID: {request.SkuId}");

        // ========== 第四步：查詢購物車項目是否已存在 ==========
        var userId = _userClaim.Id;

        // 查詢購物車項目
        var existingItem = await _repository.GetAsync(q => 
            q.Where(x => 
                x.UserId == userId && 
                x.SkuId == request.SkuId && 
                x.CartType == request.Type)
        );

        // ========== 第五步：如果項目已存在，累加數量 ==========
        if (existingItem != null)
        {
            // 累加數量
            existingItem.Quantity += request.Quantity;
            
            // 更新時間戳
            existingItem.UpdatedAt = DateTimeOffset.UtcNow;
            
            // 儲存變更
            await _repository.SaveChangeAsync();
            
            // 回傳更新後的實體
            return existingItem;
        }

        // ========== 第六步：建立新的購物車項目 ==========
        var cartItem = new CartItem
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定使用者資訊
            UserId = userId,
            
            // 設定購物車類型
            CartType = request.Type,
            
            // 設定商品資訊
            ProductId = sku.ProductId,
            SkuId = sku.Id,
            
            // 設定商品快照資訊
            ProductName = sku.Name,
            SkuAttributes = sku.Specs ?? new List<Specification>(),
            UnitPrice = sku.Price,
            Currency = "NTD",
            Quantity = request.Quantity,
            ProductImage = sku.ImageUrl ?? string.Empty,
            
            // 設定時間戳
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第七步：將實體加入倉儲 ==========
        _repository.Add(cartItem);

        // ========== 第八步：儲存變更到資料庫 ==========
        await _repository.SaveChangeAsync();

        // ========== 第九步：回傳儲存後的實體 ==========
        return cartItem;
    }
}
