using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Manian.Application.Services;
using Manian.Domain.Repositories;
using Manian.Domain.Services;
using Manian.Infrastructure.Persistence;
using Manian.Infrastructure.Services;
using Manian.Infrastructure.Settings;
using Key = Manian.Infrastructure.Settings.Key;

namespace Manian.Infrastructure;

/// <summary>
/// 依賴注入(DI)的靜態擴充類別
/// 負責將基礎設施層（Infrastructure）的所有服務註冊到 DI 容器中
/// 採用靜態擴充方法設計，讓 Program.cs 的配置保持簡潔
/// </summary>
public static class DI
{
    /// <summary>
    /// 註冊基礎設施層所需的全部服務
    /// 這是整個基礎設施層的對外入口，涵蓋資料庫、倉儲、密碼服務、金鑰等
    /// </summary>
    /// <param name="services">要擴充的 IServiceCollection 容器</param>
    /// <param name="configuration">應用程式配置介面，用於讀取 appsettings.json 或環境變數</param>
    /// <returns>傳回原容器以支援鏈式呼叫（Fluent Interface）</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) 
    {
        // 註冊金鑰相關服務（例如 RSA 金鑰的路徑或內容）
        // 必須放在前面，因為後續的 CryptoService 可能依賴它
        services.AddKey(configuration);

        // 註冊密碼雜湊服務（如 BCrypt 或 PBKDF2 的封裝）
        services.AddScoped<IPasswordService, PasswordService>();
        
        // 註冊加解密服務（如 AES、RSA 的封裝）
        services.AddScoped<ICryptoService, CryptoService>();

        // 掃描當前組件，自動註冊所有名稱結尾為 Repository 的實作
        services.AddRepositories();

        // 註冊雪花演算法 ID 產生器（單例模式，確保工作序號不衝突）
        services.AddSingleton<IUniqueIdentifier, Snowflake>();

        // 註冊當前使用者上下文服務（通常從 HttpContext 解析）
        services.AddScoped<IUserClaim, UserClaim>();

        // 註冊 MainDbContext 到 DI 容器
        // 設定使用 PostgreSQL 資料庫，連線字串從 appsettings.json 取得
        services.AddDbContext<MainDbContext>(opts =>
        {
            opts.UseNpgsql(configuration.GetConnectionString("Main"));
        });

        // 註冊驗證碼服務（通常搭配記憶體快取使用）
        services.AddValidationCodeService();

        // 註冊電子郵件服務（僅在設定檔中有對應設定時才註冊）
        services.AddEmailService(configuration);

        return services;
    }

    /// <summary>
    /// 自動掃描並註冊所有倉儲（Repository）實作
    /// 慣例：任何名稱結尾為 "Repository" 的類別，且實作了對應的 I[名稱]Repository 介面
    /// 例如：UserRepository -> IUserRepository
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <returns>傳回原容器，支援鏈式呼叫</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 找出所有符合條件的型別：具體類別、非抽象、名稱以 Repository 結尾
        var types = assembly.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.Name.EndsWith("Repository"));

        foreach (var implementationType in types)
        {
            var interfaces = implementationType.GetInterfaces();
            
            // 尋找對應的倉儲介面（慣例：I 開頭、Repository 結尾、且不是泛型基底 IRepository<>）
            var repositoryInterface = interfaces.FirstOrDefault(x => 
                x.Name.StartsWith("I") && 
                x.Name.EndsWith("Repository") &&
                x != typeof(IRepository<>)); // 排除泛型基底介面，避免錯誤註冊
                
            if (repositoryInterface != null)
            {
                // 註冊為 Scoped 生命週期（每個 HTTP 請求一個實例）
                services.AddScoped(repositoryInterface, implementationType);
            }
        }

        return services;
    }  

    /// <summary>
    /// 註冊金鑰服務（Key）
    /// 金鑰的路徑可從環境變數或設定檔取得，若無則預設為執行目錄
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <param name="configuration">應用程式設定</param>
    /// <returns>傳回原容器</returns>
    public static IServiceCollection AddKey(
        this IServiceCollection services, IConfiguration configuration)
    {
        // 嘗試從環境變數取得 ASPNETCORE_DIRECTORY（通常用於指定金鑰存放目錄）
        var directory = Environment.GetEnvironmentVariable("ASPNETCORE_DIRECTORY");
        
        // 若環境變數不存在，預設使用當前執行目錄（"./"）
        // 此 Key 物件通常包含 RSA 私鑰的路徑或憑證資訊
        services.AddSingleton(new Key(directory ?? "./"));
        
        return services;
    }

    /// <summary>
    /// 註冊驗證碼服務（Validation Code）
    /// 包含 IMemoryCache（用於暫存驗證碼）以及對應的服務實作
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <returns>傳回原容器</returns>
    public static IServiceCollection AddValidationCodeService(this IServiceCollection services)
    {
        // 註冊記憶體快取，用於儲存驗證碼（設定過期時間）
        services.AddMemoryCache();
        
        // 註冊驗證碼服務，Scoped 生命週期確保每個請求有獨立的驗證碼狀態
        services.AddScoped<IValidationCodeService, ValidationCodeService>();
        
        return services;
    }

    /// <summary>
    /// 有條件地註冊電子郵件服務
    /// 僅當設定檔中存在 "EmailSettings" 區塊（且不為空）時，才註冊郵件服務
    /// 避免在未設定郵件時拋出例外，或浪費資源註冊無效服務
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <param name="configuration">應用程式設定</param>
    public static void AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        var emailSection = configuration.GetSection("EmailSettings");

        // 檢查 EmailSettings 區塊是否存在（有任何子節點或有直接值）
        if (emailSection.GetChildren().Any() || !string.IsNullOrEmpty(emailSection.Value))
        {
            // 將設定檔中的 EmailSettings 綁定到 EmailSettings 選項類別
            // 讓 EmailService 可以透過 IOptions<EmailSettings> 取得設定
            services.Configure<EmailSettings>(emailSection);
            
            // 註冊郵件發送服務（Transient：每次請求都建立新實例，適合輕量無狀態服務）
            services.AddTransient<IEmailService, EmailService>();
        }
    }
}