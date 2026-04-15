using Manian.Domain.Entities.Promotions;

namespace Manian.Domain.Repositories.Promotions;

/// <summary>
/// 促銷活動倉儲介面
/// 
/// 職責：
/// - 定義促銷活動相關的資料存取操作
/// - 繼承泛型 IRepository<Promotion> 獲得通用 CRUD 功能
/// - 擴展促銷規則和促銷範圍的特定查詢和操作方法
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 介面隔離原則（ISP）：只定義特定方法，不暴露不必要功能
/// - 依賴反轉原則（DIP）：依賴抽象而非實作
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 由 Infrastructure 層的 PromotionRepository 實作
/// - 被 Application 層的 Query/Command 使用
/// 
/// 使用場景：
/// - 促銷活動管理（新增、查詢、更新、刪除）
/// - 促銷規則管理（新增、查詢、刪除）
/// - 促銷範圍管理（新增、查詢、刪除）
/// 
/// 關聯實體：
/// - Promotion：促銷活動實體（主實體）
/// - PromotionRule：促銷規則實體（關聯實體）
/// - PromotionScope：促銷範圍實體（關聯實體）
/// 
/// 設計特點：
/// - 提供規則和範圍的特定方法（GetRulesAsync、GetScopesAsync）
/// - 支援規則和範圍的新增和刪除（AddRule、DeleteRule、AddScope、DeleteScope）
/// - 不包含規則和範圍的更新邏輯（應使用專用的 UpdateCommand）
/// 
/// 參考實作：
/// - IProductRepository：類似的擴展方法設計（AddSku、GetSkusAsync）
/// - ICategoryRepository：類似的擴展方法設計（GetAttributeKeysAsync）
/// </summary>
public interface IPromotionRepository : IRepository<Promotion>
{
    /// <summary>
    /// 查詢指定促銷活動的所有規則
    /// 
    /// 職責：
    /// - 從資料庫查詢指定促銷活動 ID 的所有規則
    /// - 包含關聯的 Promotion 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 不包含分頁邏輯（如需要，應新增專用查詢方法）
    /// 
    /// 使用場景：
    /// - 促銷活動規則總覽
    /// - 促銷活動編輯頁面
    /// - 規則報表生成
    /// 
    /// 注意事項：
    /// - 如果促銷活動沒有規則，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含規則的詳細計算邏輯（由 PromotionRule 實體處理）
    /// </summary>
    /// <param name="promotionId">促銷活動 ID</param>
    /// <returns>該促銷活動的所有規則集合</returns>
    Task<IEnumerable<PromotionRule>> GetRulesAsync(int promotionId);

    /// <summary>
    /// 根據規則 ID 查詢單一規則
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的規則
    /// - 包含關聯的 Promotion 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳單一實體或 null
    /// - 包含 Promotion 實體以減少後續查詢
    /// 
    /// 使用場景：
    /// - 規則詳情頁顯示
    /// - 規則編輯頁面
    /// - 規則刪除前確認
    /// 
    /// 注意事項：
    /// - 如果規則不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 不包含規則的詳細計算邏輯（由 PromotionRule 實體處理）
    /// </summary>
    /// <param name="ruleId">規則 ID</param>
    /// <returns>查詢到的規則實體，若不存在則回傳 null</returns>
    Task<PromotionRule?> GetRuleAsync(int ruleId);

    /// <summary>
    /// 新增促銷規則
    /// 
    /// 職責：
    /// - 將規則實體加入資料庫追蹤
    /// - 設定規則與 Promotion 的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保規則的 PromotionId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 創建促銷活動時新增預設規則
    /// - 為現有促銷活動新增新的規則
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證規則的 PromotionId 是否正確
    /// - 不應使用此方法更新規則（應使用專用的 UpdateCommand）
    /// 
    /// 資料約束：
    /// - PromotionId 必須存在（外鍵約束）
    /// - RuleType 必須為有效值（由 PromotionRule 實體驗證）
    /// </summary>
    /// <param name="rule">要新增的規則實體</param>
    void AddRule(PromotionRule rule);

    /// <summary>
    /// 刪除促銷規則
    /// 
    /// 職責：
    /// - 將規則實體標記為待刪除
    /// - 確保資料庫約束不會被違反
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 使用 EF Core 的變更追蹤機制
    /// - 檢查實體是否存在於資料庫中
    /// 
    /// 使用場景：
    /// - 刪除促銷活動的特定規則
    /// - 促銷活動規則調整
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 如果規則有關聯的訂單，刪除會失敗
    /// - 建議在刪除前檢查是否有關聯資料
    /// 
    /// 資料約束：
    /// - 如果有關聯的訂單記錄，刪除會失敗
    /// - 由 PromotionRuleConfiguration.cs 設定 OnDelete(DeleteBehavior.Restrict)
    /// </summary>
    /// <param name="rule">要刪除的規則實體</param>
    void DeleteRule(PromotionRule rule);

    /// <summary>
    /// 查詢指定促銷活動的所有範圍
    /// 
    /// 職責：
    /// - 從資料庫查詢指定促銷活動 ID 的所有範圍
    /// - 包含關聯的 Promotion 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳集合而非單一實體
    /// - 不包含分頁邏輯（如需要，應新增專用查詢方法）
    /// 
    /// 使用場景：
    /// - 促銷活動範圍總覽
    /// - 促銷活動編輯頁面
    /// - 範圍報表生成
    /// 
    /// 注意事項：
    /// - 如果促銷活動沒有範圍，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// - 不包含範圍的詳細計算邏輯（由 PromotionScope 實體處理）
    /// </summary>
    /// <param name="promotionId">促銷活動 ID</param>
    /// <returns>該促銷活動的所有範圍集合</returns>
    Task<IEnumerable<PromotionScope>> GetScopesAsync(int promotionId);

    /// <summary>
    /// 根據範圍 ID 查詢單一範圍
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的範圍
    /// - 包含關聯的 Promotion 實體
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 回傳單一實體或 null
    /// - 包含 Promotion 實體以減少後續查詢
    /// 
    /// 使用場景：
    /// - 範圍詳情頁顯示
    /// - 範圍編輯頁面
    /// - 範圍刪除前確認
    /// 
    /// 注意事項：
    /// - 如果範圍不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// - 不包含範圍的詳細計算邏輯（由 PromotionScope 實體處理）
    /// </summary>
    /// <param name="scopeId">範圍 ID</param>
    /// <returns>查詢到的範圍實體，若不存在則回傳 null</returns>
    Task<PromotionScope?> GetScopeAsync(int scopeId);

    /// <summary>
    /// 新增促銷範圍
    /// 
    /// 職責：
    /// - 將範圍實體加入資料庫追蹤
    /// - 設定範圍與 Promotion 的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保範圍的 PromotionId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 創建促銷活動時新增預設範圍
    /// - 為現有促銷活動新增新的範圍
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證範圍的 PromotionId 是否正確
    /// - 不應使用此方法更新範圍（應使用專用的 UpdateCommand）
    /// 
    /// 資料約束：
    /// - PromotionId 必須存在（外鍵約束）
    /// - ScopeType 必須為有效值（由 PromotionScope 實體驗證）
    /// </summary>
    /// <param name="scope">要新增的範圍實體</param>
    void AddScope(PromotionScope scope);

    /// <summary>
    /// 刪除促銷範圍
    /// 
    /// 職責：
    /// - 將範圍實體標記為待刪除
    /// - 確保資料庫約束不會被違反
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 使用 EF Core 的變更追蹤機制
    /// - 檢查實體是否存在於資料庫中
    /// 
    /// 使用場景：
    /// - 刪除促銷活動的特定範圍
    /// - 促銷活動範圍調整
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 如果範圍有關聯的訂單，刪除會失敗
    /// - 建議在刪除前檢查是否有關聯資料
    /// 
    /// 資料約束：
    /// - 如果有關聯的訂單記錄，刪除會失敗
    /// - 由 PromotionScopeConfiguration.cs 設定 OnDelete(DeleteBehavior.Restrict)
    /// </summary>
    /// <param name="scope">要刪除的範圍實體</param>
    void DeleteScope(PromotionScope scope);
}
