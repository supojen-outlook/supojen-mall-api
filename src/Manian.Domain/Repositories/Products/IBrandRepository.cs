using Manian.Domain.Entities.Products;

namespace Manian.Domain.Repositories.Products;

/// <summary>
/// 品牌倉儲介面
/// 
/// 職責：
/// - 定義品牌實體（Brand）的資料存取契約
/// - 提供品牌的 CRUD 操作介面
/// 
/// 設計模式：
/// - 繼承泛型倉儲介面 IRepository<Brand>
/// - 遵循 Repository 模式和依賴反轉原則（DIP）
/// - 符合介面隔離原則（ISP）
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 實作在 Infrastructure 層（基礎設施層）
/// - 被 Application 層（應用層）使用
/// 
/// 使用場景：
/// - 品牌管理功能（新增、查詢、更新、刪除）
/// - 產品關聯品牌查詢
/// - 品牌層級結構查詢
/// 
/// 注意事項：
/// - 目前介面本身沒有定義任何額外方法
/// - 完全依賴 IRepository<Brand> 提供的通用操作
/// - 如需特定查詢（如按名稱搜尋），可在介面中新增方法
/// </summary>
public interface IBrandRepository : IRepository<Brand>;
