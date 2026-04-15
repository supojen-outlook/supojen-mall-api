using System.Reflection;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xiao.Domain.Entities;

namespace Manian.Infrastructure.Persistence;

public static class Extensions
{
    /// <summary>
    /// 自動註冊所有實作 IEntity 介面的領域實體到 EF Core
    /// 
    /// 這個方法會掃描指定組件（預設為 IEntity 所在的組件），
    /// 找出所有實作 IEntity 的具體類別，並自動呼叫 modelBuilder.Entity<T>()
    /// 將它們註冊到 EF Core 的模型中。
    /// 
    /// 為什麼需要這個方法？
    /// 1. 避免手動一個個註冊實體（DRY 原則）
    /// 2. 確保新加的實體不會被遺漏
    /// 3. 統一管理實體註冊邏輯
    /// 
    /// 使用時機：
    /// 在 DbContext 的 OnModelCreating 中呼叫此方法，
    /// 然後再套用具體的 IEntityTypeConfiguration 配置。
    /// </summary>
    /// <param name="modelBuilder">EF Core 的 ModelBuilder 實例</param>
    /// <param name="assembly">要掃描的組件，若不指定則預設使用 IEntity 所在的組件</param>
    /// <returns>原 ModelBuilder 實例，支援鏈式呼叫</returns>
    public static ModelBuilder RegisterAllEntities(
        this ModelBuilder modelBuilder, 
        Assembly? assembly = null)
    {
        // 1. 決定要掃描哪個組件
        //    如果呼叫端有傳入 assembly，就用傳入的
        //    如果沒有傳入，就用 IEntity 所在的組件（最常見的 Domain 組件）
        assembly ??= typeof(IEntity).Assembly;
        
        // 2. 找出組件中所有符合條件的類型：
        //    - 是類別（不是介面、列舉、結構）
        //    - 不是抽象類別（不能是 abstract）
        //    - 有實作 IEntity 介面（直接或間接）
        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&                     // 是類別
                       !t.IsAbstract &&                   // 不是抽象的
                       typeof(IEntity).IsAssignableFrom(t)) // 有實作 IEntity
            .ToList();
        
        // 3. 逐一註冊每個實體到 EF Core
        foreach (var entityType in entityTypes)
        {
            // 這裡動態呼叫 modelBuilder.Entity<TEntity>()
            // 因為 entityType 是 Type 物件，不是泛型參數
            // 所以用非泛型的 Entity 方法
            modelBuilder.Entity(entityType);
        }
        
        // 4. 回傳原 ModelBuilder 支援鏈式呼叫
        return modelBuilder;
    }


    /// <summary>
    /// 為模型中所有實體（Entity）的 Enum 屬性自動套用字串轉換器
    /// </summary>
    /// <param name="modelBuilder">EF Core 的 ModelBuilder 實例，用於建構實體模型</param>
    /// <remarks>
    /// 這個擴充方法會掃描所有實體中的所有屬性，
    /// 自動將 Enum 類型的屬性設定為以字串形式儲存在資料庫中，
    /// 而不是預設的數值（int）形式。
    /// </remarks>
    public static void ApplyEnumStringConverters(this ModelBuilder modelBuilder)
    {
        // 步驟 1：遍歷模型中的所有實體類型（如 User、Order 等）
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 步驟 2：遍歷目前實體類型中的所有屬性（如 Id、Name、Status 等）
            foreach (var property in entityType.GetProperties())
            {
                // 取得屬性的 CLR 型別（例如：UserStatus?、OrderStatus 等）
                var clrType = property.ClrType;
                
                // 步驟 3：處理 Nullable 型別
                // 如果屬性是 Nullable<T>（例如 UserStatus?），則取出內部的 T（UserStatus）
                // 如果屬性不是 Nullable，則直接使用原來的型別
                var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

                // 步驟 4：檢查是否為 Enum 型別
                // 只有當屬性是 Enum（或 Nullable<Enum>）時才需要處理
                if (underlyingType.IsEnum)
                {
                    // 步驟 5：建立對應的 EnumToStringConverter
                    // EnumToStringConverter<> 是 EF Core 內建的轉換器
                    // 使用 MakeGenericType 來建立像是 EnumToStringConverter<UserStatus> 的具體型別
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(clrType);
                    
                    // 步驟 6：透過 Activator 建立轉換器的實例
                    // 這等同於寫：new EnumToStringConverter<UserStatus>()
                    // 轉型為 ValueConverter 是因為 SetValueConverter 接受這個基底型別
                    var converter = Activator.CreateInstance(converterType) as ValueConverter;
                    
                    // 步驟 7：將轉換器設定給屬性
                    // 這行等同於在 Fluent API 中寫：.HasConversion<string>()
                    // 但這裡是用程式自動掃描並設定的
                    property.SetValueConverter(converter);
                    
                    // 這裡可以加入額外的配置，例如：
                    // 如果沒有設定 MaxLength，給予預設值
                    // if (property.GetMaxLength() == null)
                    // {
                    //     property.SetMaxLength(50);
                    // }
                }
            }
        }
    }

    /// <summary>
    /// 扩展方法，用于配置实体类型中的Id属性为不自动生成值
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder实例，用于构建实体模型</param>
    public static void ConfigureIdsAsNeverGenerated(this ModelBuilder modelBuilder)
    {
        // 遍历模型中的所有实体类型
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 查找名为"Id"的属性
            var idProperty = entityType.FindProperty("Id");
            // 如果找到Id属性
            if (idProperty != null)
            {
                // 将该属性的值生成策略设置为"从不生成"
                idProperty.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    /// <summary>
    /// 將所有表名轉換為 Snake Case 並複數化
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder 實例</param>
    public static void UseSnakeCasePluralTableNames(this ModelBuilder modelBuilder)
    {
        // 遍歷所有實體類型
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            // 如果表名為空，則跳過
            if (string.IsNullOrEmpty(tableName)) continue;

            // 轉換為 snake_case 並複數化
            var pluralSnake = ToSnakeCase(tableName).Pluralize(inputIsKnownToBeSingular: false);
            entity.SetTableName(pluralSnake);
        }
    }

    /// <summary>
    /// 將字串轉換為 Snake Case 格式
    /// </summary>
    /// <param name="input">要轉換的字串</param>
    /// <returns>轉換後的 Snake Case 字串</returns>
    private static string ToSnakeCase(string input)
    {
        // 如果輸入為空或 null，則直接返回
        if (string.IsNullOrEmpty(input)) return input;
        // 將字串轉換為 Snake Case 格式
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()
        ));
    }
}
