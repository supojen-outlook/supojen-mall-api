using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 更新優惠券命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新優惠券所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員修改優惠券資訊
/// - 優惠券資料維護
/// - 優惠券狀態調整
/// 
/// 設計特點：
/// - 所有屬性皆為可空（nullable），支援部分更新
/// - 未提供的欄位保持原值不變
/// - 遵循 HTTP PATCH 語意（部分更新）
/// 
/// 注意事項：
/// - 已使用的優惠券不允許修改折扣率和適用範圍
/// - 建議在 UI 層加入確認對話框
/// - 建議檢查優惠券是否有關聯的訂單
/// </summary>
public class CouponUpdateCommand : IRequest
{
    /// <summary>
    /// 優惠券唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的優惠券
    /// - 必須是資料庫中已存在的優惠券 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的優惠券
    /// 
    /// 錯誤處理：
    /// - 如果優惠券不存在，會拋出 Failure.NotFound()
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 優惠券代碼
    /// 
    /// 用途：
    /// - 用戶在結帳時輸入的優惠券代碼
    /// - 用於驗證優惠券有效性
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 必須唯一（由資料庫唯一約束保證）
    /// - 建議長度限制：1-50 字元
    /// 
    /// 注意事項：
    /// - 優惠券代碼變更會影響已發放的優惠券
    /// - 建議檢查是否有使用者已領取此優惠券
    /// </summary>
    public string? CouponCode { get; set; }

    /// <summary>
    /// 優惠券名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的優惠券名稱
    /// - 用於優惠券列表和詳細頁面
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-100 字元
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 優惠券描述
    /// 
    /// 用途：
    /// - 提供優惠券的詳細說明
    /// - 可用於 SEO 優化
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
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
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 使用場景：
    /// - VIP 專屬優惠券
    /// - 補償優惠券（如訂單問題補償）
    /// - 生日優惠券
    /// 
    /// 注意事項：
    /// - 從公開優惠券改為專屬優惠券會影響其他使用者
    /// - 從專屬優惠券改為公開優惠券會讓所有使用者可使用
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 折扣率，如：15.00 表示 85折
    /// 
    /// 用途：
    /// - 定義優惠券的折扣比例
    /// - 0 表示免費，100 表示無折扣
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須大於或等於 0
    /// - 必須小於或等於 100
    /// - 建議使用 2 位小數
    /// 
    /// 注意事項：
    /// - 已使用的優惠券不允許修改折扣率
    /// - 折扣率變更會影響未使用的優惠券
    /// </summary>
    public decimal? DiscountRate { get; set; }

    /// <summary>
    /// 適用範圍：all全部/product商品/category類別/brand品牌
    /// 
    /// 用途：
    /// - 定義優惠券的適用範圍
    /// - 限制優惠券只能用於特定商品、類別或品牌
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 可選值：
    /// - "all"：全館適用
    /// - "product"：特定商品
    /// - "category"：特定類別
    /// - "brand"：特定品牌
    /// 
    /// 注意事項：
    /// - 已使用的優惠券不允許修改適用範圍
    /// - 適用範圍變更會影響未使用的優惠券
    /// </summary>
    public string? ScopeType { get; set; }

    /// <summary>
    /// 根據 scope_type 對應到不同表的 ID
    /// 
    /// 用途：
    /// - 當 ScopeType 不為 "all" 時，指定具體的商品、類別或品牌 ID
    /// - 當 ScopeType 為 "all" 時，此欄位應為 null
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 當 ScopeType 為 "all" 時，必須為 null
    /// - 當 ScopeType 不為 "all" 時，必須有值
    /// - 必須對應資料庫中存在的 ID
    /// 
    /// 注意事項：
    /// - 已使用的優惠券不允許修改 ScopeId
    /// - ScopeId 變更會影響未使用的優惠券
    /// </summary>
    public int? ScopeId { get; set; }

    /// <summary>
    /// 有效開始時間
    /// 
    /// 用途：
    /// - 定義優惠券的開始使用時間
    /// - 在此時間之前，優惠券無法使用
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// - 建議在前端進行時區轉換
    /// - 有效開始時間變更會影響未使用的優惠券
    /// </summary>
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>
    /// 有效截止時間，NULL 表示永久有效
    /// 
    /// 用途：
    /// - 定義優惠券的結束使用時間
    /// - 在此時間之後，優惠券無法使用
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 驗證規則：
    /// - 必須晚於 ValidFrom
    /// - 建議使用 UTC 時間
    /// 
    /// 注意事項：
    /// - 時間格式應與資料庫一致（TIMESTAMPTZ）
    /// - 建議在前端進行時區轉換
    /// - 有效截止時間變更會影響未使用的優惠券
    /// </summary>
    public DateTimeOffset? ValidUntil { get; set; }
}

/// <summary>
/// 更新優惠券命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CouponUpdateCommand 命令
/// - 查詢優惠券是否存在
/// - 更新優惠券資訊
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CouponUpdateCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ICouponRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查優惠券是否已被使用
/// - 未檢查優惠券是否有關聯的訂單
/// - 未檢查 CouponCode 是否唯一
/// - 建議在實際專案中加入這些檢查
/// </summary>
internal class CouponUpdateHandler : IRequestHandler<CouponUpdateCommand>
{
    /// <summary>
    /// 優惠券倉儲介面
    /// 
    /// 用途：
    /// - 存取優惠券資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/CouponRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 繼承自 Repository<Coupon>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/ICouponRepository.cs
    /// </summary>
    private readonly ICouponRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">優惠券倉儲，用於查詢和更新優惠券</param>
    public CouponUpdateHandler(ICouponRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新優惠券命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢優惠券實體
    /// 2. 驗證優惠券是否存在
    /// 3. 驗證優惠券是否已被使用
    /// 4. 更新優惠券屬性（只更新非 null 的欄位）
    /// 5. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 優惠券不存在：拋出 Failure.NotFound()
    /// - 優惠券已被使用：拋出 Failure.BadRequest()
    /// - CouponCode 重複：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 建議檢查優惠券是否有關聯的訂單
    /// - 建議檢查 CouponCode 是否唯一
    /// </summary>
    /// <param name="request">更新優惠券命令物件，包含優惠券 ID 和要更新的欄位</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(CouponUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢優惠券實體 ==========
        // 使用 ICouponRepository.GetByIdAsync() 查詢優惠券
        // 這個方法會從資料庫中取得完整的優惠券實體
        var coupon = await _repository.GetByIdAsync(request.Id);
        
        // ========== 第二步：驗證優惠券是否存在 ==========
        // 如果找不到優惠券，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 優惠券 ID 不存在
        // - 優惠券已被刪除（軟刪除）
        if (coupon == null)
            throw Failure.NotFound($"優惠券不存在，ID: {request.Id}");

        // ========== 第三步：驗證優惠券是否已被使用 ==========
        // 如果優惠券已被使用，不允許修改折扣率和適用範圍
        if (coupon.IsUsed)
        {
            // 檢查是否嘗試修改折扣率
            if (request.DiscountRate.HasValue && request.DiscountRate != coupon.DiscountAmount)
                throw Failure.BadRequest("已使用的優惠券不允許修改折扣率");
            
            // 檢查是否嘗試修改適用範圍
            if (request.ScopeType != null && request.ScopeType != coupon.ScopeType)
                throw Failure.BadRequest("已使用的優惠券不允許修改適用範圍");
            
            if (request.ScopeId != null && request.ScopeId != coupon.ScopeId)
                throw Failure.BadRequest("已使用的優惠券不允許修改適用範圍");
        }

        // ========== 第四步：驗證 CouponCode 是否唯一 ==========
        // 如果提供了新的 CouponCode，需要檢查是否與其他優惠券重複
        if (request.CouponCode != null && request.CouponCode != coupon.CouponCode)
        {
            var existingCoupon = await _repository.GetAsync(
                q => q.Where(c => c.CouponCode == request.CouponCode)
            );
            if (existingCoupon != null)
                throw Failure.BadRequest("優惠券代碼已存在");
        }

        // ========== 第五步：更新優惠券屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        
        // 更新基本屬性
        if (request.CouponCode != null) coupon.CouponCode = request.CouponCode;
        if (request.Name != null) coupon.Name = request.Name;
        if (request.Description != null) coupon.Description = request.Description;
        
        // 更新擁有者資訊
        if (request.UserId != null) coupon.UserId = request.UserId;
        
        // 更新折扣內容
        if (request.DiscountRate.HasValue) coupon.DiscountAmount = request.DiscountRate.Value;
        
        // 更新適用範圍
        if (request.ScopeType != null) coupon.ScopeType = request.ScopeType;
        if (request.ScopeId != null) coupon.ScopeId = request.ScopeId;
        
        // 更新有效期
        if (request.ValidFrom.HasValue) coupon.ValidFrom = request.ValidFrom.Value;
        if (request.ValidUntil.HasValue) coupon.ValidUntil = request.ValidUntil.Value;

        // ========== 第六步：儲存變更 ==========
        // 使用 ICouponRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
