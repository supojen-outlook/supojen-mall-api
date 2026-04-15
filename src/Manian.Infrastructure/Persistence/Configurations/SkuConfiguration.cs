using Manian.Domain.Entities.Products;
using Manian.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// Sku 實體的 EF Core 資料庫配置類別
/// 
/// 職責：
/// - 定義 Sku 實體與資料庫表的映射關係
/// - 配置 Specs 屬性與 JSONB 的轉換
/// - 設定外鍵約束和刪除行為
/// - 配置與 AttributeValue 的多對多關聯關係
/// 
/// 設計考量：
/// - 使用 HasConversion 處理 List<Specification> 與 JSONB 的轉換
/// - 設定級聯刪除行為為 Cascade，刪除商品時一併刪除 SKU
/// - 使用 CamelCase 命名策略保持 JSON 格式一致性
/// - 多對多關係配置與 ProductConfiguration 保持一致
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 IEntityTypeConfiguration<Sku> 介面
/// - 在 MainDbContext.OnModelCreating 中自動套用
/// 
/// 生命週期：
/// - 由 EF Core 在模型建構時自動呼叫 Configure 方法
/// - 不需要手動實例化
/// </summary>
public class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    /// <summary>
    /// 配置 Sku 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        // =========================================================================
        // 1. 配置 Specs 屬性：List<Specification> -> JSONB
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
            .HasColumnName("specs")
            
            // 指定資料庫欄位類型為 JSONB
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
        // 2. 配置與 Product 的關係
        // =========================================================================
        builder.HasOne<Sku>()              // Sku 屬於一個 Product
            .WithMany()                    // Product 可以有多個 Sku
            .HasForeignKey(s => s.ProductId) // 外鍵是 Sku.ProductId
            .OnDelete(DeleteBehavior.Cascade); // Product 刪除時，Sku 也刪除

        // 說明：
        // - 使用 Cascade 確保刪除商品時一併刪除所有 SKU
        // - 這符合業務邏輯：商品不存在時，其 SKU 也應該被刪除
        // - ProductId 在 Sku 實體中是必填欄位（int），所以不能設為 NULL

        // =========================================================================
        // 3. 配置與 UnitOfMeasure 的關係
        // =========================================================================
        builder.HasOne<Sku>()              // Sku 有一個 UnitOfMeasure
            .WithMany()                    // UnitOfMeasure 可以對應多個 Sku
            .HasForeignKey(s => s.UnitOfMeasureId) // 外鍵是 Sku.UnitOfMeasuresId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除有 SKU 使用的單位

        // 說明：
        // - 使用 Restrict 防止誤刪除有 SKU 使用的計量單位
        // - UnitOfMeasuresId 在 Sku 實體中是可空欄位（int?），所以可以設為 NULL
        // - 這是一種可選的關聯，SKU 可以不指定計量單位

        // =========================================================================
        // 4. 多對多關係：Sku <-> AttributeValue (透過 SkuAttribute 聯結表)
        // =========================================================================
        builder.HasMany<AttributeValue>()     // Sku 擁有多個 AttributeValue
            .WithMany()                       // AttributeValue 屬於多個 Sku
            .UsingEntity<SkuAttribute>(       // 使用 SkuAttribute 作為聯結實體
                
                // 左側：SkuAttribute 關聯到 AttributeValue
                j => j.HasOne<AttributeValue>()          // SkuAttribute 有一個 AttributeValue
                    .WithMany()                           // AttributeValue 擁有多個 SkuAttribute
                    .HasForeignKey(x => x.AttributeValueId) // 外鍵是 SkuAttribute.AttributeValueId
                    .OnDelete(DeleteBehavior.Cascade),    // AttributeValue 刪除時，關聯的 SkuAttribute 也刪除
                
                // 右側：SkuAttribute 關聯到 Sku
                j => j.HasOne<Sku>()                       // SkuAttribute 有一個 Sku
                    .WithMany()                           // Sku 擁有多個 SkuAttribute
                    .HasForeignKey(x => x.SkuId)          // 外鍵是 SkuAttribute.SkuId
                    .OnDelete(DeleteBehavior.Cascade),    // Sku 刪除時，關聯的 SkuAttribute 也刪除
                
                // 主鍵配置：複合主鍵 (SkuId, AttributeValueId)
                j => j.HasKey(x => new { x.SkuId, x.AttributeValueId })
            );
        
        // 說明：
        // 這種配置會產生三張表：skus、attribute_values、sku_attributes
        // sku_attributes 表只有三個欄位：sku_id、attribute_value_id、created_at
        // sku_id 和 attribute_value_id 都是外鍵，共同組成複合主鍵
        // 
        // 使用場景：
        // - 定義每個 SKU 擁有哪些銷售屬性組合（如顏色、尺寸的具體值）
        // - 庫存管理（按屬性值統計庫存）
        // - 商品篩選功能（如篩選「紅色」的商品）
        // 
        // 注意事項：
        // - 銷售屬性（用於 SKU 的顏色、尺寸等）使用此實體關聯
        // - 非銷售屬性（材質、產地、風格等）應使用 ProductAttribute 關聯
        // - 與 Sku.Specs JSON 欄位互補，此表確保資料正規化
        // - 複合主鍵確保同一 SKU 不會重複關聯同一屬性值
        // - 級聯刪除確保資料一致性
    }
}
