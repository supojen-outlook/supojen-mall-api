using Manian.Domain.Entities.Orders;
using Manian.Domain.ValueObjects.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// Order 實體的 EF Core 資料庫配置類別
/// 
/// 職責：
/// - 定義 Order 實體與資料庫表的映射關係
/// - 配置與 OrderItem、Payment、Shipment、Return 的關聯關係
/// - 設定外鍵約束和刪除行為
/// - 配置 Snapshot 屬性與 JSONB 的轉換
/// 
/// 設計考量：
/// - 使用 HasOne().WithMany() 配置一對多關係
/// - 設定級聯刪除行為，確保資料一致性
/// - 使用 CamelCase 命名策略保持 JSON 格式一致性
/// - 與其他 Configuration 類別保持一致的設計風格
/// 
/// 架構位置：
/// - 位於 Infrastructure 層（基礎設施層）
/// - 實作 IEntityTypeConfiguration<Order> 介面
/// - 在 MainDbContext.OnModelCreating 中自動套用
/// 
/// 生命週期：
/// - 由 EF Core 在模型建構時自動呼叫 Configure 方法
/// - 不需要手動實例化
/// 
/// 參考實作：
/// - ProductConfiguration：JSONB 轉換配置
/// - SkuConfiguration：外鍵約束配置
/// - UserConfiguration：一對多關係配置
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>
    /// 配置 Order 實體的資料庫映射
    /// </summary>
    /// <param name="builder">用於配置實體類型的建構器</param>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // =========================================================================
        // 1. 配置 Snapshot 屬性：JSONB
        // =========================================================================
        
        // 建立 JSON 序列化選項
        var jsonOptions = new JsonSerializerOptions
        {
            // 使用 CamelCase 命名策略，確保 JSON 屬性名稱符合 JavaScript 慣例
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            
            // 不縮排，減少 JSON 字串長度，節省儲存空間
            WriteIndented = false
        };
        
        // 配置 Snapshot 屬性的映射
        builder.Property(x => x.Snapshot)
            // 指定資料庫欄位名稱為 "snapshot"
            .HasColumnName("snapshot")
            
            // 指定資料庫欄位類型為 JSONB
            .HasColumnType("jsonb")
            
            // 配置值轉換器
            .HasConversion(
                // 寫入資料庫時：將物件序列化為 JSON 字串
                v => v != null ? JsonSerializer.Serialize(v, jsonOptions) : null,
                
                // 從資料庫讀取時：將 JSON 字串反序列化為 List<OrderSnapshot>
                // 如果 JSON 為 null 或空字串，則返回空集合
                v => JsonSerializer.Deserialize<OrderSnapshot>(v ?? "", jsonOptions) ?? new OrderSnapshot()
            );

        // =========================================================================
        // 2. 配置與 OrderItem 的關係（一對多）
        // =========================================================================
        builder.HasMany<OrderItem>()              // Order 可以有多個 OrderItem
            .WithOne()                        // OrderItem 屬於一個 Order
            .HasForeignKey(oi => oi.OrderId)  // 外鍵是 OrderItem.OrderId
            .OnDelete(DeleteBehavior.Cascade); // Order 刪除時，OrderItem 也刪除

        // 說明：
        // - 使用 Cascade 確保刪除訂單時一併刪除所有訂單項目
        // - 這符合業務邏輯：訂單不存在時，其項目也應該被刪除
        // - OrderId 在 OrderItem 實體中是必填欄位（int），所以不能設為 NULL

        // =========================================================================
        // 3. 配置與 Payment 的關係（一對一）
        // =========================================================================
        builder.HasOne<Payment>()                   // Order 有一個 Payment
            .WithOne()                              // Payment 屬於一個 Order
            .HasForeignKey<Payment>(p => p.OrderId) // 外鍵是 Payment.OrderId
            .OnDelete(DeleteBehavior.Cascade);      // Order 刪除時，Payment 也刪除

        // 說明：
        // - 使用 Cascade 確保刪除訂單時一併刪除付款記錄
        // - 這符合業務邏輯：訂單不存在時，其付款記錄也應該被刪除
        // - OrderId 在 Payment 實體中是必填欄位（int），所以不能設為 NULL

        // =========================================================================
        // 4. 配置與 Shipment 的關係
        // =========================================================================
        builder.HasOne<Shipment>()                   // Order 有一個 Shipment
            .WithOne()                               // Shipment 屬於一個 Order
            .HasForeignKey<Shipment>(s => s.OrderId) // 外鍵是 Shipment.OrderId
            .OnDelete(DeleteBehavior.Cascade);       // Order 刪除時，Shipment 也刪除


        // =========================================================================
        // 5. 配置與 Return 的關係（透過 OrderItem）
        // =========================================================================
        // 注意：Return 直接關聯到 OrderItem，而不是 Order
        // 這種配置是透過 OrderItem 的級聯刪除實現的
        // 當 Order 刪除時 → OrderItem 刪除 → Return 刪除
    }
}
