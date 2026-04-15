using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Promotions;

/// <summary>
/// 促銷活動倉儲實作類別
/// 
/// 職責：
/// - 實作 IPromotionRepository 介面
/// - 處理 Promotion、PromotionRule、PromotionScope 實體的所有資料庫操作
/// - 管理 Promotion 與 PromotionRule、PromotionScope 的關聯關係
/// - 繼承泛型 Repository<Promotion> 獲得通用 CRUD 功能
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 繼承模式：複用泛型 Repository 的通用功能
/// - 依賴注入：透過建構函式注入 MainDbContext
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 Domain 層的 IPromotionRepository 介面
/// - 使用 MainDbContext 與資料庫互動
/// 
/// 生命週期：
/// - 註冊為 Scoped（見 Infrastructure/DI.cs）
/// - 每個 HTTP 請求一個實例
/// 
/// 參考實作：
/// - CategoryRepository：展示了如何處理多對多關係（UpdateAttributeKeysAsync）
/// - ProductRepository：展示了如何處理一對多關係（AddSku、GetSkusAsync）
/// </summary>
public class PromotionRepository : Repository<Promotion>, IPromotionRepository
{
    /// <summary>
    /// 建構函式
    /// 
    /// 職責：
    /// - 初始化倉儲實例
    /// - 注入資料庫上下文
    /// - 傳遞給父類別 Repository<Promotion>
    /// 
    /// 參數說明：
    /// - context：MainDbContext 實例，用於資料庫操作
    /// 
    /// 設計考量：
    /// - 不指定主鍵屬性名稱，使用父類別預設值
    /// - 與 CategoryRepository 不同，CategoryRepository 明確指定 "Id"
    /// 
    /// 父類別建構函式簽名：
    /// Repository(DbContext context, string? idPropertyName = null)
    /// </summary>
    /// <param name="context">
    /// MainDbContext 實例
    /// - 負責與資料庫的連線和操作
    /// - 由 DI 容器自動注入
    /// - 生命週期為 Scoped
    /// </param>
    public PromotionRepository(MainDbContext context) : base(context) {}


    /// <summary>
    /// 查詢指定促銷活動的所有規則
    /// 
    /// 職責：
    /// - 從資料庫查詢指定促銷活動 ID 的所有規則
    /// - 包含關聯的 Promotion 實體
    /// - 按建立時間排序
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
    public async Task<IEnumerable<PromotionRule>> GetRulesAsync(int promotionId)
    {
        // ========== 第一步：取得 PromotionRule 的 DbSet ==========
        // context.Set<PromotionRule>() 取得 PromotionRule 實體的 DbSet
        var ruleSet = context.Set<PromotionRule>();

        // ========== 第二步：查詢指定促銷活動的所有規則 ==========
        // ruleSet.Where(r => r.PromotionId == promotionId) 過濾出指定促銷活動的規則
        // OrderBy(r => r.CreatedAt) 按建立時間排序
        // ToListAsync() 執行查詢並轉換為 List
        var rules = await ruleSet
            .Where(r => r.PromotionId == promotionId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的規則集合
        // 如果沒有找到，會返回空集合
        return rules;
    }

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
    public async Task<PromotionRule?> GetRuleAsync(int ruleId)
    {
        // ========== 第一步：取得 PromotionRule 的 DbSet ==========
        // context.Set<PromotionRule>() 取得 PromotionRule 實體的 DbSet
        var ruleSet = context.Set<PromotionRule>();

        // ========== 第二步：根據 ID 查詢規則實體 ==========
        // ruleSet.FindAsync(ruleId) 根據主鍵查詢實體
        // 這個方法會先在記憶體中追蹤的實體中查找
        // 如果找不到，會發送 SQL 查詢到資料庫
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var rule = await ruleSet.FindAsync(ruleId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的規則實體
        // 如果找不到，回傳 null
        return rule;
    }

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
    public void AddRule(PromotionRule rule)
    {
        // ========== 第一步：取得 PromotionRule 的 DbSet ==========
        // context.Set<PromotionRule>() 取得 PromotionRule 實體的 DbSet
        var ruleSet = context.Set<PromotionRule>();

        // ========== 第二步：將規則加入追蹤 ==========
        // ruleSet.Add(rule) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        ruleSet.Add(rule);
    }

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
    public void DeleteRule(PromotionRule rule)
    {
        // ========== 第一步：取得 PromotionRule 的 DbSet ==========
        // context.Set<PromotionRule>() 取得 PromotionRule 實體的 DbSet
        var ruleSet = context.Set<PromotionRule>();

        // ========== 第二步：刪除規則實體 ==========
        // if(rule != null) 檢查實體是否存在
        // 這是一種防禦性編程，避免對 null 執行操作
        // 
        // ruleSet.Remove(rule) 將實體標記為 Deleted
        // EF Core 會追蹤這個實體的狀態為 Deleted
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 DELETE
        // 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
        if(rule != null) ruleSet.Remove(rule);
    }

    /// <summary>
    /// 查詢指定促銷活動的所有範圍
    /// 
    /// 職責：
    /// - 從資料庫查詢指定促銷活動 ID 的所有範圍
    /// - 包含關聯的 Promotion 實體
    /// - 按建立時間排序
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
    public async Task<IEnumerable<PromotionScope>> GetScopesAsync(int promotionId)
    {
        // ========== 第一步：取得 PromotionScope 的 DbSet ==========
        // context.Set<PromotionScope>() 取得 PromotionScope 實體的 DbSet
        var scopeSet = context.Set<PromotionScope>();

        // ========== 第二步：查詢指定促銷活動的所有範圍 ==========
        // scopeSet.Where(s => s.PromotionId == promotionId) 過濾出指定促銷活動的範圍
        // OrderBy(s => s.CreatedAt) 按建立時間排序
        // ToListAsync() 執行查詢並轉換為 List
        var scopes = await scopeSet
            .Where(s => s.PromotionId == promotionId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的範圍集合
        // 如果沒有找到，會返回空集合
        return scopes;
    }

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
    public async Task<PromotionScope?> GetScopeAsync(int scopeId)
    {
        // ========== 第一步：取得 PromotionScope 的 DbSet ==========
        // context.Set<PromotionScope>() 取得 PromotionScope 實體的 DbSet
        var scopeSet = context.Set<PromotionScope>();

        // ========== 第二步：根據 ID 查詢範圍實體 ==========
        // scopeSet.FindAsync(scopeId) 根據主鍵查詢實體
        // 這個方法會先在記憶體中追蹤的實體中查找
        // 如果找不到，會發送 SQL 查詢到資料庫
        // 返回值可能是 null（如果找不到對應的實體）
        // 使用 await 非同步等待查詢結果
        var scope = await scopeSet.FindAsync(scopeId);

        // ========== 第三步：回傳查詢結果 ==========
        // 回傳查詢到的範圍實體
        // 如果找不到，回傳 null
        return scope;
    }

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
    public void AddScope(PromotionScope scope)
    {
        // ========== 第一步：取得 PromotionScope 的 DbSet ==========
        // context.Set<PromotionScope>() 取得 PromotionScope 實體的 DbSet
        var scopeSet = context.Set<PromotionScope>();

        // ========== 第二步：將範圍加入追蹤 ==========
        // scopeSet.Add(scope) 將新建立的實體加入 EF Core 的變更追蹤器
        // 實體狀態會被標記為 Added，表示這是一個待新增的實體
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 INSERT
        // 這種設計允許在同一個工作單元中新增多筆資料後再一起送出
        scopeSet.Add(scope);
    }

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
    public void DeleteScope(PromotionScope scope)
    {
        // ========== 第一步：取得 PromotionScope 的 DbSet ==========
        // context.Set<PromotionScope>() 取得 PromotionScope 實體的 DbSet
        var scopeSet = context.Set<PromotionScope>();

        // ========== 第二步：刪除範圍實體 ==========
        // if(scope != null) 檢查實體是否存在
        // 這是一種防禦性編程，避免對 null 執行操作
        // 
        // scopeSet.Remove(scope) 將實體標記為 Deleted
        // EF Core 會追蹤這個實體的狀態為 Deleted
        // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync() 才會實際執行 DELETE
        // 這種設計允許在同一個工作單元中刪除多筆資料後再一起送出
        if(scope != null) scopeSet.Remove(scope);
    }
}
