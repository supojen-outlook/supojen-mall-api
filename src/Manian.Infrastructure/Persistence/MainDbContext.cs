using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Persistence;

/// <summary>
/// 自定义数据库上下文类，继承自DbContext，用于管理数据库连接和实体映射
/// </summary>
public class MainDbContext : DbContext
{
    /// <summary>
    /// 构造函数，初始化数据库上下文
    /// </summary>
    /// <param name="options">数据库上下文选项，包含配置信息</param>
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

    /// <summary>
    /// 配置数据库上下文选项，在这里设置命名约定为蛇形命名法
    /// </summary>
    /// <param name="optionsBuilder">用于构建DbContext选项的构建器</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 使用蛇形命名约定配置数据库表名和列名
        // 这将使实体属性名与数据库列名之间的映射采用下划线分隔的命名方式
        // 例如：UserName 将映射为 user_name
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    /// <summary>
    /// 配置实体模型，在此处应用实体配置、设置表名格式和特殊属性配置
    /// </summary>
    /// <param name="modelBuilder">用于构建实体模型的构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1️⃣ 先自動註冊所有實體（建立基本模型））
        modelBuilder.RegisterAllEntities();

        // 2️⃣ 再用具體配置覆蓋部分設定
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);

        // 设置表名使用蛇形复数命名法
        modelBuilder.UseSnakeCasePluralTableNames();
        
        // 配置ID属性为不自动生成
        modelBuilder.ConfigureIdsAsNeverGenerated();

        // 配置枚举类型属性使用字符串存储
        modelBuilder.ApplyEnumStringConverters();
    }

    /// <summary>
    /// 取得指定 Entity 對應資料表的估計資料筆數
    /// </summary>
    /// <remarks>
    /// 此方法查詢 PostgreSQL 系統目錄 pg_class 取得估計筆數，比 COUNT(*) 更快，
    /// 適合儀表板、監控等不需要精確數值的場景。
    /// 
    /// 注意：回傳值為估計值，可能與實際筆數有誤差（通常 1-5% 以內）。
    /// 若資料表從未 ANALYZE 或發生錯誤，則回傳 null。
    /// </remarks>
    /// <typeparam name="T">Entity 類型</typeparam>
    /// <returns>估計筆數，若無法取得則回傳 null</returns>
    /// <example>
    /// var userCount = _context.GetEstimatedRowCount&lt;User&gt;() ?? 0;
    /// </example>
    public int? GetEstimatedRowCount<T>() where T : class
    {
        try
        {
            var tableName = Model.FindEntityType(typeof(T))?.GetTableName();
            if (string.IsNullOrEmpty(tableName))
                return null;

            FormattableString sql = $"SELECT NULLIF(reltuples, -1)::int FROM pg_class WHERE relname = {tableName}";
            return Database.SqlQuery<int?>(sql).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
