using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 新增優惠券命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增優惠券所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Coupon>，表示這是一個會回傳 Coupon 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 CouponAddHandler 配合使用，完成新增優惠券的業務邏輯
/// 
/// 使用場景：
/// - 管理員建立新的優惠券
/// - 系統自動建立優惠券（如註冊送優惠券）
/// - API 端點接收優惠券新增請求
/// 
/// 設計特點：
/// - 包含優惠券基本資訊（代碼、名稱、描述等）
/// - 包含優惠券折扣資訊（折扣率）
/// - 包含優惠券適用範圍（scope_type、scope_id）
/// - 包含優惠券有效期資訊
/// - 支援指定用戶（user_id）
/// </summary>
public class CouponAddCommand : IRequest<Coupon>
{
    /// <summary>
    /// 優惠券代碼，用戶輸入
    /// 
    /// 用途：
    /// - 用戶在結帳時輸入的優惠券代碼
    /// - 用於驗證優惠券有效性
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 必須唯一（由資料庫唯一約束保證）
    /// - 建議長度限制：1-50 字元
    /// 
    /// 範例：
    /// - "VIP85"：VIP專屬85折
    /// - "NEWUSER"：新用戶專屬優惠
    /// - "SUMMER2024"：夏季促銷優惠券
    /// </summary>
    public string? CouponCode { get; set; }


    /// <summary>
    /// 優惠券名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的優惠券名稱
    /// - 用於優惠券列表和詳細頁面
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-100 字元
    /// 
    /// 範例：
    /// - "VIP專屬85折"
    /// - "新用戶專屬優惠"
    /// - "夏季清倉大特賣"
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 優惠券描述
    /// 
    /// 用途：
    /// - 提供優惠券的詳細說明
    /// - 可用於 SEO 優化
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 優惠券詳細頁面
    /// - 優惠券列表頁面
    /// - 行銷推廣文案
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 指定給特定用戶，NULL 表示不指定
    /// 
    /// 用途：
    /// - 建立專屬優惠券給特定用戶
    /// - NULL 表示公開優惠券，任何用戶都可以使用
    /// 
    /// 預設值：
    /// - null（公開優惠券）
    /// 
    /// 使用場景：
    /// - VIP 專屬優惠券
    /// - 補償優惠券（如訂單問題補償）
    /// - 生日優惠券
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 折扣率，如：15.00 表示 85折
    /// 
    /// 用途：
    /// - 定義優惠券的折扣比例
    /// - 0 表示免費，100 表示無折扣
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 必須小於或等於 100
    /// - 建議使用 2 位小數
    /// 
    /// 範例：
    /// - 15.00：85折（折扣 15%）
    /// - 20.00：8折（折扣 20%）
    /// - 50.00：5折（折扣 50%）
    /// </summary>
    public decimal DiscountRate { get; set; }

    /// <summary>
    /// 適用範圍：all全部/product商品/category類別/brand品牌
    /// 
    /// 用途：
    /// - 定義優惠券的適用範圍
    /// - 限制優惠券只能用於特定商品、類別或品牌
    /// 
    /// 可選值：
    /// - "all"：全館適用（預設值）
    /// - "product"：特定商品
    /// - "category"：特定類別
    /// - "brand"：特定品牌
    /// 
    /// 預設值：
    /// - "all"（如果未提供）
    /// 
    /// 使用場景：
    /// - 全館優惠券
    /// - 特定商品優惠券
    /// - 特定類別優惠券
    /// - 特定品牌優惠券
    /// </summary>
    public string ScopeType { get; set; } = "all";

    /// <summary>
    /// 根據 scope_type 對應到不同表的 ID
    /// 
    /// 用途：
    /// - 當 ScopeType 不為 "all" 時，指定具體的商品、類別或品牌 ID
    /// - 當 ScopeType 為 "all" 時，此欄位應為 null
    /// 
    /// 預設值：
    /// - null（當 ScopeType 為 "all" 時）
    /// 
    /// 驗證規則：
    /// - 當 ScopeType 為 "all" 時，必須為 null
    /// - 當 ScopeType 不為 "all" 時，必須有值
    /// - 必須對應資料庫中存在的 ID
    /// 
    /// 範例：
    /// - ScopeType = "product"：商品 ID
    /// - ScopeType = "category"：類別 ID
    /// - ScopeType = "brand"：品牌 ID
    /// </summary>
    public int? ScopeId { get; set; }

    /// <summary>
    /// 有效開始時間
    /// 
    /// 用途：
    /// - 定義優惠券的開始使用時間
    /// - 在此時間之前，優惠券無法使用
    /// 
    /// 預設值：
    /// - DateTimeOffset.UtcNow（當前 UTC 時間）
    /// 
    /// 注意事項：
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// - 建議在前端進行時區轉換
    /// </summary>
    public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 有效截止時間，NULL 表示永久有效
    /// 
    /// 用途：
    /// - 定義優惠券的結束使用時間
    /// - 在此時間之後，優惠券無法使用
    /// 
    /// 預設值：
    /// - null（永久有效）
    /// 
    /// 驗證規則：
    /// - 必須晚於 ValidFrom
    /// - 建議使用 UTC 時間
    /// 
    /// 注意事項：
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// - 建議在前端進行時區轉換
    /// </summary>
    public DateTimeOffset? ValidUntil { get; set; }
}

/// <summary>
/// 新增優惠券命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CouponAddCommand 命令
/// - 建立新的 Coupon 實體
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 Coupon 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CouponAddCommand, Coupon> 介面
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
internal class CouponAddHandler : IRequestHandler<CouponAddCommand, Coupon>
{
    /// <summary>
    /// 優惠券倉儲介面
    /// 
    /// 用途：
    /// - 存取優惠券資料
    /// - 提供新增、查詢等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/CouponRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<Coupon>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/ICouponRepository.cs
    /// </summary>
    private readonly ICouponRepository _repository;

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
    /// 
    /// 設計考量：
    /// - 確保在分散式環境下的唯一性
    /// - 避免使用資料庫自增 ID（不適合分散式環境）
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">優惠券倉儲，用於新增優惠券</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生優惠券 ID</param>
    public CouponAddHandler(
        ICouponRepository repository, 
        IUniqueIdentifier uniqueIdentifier)
    {
        _repository = repository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增優惠券命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證優惠券代碼是否已存在
    /// 2. 建立新的 Coupon 實體
    /// 3. 設定實體屬性
    /// 4. 將實體加入倉儲
    /// 5. 儲存變更到資料庫
    /// 6. 回傳儲存後的實體
    /// 
    /// 返回值：
    /// - Coupon：儲存後的優惠券實體，包含自動生成的 ID
    /// 
    /// 錯誤處理：
    /// - 優惠券代碼已存在：拋出 Failure.BadRequest("優惠券代碼已存在")
    /// 
    /// 注意事項：
    /// - 新增後的實體會包含自動生成的 ID
    /// - 建議在 UI 層顯示新增成功的訊息
    /// </summary>
    /// <param name="request">新增優惠券命令物件，包含優惠券的所有資訊</param>
    /// <returns>儲存後的優惠券實體，包含自動生成的 ID</returns>
    public async Task<Coupon> HandleAsync(CouponAddCommand request)
    {
        // ========== 第一步：驗證優惠券代碼唯一性 ==========
        // 查詢資料庫中是否已存在相同代碼的優惠券
        var existingCoupon = await _repository.GetAsync(
            q => q.Where(c => c.CouponCode == request.CouponCode)
        );
        if (existingCoupon != null)
            throw Failure.BadRequest("優惠券代碼已存在");

        // ========== 第二步：產生優惠券 ID ==========
        // 使用雪花演算法產生全域唯一 ID
        var id = _uniqueIdentifier.NextInt();

        // ========== 第三步：建立 Coupon 實體 ==========
        // 若未提供代碼，則使用 ID 作為代碼
        var coupon = new Coupon
        {
            // 產生全域唯一的整數 ID
            // 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
            Id = id,
            
            // 設定基本屬性
            CouponCode = request.CouponCode ?? id.ToString(),
            Name = request.Name,
            Description = request.Description,
            
            // 設定擁有者資訊
            UserId = request.UserId,
            
            // 設定折扣內容
            DiscountAmount = request.DiscountRate,
            
            // 設定適用範圍
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            
            // 設定使用狀態（預設值）
            IsUsed = false,
            UsedAt = null,
            OrderId = null,
            
            // 設定有效期
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第四步：加入倉儲並儲存 ==========
        // 將實體加入追蹤並寫入資料庫
        _repository.Add(coupon);

        // ========== 第五步：回傳結果 ==========
        // 回傳儲存後的 Coupon 實體
        await _repository.SaveChangeAsync();

        // ========== 第六步：回傳儲存後的實體 ==========
        // 回傳儲存後的 Coupon 實體
        // 包含所有屬性值，包括自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return coupon;
    }
}
