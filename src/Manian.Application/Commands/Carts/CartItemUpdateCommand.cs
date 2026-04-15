using Manian.Application.Services;
using Manian.Domain.Entities.Carts;
using Manian.Domain.Repositories.Carts;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Carts;

/// <summary>
/// 更新購物車項目命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新購物車項目所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 使用者修改購物車項目數量
/// - 使用者從願望清單移至購物車
/// - 使用者從購物車移至願望清單
/// 
/// 設計特點：
/// - 支援數量更新
/// - 支援購物車類型切換
/// - 不回傳資料（遵循 HTTP PUT 語意）
/// </summary>
public class CartItemUpdateCommand : IRequest
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
    /// 購物車項目 ID
    /// 
    /// 用途：
    /// - 指定要更新的購物車項目
    /// 
    /// 驗證規則：
    /// - 必須是正整數
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 數量
    /// 
    /// 用途：
    /// - 指定購物車項目的數量
    /// - 直接設定數量（非累加）
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
/// 更新購物車項目命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CartItemUpdateCommand 命令
/// - 查詢購物車項目是否存在
/// - 更新購物車項目資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CartItemUpdateCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICartItemRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查購物車項目是否屬於當前使用者
/// - 建議在實際專案中加入使用者權限檢查
/// </summary>
internal class CartItemUpdateHandler : IRequestHandler<CartItemUpdateCommand>
{
    /// <summary>
    /// 購物車倉儲介面
    /// 
    /// 用途：
    /// - 存取購物車項目資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Carts/CartItemRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 繼承自 Repository<CartItem>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Carts/ICartItemRepository.cs
    /// </summary>
    private readonly ICartItemRepository _repository;

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
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">購物車倉儲，用於查詢和更新購物車項目</param>
    /// <param name="userClaim">使用者宣告，用於取得使用者 ID</param>
    public CartItemUpdateHandler(
        ICartItemRepository repository,
        IUserClaim userClaim)
    {
        _repository = repository;
        _userClaim = userClaim;
    }

    /// <summary>
    /// 處理更新購物車項目命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證購物車類型
    /// 2. 驗證數量
    /// 3. 查詢購物車項目是否存在
    /// 4. 更新購物車項目資訊
    /// 5. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 購物車類型無效：拋出 ArgumentException
    /// - 數量小於等於 0：拋出 ArgumentException
    /// - 購物車項目不存在：拋出 Failure.NotFound()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查購物車項目是否屬於當前使用者
    /// </summary>
    /// <param name="request">更新購物車項目命令物件，包含購物車項目的所有資訊</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(CartItemUpdateCommand request)
    {
        // ========== 第一步：驗證購物車類型 ==========
        if (request.Type != "shopping" && request.Type != "wishlist")
            throw new ArgumentException("購物車類型必須是 'shopping' 或 'wishlist'");

        // ========== 第二步：驗證數量 ==========
        if (request.Quantity <= 0)
            throw new ArgumentException("數量必須大於 0");

        // ========== 第三步：查詢購物車項目是否存在 ==========
        var userId = _userClaim.Id;
        
        // 查詢購物車項目
        var cartItem = await _repository.GetAsync(q => 
            q.Where(x => 
                x.UserId == userId && 
                x.Id == request.Id &&
                x.CartType == request.Type)
        );

        // 驗證購物車項目是否存在
        if (cartItem == null)
            throw Failure.NotFound($"購物車項目不存在，SKU ID: {request.Id}");

        // ========== 第四步：更新購物車項目資訊 ==========
        // 更新數量
        cartItem.Quantity = request.Quantity;
        
        // 更新時間戳
        cartItem.UpdatedAt = DateTimeOffset.UtcNow;

        // ========== 第五步：儲存變更 ==========
        await _repository.SaveChangeAsync();
    }
}
