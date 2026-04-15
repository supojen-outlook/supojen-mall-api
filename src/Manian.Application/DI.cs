using System.Reflection;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Po.Media;
using Shared.Mediator;

namespace Manian.Application;

/// <summary>
/// 應用層的依賴注入配置類別
/// 
/// 這個類別負責註冊應用層（Application Layer）所需的所有服務
/// 遵循 Clean Architecture 的相依性原則：
/// - 應用層依賴於領域層（Domain）
/// - 基礎設施層（Infrastructure）依賴於應用層
/// - 外部專案不應直接依賴應用層內部實作
/// </summary>
public static class DI
{
    /// <summary>
    /// 註冊應用層服務
    /// 
    /// 這個擴充方法封裝了應用層的所有 DI 註冊邏輯
    /// 讓 Program.cs 保持簡潔，只需一行：services.AddApplication(configuration)
    /// 
    /// 註冊的服務包括：
    /// 1. 多媒體服務（圖片、檔案處理）
    /// 2. 中介者模式實作（處理命令和查詢）
    /// 3. Mapster 物件映射配置（實體與 DTO 轉換）
    /// </summary>
    /// <param name="services">要擴充的 IServiceCollection 容器</param>
    /// <param name="configuration">應用程式設定，用於讀取連線字串或外部服務設定</param>
    /// <returns>傳回原容器，支援鏈式呼叫</returns>
    public static IServiceCollection AddApplication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // ----- 1. 註冊多媒體服務（圖片上傳、檔案處理）-----
        // 這是 Po.Media 專案提供的功能，可能包含：
        // - 圖片縮圖產生
        // - 檔案儲存（本機或雲端）
        // - 多媒體格式驗證
        services.AddMediaService(configuration);

        // ----- 2. 註冊中介者模式實作 -----
        // Shared.Mediator 可能實作了 Mediator 模式
        // 用於處理 CQRS 的命令和查詢
        // 類似 MediatR 套件的功能，但可能是自訂實作
        services.AddMediator();

        // ----- 3. 設定 Mapster 物件映射 -----
        // 掃描當前組件（Application 層）中所有 Mapster 相關的配置
        // 例如：實作 IRegister 介面的類別，或用 Attribute 標記的映射規則
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        
        // 註冊 Mapster 服務到 DI 容器
        // 讓應用程式中可以透過 IMapper 介面使用物件映射功能
        services.AddMapster();

        return services;
    }
}