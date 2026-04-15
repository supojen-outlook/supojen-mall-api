using Manian.Domain.Entities.Carts;
using Manian.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// CartItem 實體的 EF Core 資料庫配置類別
/// 
/// 職責：
/// - 定義 CartItem 實體與資料庫表的映射關係
/// - 配置 SkuAttributes 屬性與 JSONB 的轉換
/// - 設定外鍵約束和刪除行為
/// - 配置與 User、Product、Sku 的關聯關係
/// 
/// 設計考量：
/// - 使用 HasConversion 處理 List<Specification> 與 JSONB 的轉換
/// - 設定級聯刪除行為，確保資料一致性
/// - 使用 CamelCase 命名策略保持 JSON 格式一致性
/// - 與 ProductConfiguration、SkuConfiguration 保持一致
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 IEntityTypeConfiguration<CartItem> 介面
/// - 在 MainDbContext.OnModelCreating 中自動套用
/// 
/// 生命週期：
/// - 由 EF Core 在模型建構時自動呼叫 Configure 方法
/// - 不需要手動實例化
/// 
/// 參考實作：
/// - ProductConfiguration：JSONB 轉換配置
/// - SkuConfiguration：JSONB 轉換配置
/// - CategoryConfiguration：外鍵約束配置
/// </summary>
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    /// <summary>
    /// 配置 CartItem 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        // =========================================================================
        // 1. 配置 SkuAttributes 屬性：List<Specification> -> JSONB
        // =========================================================================
        
        // 建立 JSON 序列化選項
        var jsonOptions = new JsonSerializerOptions
        {
            // 使用 CamelCase 命名策略，確保 JSON 屬性名稱符合 JavaScript 慣例
            // 例如：KeyId 轉換為 keyId
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            
            // 不縮排，減少 JSON 字串長度，節省儲存空間
            WriteIndented = false
        };
        
        // 配置 SkuAttributes 屬性的映射
        builder.Property(x => x.SkuAttributes)
            // 指定資料庫欄位名稱為 "sku_attributes"
            // 注意：這裡使用小寫，符合 PostgreSQL 的命名慣例
            .HasColumnName("sku_attributes")
            
            // 指定資料庫欄位類型為 JSONB
            // JSONB 是 PostgreSQL 的二進制 JSON 類型，支援索引和查詢
            .HasColumnType("jsonb")
            
            // 配置值轉換器
            .HasConversion(
                // 寫入資料庫時：將 List<Specification> 序列化為 JSON 字串
                v => JsonSerializer.Serialize(v, jsonOptions),
                
                // 從資料庫讀取時：將 JSON 字串反序列化為 List<Specification>
                // 如果 JSON 為 null 或空字串，則返回空集合
                v => JsonSerializer.Deserialize<List<Specification>>(v, jsonOptions) ?? new List<Specification>()
            );

        // =========================================================================
        // 2. 配置與 User 的關係
        // =========================================================================
        builder.HasOne<CartItem>()          // CartItem 屬於一個 User
            .WithMany()                     // User 可以有多個 CartItem
            .HasForeignKey(x => x.UserId)   // 外鍵是 CartItem.UserId
            .OnDelete(DeleteBehavior.Cascade); // User 刪除時，CartItem 也刪除

        // 說明：
        // - 使用 Cascade 確保刪除使用者時一併刪除購物車項目
        // - 這符合業務邏輯：使用者不存在時，其購物車項目也應該被刪除
        // - UserId 在 CartItem 實體中是必填欄位（int），所以不能設為 NULL

        // =========================================================================
        // 3. 配置與 Product 的關係
        // =========================================================================
        builder.HasOne<CartItem>()          // CartItem 屬於一個 Product
            .WithMany()                     // Product 可以有多個 CartItem
            .HasForeignKey(x => x.ProductId) // 外鍵是 CartItem.ProductId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除有購物車項目的商品

        // 說明：
        // - 使用 Restrict 防止誤刪除有購物車項目的商品
        // - ProductId 在 CartItem 實體中是必填欄位（int），所以不能設為 NULL
        // - 如果需要刪除商品，應先處理相關的購物車項目

        // =========================================================================
        // 4. 配置與 Sku 的關係
        // =========================================================================
        builder.HasOne<CartItem>()          // CartItem 屬於一個 Sku
            .WithMany()                     // Sku 可以有多個 CartItem
            .HasForeignKey(x => x.SkuId)   // 外鍵是 CartItem.SkuId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除有購物車項目的 SKU

        // 說明：
        // - 使用 Restrict 防止誤刪除有購物車項目的 SKU
        // - SkuId 在 CartItem 實體中是必填欄位（int），所以不能設為 NULL
        // - 如果需要刪除 SKU，應先處理相關的購物車項目
    }
}
