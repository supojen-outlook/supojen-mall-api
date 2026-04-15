using Manian.Domain.Entities.Products;
using Manian.Infrastructure.Persistence.ManyToMany;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// Category 實體的 EF Core 配置類別
/// 
/// 負責定義 Category 實體與資料庫的對應關係，包括：
/// 1. 類別層級結構（父子關係）
/// 2. 與 AttributeKey 的多對多關聯關係
/// 
/// 為什麼需要配置類別？
/// - 將資料庫對應邏輯從 Domain 實體中分離出來
/// - 符合 Persistence Ignorance（持久化無關）原則
/// - 讓 Domain 實體保持乾淨，只包含業務邏輯
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>
    /// 配置 Category 實體的資料庫對應
    /// </summary>
    /// <param name="builder">用於建構 EntityTypeConfiguration 的 Builder</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // =========================================================================
        // 1. 自我參照關係：Category -> Category (父子關係)
        // =========================================================================
        builder.HasOne<Category>()          // Category 有一個父類別
            .WithMany()                      // 父類別有多個子類別
            .HasForeignKey(x => x.ParentId) // 外鍵是 Category.ParentId
            .OnDelete(DeleteBehavior.Restrict); // 禁止刪除父類別當還有子類別時
        
        // 說明：
        // 這是一種自我參照的關係，Category 可以有 ParentId 指向另一個 Category
        // 當 ParentId 為 NULL 時，表示這是一個根類別
        // 設定為 Restrict 防止誤刪除有子類別的父類別

        // =========================================================================
        // 2. 多對多關係：Category <-> AttributeKey (透過 CategoryAttribute 聯結表)
        // =========================================================================
        builder.HasMany<AttributeKey>()      // Category 擁有多個 AttributeKey
            .WithMany()                      // AttributeKey 屬於多個 Category
            .UsingEntity<CategoryAttribute>( // 使用 CategoryAttribute 作為聯結實體
                
                // 左側：CategoryAttribute 關聯到 AttributeKey
                j => j.HasOne(x => x.AttributeKey)        // CategoryAttribute 有一個 AttributeKey
                    .WithMany()                           // AttributeKey 擁有多個 CategoryAttribute
                    .HasForeignKey(x => x.AttributeKeyId) // 外鍵是 CategoryAttribute.AttributeKeyId
                    .OnDelete(DeleteBehavior.Cascade),    // AttributeKey 刪除時，關聯的 CategoryAttribute 也刪除
                
                // 右側：CategoryAttribute 關聯到 Category
                j => j.HasOne<Category>()                 // CategoryAttribute 有一個 Category
                    .WithMany()                           // Category 擁有多個 CategoryAttribute
                    .HasForeignKey(x => x.CategoryId)     // 外鍵是 CategoryAttribute.CategoryId
                    .OnDelete(DeleteBehavior.Cascade),    // Category 刪除時，關聯的 CategoryAttribute 也刪除
                
                // 主鍵配置：複合主鍵 (CategoryId, AttributeKeyId)
                j => j.HasKey(x => new { x.CategoryId, x.AttributeKeyId })
            );
        
        // 說明：
        // 這種配置會產生三張表：categories、attribute_keys、category_attributes
        // category_attributes 表只有兩個欄位：category_id 和 attribute_key_id，都是外鍵也是複合主鍵
        // 這是一個純關聯表，不包含額外欄位
        // 
        // 使用場景：
        // - 定義每個類別下可以使用哪些屬性，用於商品發布時的屬性選擇
        // - 例如：手機類別關聯顏色、尺寸、記憶體等屬性
        // - 複合主鍵確保同一類別不會重複關聯同一屬性
    }
}
