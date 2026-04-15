using Manian.Domain.Entities.Products;
using Manian.Domain.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// Location 實體的 EF Core 資料庫配置類別
/// 
/// 職責：
/// - 定義 Location 實體與資料庫表的映射關係
/// - 配置自我參照的父子關係
/// - 設定外鍵約束和刪除行為
/// 
/// 設計考量：
/// - 使用 HasOne().WithMany() 配置一對多關係
/// - 設定級聯刪除行為為 Restrict，防止誤刪
/// - 不需要額外的索引配置，因為資料量 < 1000 筆
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 IEntityTypeConfiguration<Location> 介面
/// - 在 MainDbContext.OnModelCreating 中自動套用
/// 
/// 生命週期：
/// - 由 EF Core 在模型建構時自動呼叫 Configure 方法
/// - 不需要手動實例化
/// </summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    /// <summary>
    /// 配置 Location 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        // =========================================================================
        // 配置自我參照的父子關係
        // =========================================================================
        builder.HasOne<Location>()          // Location 有一個父 Location
            .WithMany()                     // 父 Location 可以有多個子 Location
            .HasForeignKey(x => x.ParentId) // 外鍵是 Location.ParentId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除有子節點的父節點
        
        // 說明：
        // 這是一種自我參照的關係，Location 可以有 ParentId 指向另一個 Location
        // 當 ParentId 為 NULL 時，表示這是一個根區域
        // 設定為 Restrict 防止誤刪除有子區域的父區域
        
        // 設計考量：
        // 1. 使用 Restrict 而不是 Cascade，避免誤刪導致資料遺失
        // 2. 不需要額外的索引，因為資料量 < 1000 筆
        // 3. 路徑相關欄位（path_cache、level）由觸發器維護


        // =========================================================================
        // 配置與 UnitOfMeasure 的關聯
        // =========================================================================
        builder.HasOne<UnitOfMeasure>() // Location 有一個 UnitOfMeasure
        .WithMany() // UnitOfMeasure 可以對應多個 Location
        .HasForeignKey(x => x.UnitOfMeasureId); // 外鍵是 Location.UnitOfMeasureId

        // 說明：
        // 這是一個標準的外鍵關係，Location 透過 UnitOfMeasureId 關聯到 UnitOfMeasure
        // UnitOfMeasureId 為必填欄位，因為 Location 必須使用某個計量單位來管理庫存

        // 設計考量：
        // 1. 採用預設的 DeleteBehavior.ClientCascade，確保資料一致性
        // 2. 當 UnitOfMeasure 被刪除時，相關的 Location 也會被 EF Core 刪除
        // 3. 外鍵約束由資料庫自動管理，確保參照完整性
    }
}
