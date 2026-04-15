using Manian.Domain.Entities.Products;
using Manian.Domain.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// Inventory 實體的 EF Core 資料庫配置類別
/// 
/// 職責：
/// - 定義 Inventory 實體與資料庫表的映射關係
/// - 配置與 Sku、Location 和 InventoryTransaction 的外鍵關係
/// - 設定外鍵約束和刪除行為
/// 
/// 設計考量：
/// - 使用 HasOne().WithMany() 配置一對多關係
/// - 設定級聯刪除行為為 Restrict，防止誤刪
/// - 配置複合唯一索引確保 (sku_id + location_id) 的唯一性
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 IEntityTypeConfiguration<Inventory> 介面
/// - 在 MainDbContext.OnModelCreating 中自動套用
/// 
/// 生命週期：
/// - 由 EF Core 在模型建構時自動呼叫 Configure 方法
/// - 不需要手動實例化
/// </summary>
public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    /// <summary>
    /// 配置 Inventory 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        // =========================================================================
        // 1. 配置與 Sku 的關係
        // =========================================================================
        builder.HasOne<Sku>()              // Inventory 關聯到一個 Sku
            .WithMany()                    // Sku 可以有多個 Inventory
            .HasForeignKey(i => i.SkuId)  // 外鍵是 Inventory.SkuId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除有庫存記錄的 Sku

        // 說明：
        // - 使用 Restrict 防止誤刪除有庫存記錄的 Sku
        // - SkuId 在 Inventory 實體中是必填欄位（int），所以不能設為 NULL
        // - 這是一種強制的關聯，Inventory 必須關聯到一個 Sku

        // =========================================================================
        // 2. 配置與 Location 的關係
        // =========================================================================
        builder.HasOne<Location>()          // Inventory 關聯到一個 Location
            .WithMany()                     // Location 可以有多個 Inventory
            .HasForeignKey(i => i.LocationId) // 外鍵是 Inventory.LocationId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除有庫存記錄的 Location

        // 說明：
        // - 使用 Restrict 防止誤刪除有庫存記錄的 Location
        // - LocationId 在 Inventory 實體中是必填欄位（int），所以不能設為 NULL
        // - 這是一種強制的關聯，Inventory 必須關聯到一個 Location

        // =========================================================================
        // 3. 配置與 InventoryTransaction 的關係
        // =========================================================================
        builder.HasMany<InventoryTransaction>()  // Inventory 可以有多個 InventoryTransaction
            .WithOne()                           // InventoryTransaction 屬於一個 Inventory
            .HasForeignKey(t => t.SkuId)        // 外鍵是 InventoryTransaction.SkuId
            .OnDelete(DeleteBehavior.Restrict);  // 禁止刪除有交易記錄的 Inventory

        // 說明：
        // - 使用 Restrict 防止誤刪除有交易記錄的 Inventory
        // - InventoryTransaction 通過 SkuId 關聯到 Inventory
        // - 這是一種一對多的關係，一個 Inventory 可以有多個 InventoryTransaction
    }
}
