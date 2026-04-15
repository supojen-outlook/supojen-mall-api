using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Products;

/// <summary>
/// 品牌倉儲實作類別
/// 
/// 職責：
/// - 實作 IBrandRepository 介面
/// - 處理 Brand 實體的所有資料庫操作
/// - 繼承泛型 Repository<Brand> 獲得通用 CRUD 功能
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 繼承模式：複用泛型 Repository 的通用功能
/// - 依賴注入：透過建構函式注入 MainDbContext
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 Domain 層的 IBrandRepository 介面
/// - 使用 MainDbContext 與資料庫互動
/// 
/// 生命週期：
/// - 註冊為 Scoped（見 Infrastructure/DI.cs）
/// - 每個 HTTP 請求一個實例
/// 
/// 設計特點：
/// - 目前介面沒有定義額外方法
/// - 完全依賴父類別 Repository<Brand> 提供的功能
/// - 未來如需特定查詢，可在介面中新增方法並在此實作
/// 
/// 參考實作：
/// - CategoryRepository：展示了如何新增特定方法（GetAttributeKeysAsync、UpdateAttributeKeysAsync）
/// - UnitOfMeasureRepository：與此類別類似的簡潔實作
/// </summary>
public class BrandRepository : Repository<Brand>, IBrandRepository
{
    /// <summary>
    /// 建構函式
    /// 
    /// 職責：
    /// - 初始化倉儲實例
    /// - 注入資料庫上下文
    /// - 傳遞給父類別 Repository<Brand>
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
    public BrandRepository(MainDbContext context) : base(context) {}
}
