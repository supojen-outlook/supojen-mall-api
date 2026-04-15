using Manian.Domain.Entities.Assets;
using Manian.Domain.Repositories.Assets;
using Manian.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Assets;

/// <summary>
/// 資產倉儲實作類別
/// 
/// 職責：
/// - 處理 Asset 實體的資料存取層 (DAL) 邏輯
/// - 封裝對 MainDbContext 中 Assets DbSet 的操作
/// - 繼承自泛型 Repository<Asset> 以獲得通用的 CRUD 功能
/// 
/// 設計模式：
/// - 繼承 Repository 模式
/// - 實作 IAssetRepository 介面 (依賴反轉原則)
/// - 使用 Entity Framework Core 作為 ORM
/// </summary>
public class AssetRepository : Repository<Asset>, IAssetRepository
{
    /// <summary>
    /// 建構函式
    /// 
    /// 用途：
    /// - 初始化倉儲實例並注入資料庫上下文
    /// 
    /// 參數說明：
    /// - context：EF Core 的資料庫上下文，包含資料庫連線與實體映射
    /// 
    /// 設計考量：
    /// - 將 context 傳遞給基底類別 Repository 的建構函式
    /// - 基底類別通常會將 context 賦值給受保護的欄位供子類使用
    /// </summary>
    /// <param name="context">資料庫上下文</param>
    public AssetRepository(MainDbContext context) : base(context) {}

    /// <summary>
    /// 根據 URL 查詢資產
    /// 
    /// 用途：
    /// - 透過完整的公開 URL 反查資產實體
    /// - 用於驗證 URL 是否屬於系統內的資源
    /// - 用於更新資產時的定位 (AssetUpdateCommand)
    /// 
    /// 執行流程：
    /// 1. 從基底類別獲取 DbSet (dbSet) 並轉換為 IQueryable
    /// 2. 應用篩選條件 (Url == url)
    /// 3. 執行非同步查詢並回傳第一筆結果
    /// 
    /// 查詢特性：
    /// - 使用 FirstOrDefaultAsync，若找不到則回傳 null (因為回傳型別是 Asset?)
    /// - 注意：若回傳型別為非可空 Asset，FirstAsync 在找不到時會拋出例外
    /// 
    /// 索引使用：
    /// - 此查詢會利用資料庫索引 idx_assets_url 進行查找
    /// - 確保 URL 欄位有建立索引以獲得最佳效能
    /// 
    /// 錯誤處理：
    /// - 若 URL 為 null 或空字串，可能導致查詢異常或無結果
    /// - 若資料庫中有多筆相同的 URL (違反唯一性)，只會回傳第一筆
    /// </summary>
    /// <param name="url">資產的公開訪問 URL</param>
    /// <returns>符合條件的資產實體，若不存在則為 null</returns>
    public async Task<Asset?> GetByUrlAsync(string url)
    {
        // ========== 第一步：獲取查詢物件 ==========
        // dbSet：繼承自基底類別，代表 MainDbContext 中的 Assets DbSet
        // AsQueryable()：將 DbSet 轉換為 IQueryable，以便後續使用 LINQ 擴充方法
        var query = dbSet.AsQueryable();
        
        // ========== 第二步：執行查詢 ==========
        // FirstAsync：執行查詢並回傳序列中的第一個元素
        // x => x.Url == url：Lambda 運算式，指定查詢條件為 Url 欄位等於參數 url
        return await query.FirstAsync(x => x.Url == url);
    }

    /// <summary>
    /// 根據 URL 集合查詢資產
    /// 
    /// 用途：
    /// - 透過多個 URL 反查資產實體集合
    /// - 用於批量處理資產時的定位 (AssetBatchCommand)
    /// 
    /// 執行流程：
    /// 1. 從基底類別獲取 DbSet (dbSet) 並轉換為 IQueryable
    /// 2. 應用篩選條件 (urls.Contains(x.Url))
    /// 3. 執行非同步查詢並回傳結果集合
    /// 
    /// 查詢特性：
    /// - 使用 ToListAsync，將查詢結果轉換為非同步 Task<List<Asset>>
    /// 
    /// 索引使用：
    /// - 此查詢會利用資料庫索引 idx_assets_url 進行查找
    /// - 確保 URL 欄位有建立索引以獲得最佳效能
    /// 
    /// 錯誤處理：
    /// - 若 urls 為 null 或空集合，可能導致查詢異常或無結果
    /// - 若資料庫中有多筆相同的 URL (違反唯一性)，只會回傳第一筆
    /// </summary>
    public async Task<IEnumerable<Asset>> GetByUrlsAsync(IEnumerable<string> urls)
    {
        // ========== 第一步：獲取查詢物件 ==========
        // dbSet：繼承自基底類別，代表 MainDbContext 中的 Assets DbSet
        // AsQueryable()：將 DbSet 轉換為 IQueryable，以便後續使用 LINQ 擴充方法
        var query = dbSet.AsQueryable();

        // ========== 第二步：執行查詢 ==========
        // Where：篩選出 Url 欄位在 urls 集合中的資產
        // ToListAsync：將查詢結果轉換為非同步 Task<List<Asset>>
        return await query.Where(x => urls.Contains(x.Url)).ToListAsync();
    }
}
