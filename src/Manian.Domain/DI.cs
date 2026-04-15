using Microsoft.Extensions.DependencyInjection;
using Manian.Domain.Services;

namespace Manian.Domain;

/// <summary>
/// 領域層的依賴注入配置類別
/// 
/// 職責：
/// - 註冊領域層服務到 DI 容器
/// - 封裝領域層的服務註冊邏輯
/// 
/// 設計原則：
/// - 遵循 Clean Architecture 的相依性原則
/// - 領域層不依賴基礎設施層
/// - 只註冊領域服務，不註冊倉儲實作
/// 
/// 使用方式：
/// - 在 Infrastructure 層的 DI.cs 中呼叫 AddDomain()
/// - 確保領域服務在基礎設施服務之前註冊
/// </summary>
public static class DI
{
    /// <summary>
    /// 註冊領域層服務
    /// 
    /// 註冊的服務：
    /// - PromotionCalculationService：促銷計算服務
    /// 
    /// 設計考量：
    /// - 使用 Scoped 生命週期，與 HTTP 請求一致
    /// - 依賴介面而非實作，符合依賴倒置原則
    /// - 只註冊服務，不註冊倉儲（倉儲由 Infrastructure 層註冊）
    /// 
    /// 注意事項：
    /// - 此方法不應註冊任何基礎設施層的服務
    /// - 倉儲介面和實作的註冊應在 Infrastructure 層完成
    /// </summary>
    /// <param name="services">要擴充的 IServiceCollection 容器</param>
    /// <returns>傳回原容器，支援鏈式呼叫</returns>
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        // ========== 註冊促銷計算服務 ==========
        // 使用 Scoped 生命週期，與 HTTP 請求一致
        // 依賴 IProductRepository 和 IPromotionRepository
        // 這些介面由 Infrastructure 層提供實作
        services.AddScoped<PromotionCalculationService>();

        // ========== 註冊運費計算服務 ==========
        // 使用 Scoped 生命週期，與 HTTP 請求一致
        // 依賴 IShippingRuleRepository
        // 這個介面由 Infrastructure 層提供實作
        services.AddScoped<ShippingFeeCalculationService>();

       // ========== 註冊優惠券計算服務 ==========
        // 使用 Scoped 生命週期，與 HTTP 請求一致
        // 依賴 ICouponRepository
        // 這個介面由 Infrastructure 層提供實作
        services.AddScoped<CouponCalculationService>();
        
        return services;
    }
}
