using System;
using Manian.Domain.Entities.Assets;

namespace Manian.Domain.Repositories.Assets;

public interface IAssetRepository : IRepository<Asset>
{
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
    Task<Asset?> GetByUrlAsync(string url);

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
    Task<IEnumerable<Asset>> GetByUrlsAsync(IEnumerable<string> urls);
}
