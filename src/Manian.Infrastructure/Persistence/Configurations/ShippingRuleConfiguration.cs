// 路徑: src/Manian.Infrastructure/Persistence/Configurations/ShippingRuleConfiguration.cs

using System.Text.Json;
using Manian.Domain.Entities.Orders;
using Manian.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ShippingRuleConfiguration : IEntityTypeConfiguration<ShippingRule>
{
    public void Configure(EntityTypeBuilder<ShippingRule> builder)
    {
        // =========================================================================
        // 1. 配置 Condition 屬性：ShippingRuleCondition -> JSONB
        // =========================================================================
        
        // 建立 JSON 序列化選項
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        // 配置 Condition 屬性的映射
        builder.Property(x => x.Condition)
            .HasColumnName("conditions")
            .HasColumnType("jsonb")
            .HasConversion(
                // 寫入資料庫時：根據實際類型序列化
                v => v != null ? JsonSerializer.Serialize(v, v.GetType(), jsonOptions) : null,
                
                // 從資料庫讀取時：根據 RuleType 決定反序列化類型
                v => v != null ? DeserializeCondition(v, jsonOptions) : null
            );
    }

    /// <summary>
    /// 根據 RuleType 反序列化條件
    /// </summary>
    private ShippingRuleCondition? DeserializeCondition(string json, JsonSerializerOptions options)
    {
        // 使用 JsonDocument 解析 JSON，避免完整反序列化
        using var document = JsonDocument.Parse(json);
        
        // 嘗試從 JSON 中讀取 ruleType（如果存在）
        // 注意：這裡我們假設 JSON 中可能包含 ruleType 欄位
        // 如果沒有，我們需要從父實體的 RuleType 獲取
        if (document.RootElement.TryGetProperty("ruleType", out var ruleTypeElement))
        {
            var ruleType = ruleTypeElement.GetString();
            return DeserializeByType(json, ruleType, options);
        }
        
        // 如果 JSON 中沒有 ruleType，返回 null
        // 在實際使用中，我們應該從 ShippingRule.RuleType 獲取
        return null;
    }

    /// <summary>
    /// 根據規則類型反序列化
    /// </summary>
    private ShippingRuleCondition? DeserializeByType(string json, string? ruleType, JsonSerializerOptions options)
    {
        return ruleType switch
        {
            "quantity" => JsonSerializer.Deserialize<QuantityShippingCondition>(json, options),
            "amount" => JsonSerializer.Deserialize<AmountShippingCondition>(json, options),
            _ => null
        };
    }
}
