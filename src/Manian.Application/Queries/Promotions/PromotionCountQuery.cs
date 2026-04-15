using Manian.Domain.Repositories.Promotions;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Promotions;

/// <summary>
/// 查詢促銷活動總數的請求物件
/// 
/// 用途：
/// - 取得促銷活動的總數量
/// - 用於儀表板、統計報表等場景
/// 
/// 設計模式：
/// - 實作 IRequest<int>，表示這是一個查詢請求，回傳整數（促銷活動總數）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PromotionCountQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 儀表板顯示促銷活動總數
/// - 統計報表生成
/// - 系統監控
/// 
/// 參考實作：
/// - CategoryCountQuery：相同的實作模式，用於查詢類別總數
/// - CouponCountQuery：相同的實作模式，用於查詢優惠券總數
/// </summary>
public class PromotionCountQuery : IRequest<int>;

/// <summary>
/// 促銷活動數量查詢處理器
/// 
/// 職責：
/// - 接收 PromotionCountQuery 請求
/// - 從資料庫取得促銷活動的總數量
/// - 優先使用估計數量，提升效能
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PromotionCountQuery, int> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 參考實作：
/// - CategoryCountQueryHandler：相同的實作模式
/// - CouponCountQueryHandler：相同的實作模式
/// </summary>
public class PromotionCountQueryHandler : IRequestHandler<PromotionCountQuery, int>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷活動資料
    /// - 提供估計數量和精確計數兩種方法
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - EstimatedCount() 查詢 PostgreSQL 系統目錄，效能高但不精確
    /// - CountAsync() 執行精確計數，效能較低但結果準確
    /// 
    /// 注意事項：
    /// - PromotionRepository 目前沒有定義額外方法
    /// - 完全依賴父類別 Repository<Promotion> 提供的功能
    /// - EstimatedCount() 和 CountAsync() 來自父類別
    /// </summary>
    private readonly IPromotionRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢促銷活動數量</param>
    public PromotionCountQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理促銷活動數量查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 嘗試取得估計數量（快速但不精確）
    /// 2. 如果估計數量不可用，執行精確計數（較慢但準確）
    /// 3. 回傳促銷活動總數
    /// 
    /// 效能考量：
    /// - 優先使用 EstimatedCount()，避免全表掃描
    /// - 只有在估計數量不可用時才執行 CountAsync()
    /// - 適合資料量大的場景，避免影響系統效能
    /// 
    /// 精確度考量：
    /// - 估計數量誤差通常在 1-5% 以內
    /// - 如果需要精確數量，可以改為直接呼叫 CountAsync()
    /// 
    /// 與 CategoryCountQuery 的對比：
    /// - 實作模式完全相同
    /// - 都優先使用估計數量
    /// - 都在估計數量不可用時執行精確計數
    /// </summary>
    /// <param name="request">促銷活動數量查詢請求物件（不包含任何屬性）</param>
    /// <returns>促銷活動總數（整數）</returns>
    public async Task<int> HandleAsync(PromotionCountQuery request)
    {
        // ========== 第一步：嘗試取得估計數量 ==========
        // 呼叫 EstimatedCount() 取得 PostgreSQL 系統目錄中的估計筆數
        // 這個方法不掃描實際資料表，效能極高
        // 來自父類別 Repository<T> 的實作
        var count = _repository.EstimatedCount();

        // ========== 第二步：如果估計數量不可用，執行精確計數 ==========
        // 以下情況會執行精確計數：
        // - 資料表從未分析（統計資訊不存在）
        // - 查詢系統目錄時發生錯誤
        if (count == null)
        {
            // 執行精確計數，會掃描整個資料表
            // 資料量大時可能會影響效能
            // 來自父類別 Repository<T> 的實作
            count = await _repository.CountAsync();
        }

        // ========== 第三步：回傳促銷活動總數 ==========
        // 使用 .Value 取得可空整數的值
        // 由於上面已經處理 null 情況，這裡可以安全地取值
        return count.Value;
    }
}
