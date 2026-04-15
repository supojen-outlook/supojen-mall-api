using Manian.Domain.Entities.Products;
using Manian.Domain.ValueObjects;
using Manian.Infrastructure.Persistence.ManyToMany;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// Product 實體的 EF Core 資料庫配置類別
/// 
/// 職責：
/// - 定義 Product 實體與資料庫表的映射關係
/// - 配置 Specs 屬性與 JSONB 的轉換
/// - 設定外鍵約束和刪除行為
/// - 配置與 AttributeValue 的多對多關聯關係
/// 
/// 設計考量：
/// - 使用 HasConversion 處理 List<Specification> 與 JSONB 的轉換
/// - 設定級聯刪除行為為 SetNull，防止誤刪
/// - 使用 CamelCase 命名策略保持 JSON 格式一致性
/// - 多對多關係配置與 CategoryConfiguration 保持一致
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 IEntityTypeConfiguration<Product> 介面
/// - 在 MainDbContext.OnModelCreating 中自動套用
/// 
/// 生命週期：
/// - 由 EF Core 在模型建構時自動呼叫 Configure 方法
/// - 不需要手動實例化
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>
    /// 配置 Product 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // =========================================================================
        // 1. 配置 Specs 屬性：ICollection<Specification> -> JSONB
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
        
        // 配置 Specs 屬性的映射
        builder.Property(x => x.Specs)
            // 指定資料庫欄位名稱為 "specs"
            // 注意：這裡使用小寫，符合 PostgreSQL 的命名慣例
            .HasColumnName("specs")
            
            // 指定資料庫欄位類型為 JSONB
            // JSONB 是 PostgreSQL 的二進制 JSON 類型，支援索引和查詢
            .HasColumnType("jsonb")
            
            // 配置值轉換器
            .HasConversion(
                // 寫入資料庫時：將 ICollection<Specification> 序列化為 JSON 字串
                v => JsonSerializer.Serialize(v, jsonOptions),
                
                // 從資料庫讀取時：將 JSON 字串反序列化為 List<Specification>
                // 如果 JSON 為 null 或空字串，則返回空集合
                v => JsonSerializer.Deserialize<List<Specification>>(v, jsonOptions) ?? new List<Specification>()
            );

        // =========================================================================
        // 2. 配置與 Category 的關係
        // =========================================================================
        builder.HasOne<Product>()           // Product 有一個 Category
            .WithMany()                     // Category 可以對應多個 Product
            .HasForeignKey(p => p.CategoryId) // 外鍵是 Product.CategoryId
            .OnDelete(DeleteBehavior.SetNull); // Category 刪除時，Product.CategoryId 設為 NULL

        // 說明：
        // - 使用 SetNull 而非 Cascade，避免誤刪 Category 時連帶刪除所有 Product
        // - CategoryId 在 Product 實體中是可空的（int?），所以可以設為 NULL
        // - 這是一種可選的關聯，Product 可以不屬於任何 Category

        // =========================================================================
        // 3. 配置與 Brand 的關係
        // =========================================================================
        builder.HasOne<Product>()             // Product 有一個 Brand
            .WithMany()                       // Brand 可以對應多個 Product
            .HasForeignKey(p => p.BrandId)    // 外鍵是 Product.BrandId
            .OnDelete(DeleteBehavior.SetNull); // Brand 刪除時，Product.BrandId 設為 NULL

        // 說明：
        // - 與 Category 類似，使用 SetNull 避免級聯刪除
        // - BrandId 在 Product 實體中是可空的（int?），所以可以設為 NULL
        // - 這是一種可選的關聯，Product 可以不屬於任何 Brand

        // =========================================================================
        // 4. 多對多關係：Product <-> AttributeValue (透過 ProductAttribute 聯結表)
        // =========================================================================
        builder.HasMany<AttributeValue>()     // Product 擁有多個 AttributeValue
            .WithMany()                       // AttributeValue 屬於多個 Product
            .UsingEntity<ProductAttribute>(   // 使用 ProductAttribute 作為聯結實體
                
                // 左側：ProductAttribute 關聯到 AttributeValue
                j => j.HasOne<AttributeValue>()          // ProductAttribute 有一個 AttributeValue
                    .WithMany()                           // AttributeValue 擁有多個 ProductAttribute
                    .HasForeignKey(x => x.AttributeValueId) // 外鍵是 ProductAttribute.AttributeValueId
                    .OnDelete(DeleteBehavior.Cascade),    // AttributeValue 刪除時，關聯的 ProductAttribute 也刪除
                
                // 右側：ProductAttribute 關聯到 Product
                j => j.HasOne<Product>()                   // ProductAttribute 有一個 Product
                    .WithMany()                           // Product 擁有多個 ProductAttribute
                    .HasForeignKey(x => x.ProductId)      // 外鍵是 ProductAttribute.ProductId
                    .OnDelete(DeleteBehavior.Cascade),    // Product 刪除時，關聯的 ProductAttribute 也刪除
                
                // 主鍵配置：複合主鍵 (ProductId, AttributeValueId)
                j => j.HasKey(x => new { x.ProductId, x.AttributeValueId })
            );
        
        // 說明：
        // 這種配置會產生三張表：products、attribute_values、product_attributes
        // product_attributes 表只有三個欄位：product_id、attribute_value_id、created_at
        // product_id 和 attribute_value_id 都是外鍵，共同組成複合主鍵
        // 
        // 使用場景：
        // - 定義每個商品擁有哪些非銷售屬性（如材質、產地、風格等）
        // - 商品篩選功能（如篩選「棉質」材質的商品）
        // - 商品比較功能
        // 
        // 注意事項：
        // - 銷售屬性（用於 SKU 的顏色、尺寸等）應放在 Sku.Specs 欄位中
        // - 複合主鍵確保同一商品不會重複關聯同一屬性值
        // - 級聯刪除確保資料一致性
    }
}
