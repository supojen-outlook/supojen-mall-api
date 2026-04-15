using Manian.Domain.Repositories.Promotions;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Promotions;

/// <summary>
/// 刪除優惠券命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除優惠券所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 管理員刪除不需要的優惠券
/// - 清理測試資料
/// - 優惠券結束後的清理
/// 
/// 注意事項：
/// - 已使用的優惠券不允許刪除
/// - 建議在刪除前檢查是否有關聯的訂單
/// - 軟刪除可能比硬刪除更安全
/// </summary>
public class CouponDeleteCommand : IRequest
{
    /// <summary>
    /// 優惠券唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要刪除的優惠券
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
}

/// <summary>
/// 刪除優惠券命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 CouponDeleteCommand 命令
/// - 查詢優惠券是否存在
/// - 驗證優惠券是否已被使用
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<CouponDeleteCommand> 介面
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
/// - 未檢查優惠券是否有關聯的訂單
/// - 建議考慮使用軟刪除（標記為已刪除）而非硬刪除
/// 
/// 參考實作：
/// - BrandDeleteHandler：類似的刪除邏輯
/// - ProductDeleteHandler：類似的刪除邏輯
/// </summary>
internal class CouponDeleteHandler : IRequestHandler<CouponDeleteCommand>
{
    /// <summary>
    /// 優惠券倉儲介面
    /// 
    /// 用途：
    /// - 存取優惠券資料
    /// - 提供查詢、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/CouponRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、DeleteAsync 等
    /// - 繼承自 Repository<Coupon>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/ICouponRepository.cs
    /// </summary>
    private readonly ICouponRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">優惠券倉儲，用於查詢和刪除優惠券</param>
    public CouponDeleteHandler(ICouponRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理刪除優惠券命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢優惠券實體
    /// 2. 驗證優惠券是否存在
    /// 3. 驗證優惠券是否已被使用
    /// 4. 刪除優惠券
    /// 5. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 優惠券不存在：拋出 Failure.NotFound()
    /// - 優惠券已被使用：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆，建議在 UI 層加入確認對話框
    /// - 已使用的優惠券不允許刪除
    /// - 建議檢查優惠券是否有關聯的訂單
    /// </summary>
    /// <param name="request">刪除優惠券命令物件，包含優惠券 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(CouponDeleteCommand request)
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
        // 如果優惠券已被使用，不允許刪除
        // 這是為了保護訂單資料的完整性
        if (coupon.IsUsed)
        {
            throw Failure.BadRequest(
                $"優惠券已被使用，無法刪除。使用時間：{coupon.UsedAt}，訂單 ID：{coupon.OrderId}");
        }

        // ========== 第四步：刪除優惠券 ==========
        // 使用 ICouponRepository.Delete() 刪除優惠券
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 如果需要軟刪除，應該改為更新優惠券的狀態欄位
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        _repository.Delete(coupon);

        // ========== 第五步：儲存變更 ==========
        // 使用 ICouponRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();
    }
}
