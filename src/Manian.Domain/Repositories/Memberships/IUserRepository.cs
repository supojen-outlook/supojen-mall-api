using System;
using Manian.Domain.Entities.Memberships;

namespace Manian.Domain.Repositories.Memberships;

public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根據電子郵件獲取用戶
    /// </summary>
    /// <param name="email">用戶 Email</param>
    /// <returns></returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// 查詢指定用戶的點數交易記錄
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="func">查詢條件</param>
    /// <returns>該用戶的點數交易記錄集合</returns>
    Task<IEnumerable<PointTransaction>> GetPointTransactionsAsync(int userId, Func<IQueryable<PointTransaction>, IQueryable<PointTransaction>> func);

    /// <summary>
    /// 新增點數交易記錄到用戶
    /// 
    /// 職責：
    /// - 將點數交易實體加入資料庫追蹤
    /// - 設定點數交易與用戶的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保點數交易的 UserId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 訂單完成後贈送點數
    /// - 退款時扣除點數
    /// - 促銷活動贈送點數
    /// - 手動調整用戶點數
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證 UserId 是否正確
    /// - 不支援更新點數交易（應使用 Repository 的 Update 方法）
    /// </summary>
    /// <param name="pointTransaction">要新增的點數交易實體</param>
    void AddPointTransaction(PointTransaction pointTransaction);

    /// <summary>
    /// 根據 ID 查詢單一身份認證資訊
    /// 
    /// 職責：
    /// - 從資料庫查詢指定 ID 的身份認證資訊
    /// - 包含關聯的 User 實體
    /// 
    /// 設計考量：
    /// - 使用 FindAsync 方法提高查詢效率
    /// - 包含 User 實體以減少後續查詢
    /// - 使用非同步操作避免阻塞執行緒
    /// 
    /// 使用場景：
    /// - 身份認證資訊詳情頁顯示
    /// - 身份認證資訊編輯表單
    /// - 身份認證資訊確認
    /// 
    /// 注意事項：
    /// - 如果身份認證資訊不存在，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// </summary>
    /// <param name="id">身份認證資訊 ID</param>
    /// <returns>查詢到的身份認證資訊實體，若不存在則回傳 null</returns>
    Task<Identity?> GetIdentityAsync(int id);

    /// <summary>
    /// 查詢指定用戶的身份認證資訊
    /// 
    /// 職責：
    /// - 從資料庫查詢指定用戶的所有身份認證資訊
    /// - 支援自訂查詢條件（分頁、排序、篩選等）
    /// 
    /// 設計考量：
    /// - 使用非同步操作避免阻塞執行緒
    /// - 支援自訂查詢邏輯（透過 func 參數）
    /// - 預設按 Provider 排序
    /// 
    /// 使用場景：
    /// - 用戶管理頁面查看用戶的登入方式
    /// - 用戶資料編輯表單
    /// - 用戶身份認證資訊確認
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="func">查詢條件，用於分頁、排序、篩選等</param>
    /// <returns>該用戶的身份認證資訊集合</returns>
    Task<IEnumerable<Identity>> GetIdentitiesAsync(int userId, Func<IQueryable<Identity>, IQueryable<Identity>> func);

    /// <summary>
    /// 新增身份認證資訊到用戶
    /// 
    /// 職責：
    /// - 將身份認證實體加入資料庫追蹤
    /// - 設定身份認證與用戶的關聯
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 確保身份認證的 UserId 已正確設定
    /// - 使用 EF Core 的變更追蹤機制
    /// 
    /// 使用場景：
    /// - 用戶使用第三方登入（Google、Line、Microsoft、Facebook）
    /// - 用戶綁定新的登入方式
    /// - 系統遷移用戶資料
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 建議在新增前驗證 UserId 是否正確
    /// - 不支援更新身份認證（應使用 Repository 的 Update 方法）
    /// 
    /// 資料約束：
    /// - (UserId + Provider + ProviderUid) 必須唯一（複合唯一索引）
    /// - Provider 必須是有效的認證廠商（google、line、microsoft、facebook）
    /// </summary>
    /// <param name="identity">要新增的身份認證實體</param>
    void AddIdentity(Identity identity);

    /// <summary>
    /// 刪除身份認證資訊
    /// 
    /// 職責：
    /// - 將身份認證實體標記為待刪除
    /// - 確保資料庫約束不會被違反
    /// 
    /// 設計考量：
    /// - 不立即寫入資料庫，由 SaveChangeAsync 統一處理
    /// - 使用 EF Core 的變更追蹤機制
    /// - 檢查實體是否存在於資料庫中
    /// 
    /// 使用場景：
    /// - 用戶解除綁定第三方登入
    /// - 用戶切換登入方式
    /// - 系統清理過期的身份認證資訊
    /// 
    /// 注意事項：
    /// - 必須在呼叫後執行 SaveChangeAsync 才會寫入資料庫
    /// - 如果用戶只有一種登入方式，不應刪除
    /// - 建議在刪除前檢查用戶是否還有其他登入方式
    /// 
    /// 資料約束：
    /// - 如果用戶沒有其他登入方式，刪除會導致用戶無法登入
    /// - 建議在 UI 層加入確認對話框
    /// </summary>
    /// <param name="identity">要刪除的身份認證實體</param>
    void DeleteIdentity(Identity identity);
}
