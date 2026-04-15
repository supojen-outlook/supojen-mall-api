using Manian.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// AttributeKey 實體的 EF Core 資料庫配置類別
/// 實作 IEntityTypeConfiguration<AttributeKey> 介面以定義實體與資料庫表的映射關係
/// </summary>
public class AttributeKeyConfiguration : IEntityTypeConfiguration<AttributeKey>
{
    /// <summary>
    /// 配置 AttributeKey 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<AttributeKey> builder)
    {
        // 配置 AttributeKey 與 AttributeValue 之間的一對多關係
        // 一個 AttributeKey (HasMany) 可以包含多個 AttributeValue
        // 這些 AttributeValue 不需要導航屬性指向回 AttributeKey (WithOne)
        // 關聯透過 AttributeValue 實體中的 AttributeId 外鍵屬性來建立 (HasForeignKey)
        builder.HasMany<AttributeValue>()
               .WithOne()
               .HasForeignKey(x => x.AttributeId); 
    }
}
