using Microsoft.Extensions.Caching.Memory;
using Manian.Application.Services;

namespace Manian.Infrastructure.Services;

/// <summary>
/// 验证码服务类，实现IValidationCodeService接口
/// 用于生成和验证验证码，并使用内存缓存存储验证码
/// </summary>
public class ValidationCodeService : IValidationCodeService
{
    // 内存缓存接口，用于存储验证码
    private readonly IMemoryCache _cache;
    // 验证码过期时间，默认为5分钟
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 构造函数，通过依赖注入获取内存缓存服务
    /// </summary> 
    /// <param name="cache">内存缓存服务</param>
    public ValidationCodeService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 生成 6 位數字驗證碼，並綁定到指定 key (email/phone)
    /// </summary>
    /// <param name="key">绑定的键值（可以是邮箱或手机号）</param>
    /// <returns>生成的6位数字验证码</returns>
    public string GenerateCode(string key)
    {
        // 创建随机数生成器
        var random = new Random();
        // 生成6位数字验证码，不足6位时前面补零
        string code = random.Next(0, 999999).ToString("D6"); // 6 位數字，補零

        // 将验证码存入缓存，并设置过期时间
        _cache.Set(key, code, _expiration);

        // 返回生成的验证码
        return code;
    }

    /// <summary>
    /// 驗證使用者輸入的 code 是否正確
    /// </summary>
    /// <param name="key">验证码绑定的键值</param>
    /// <param name="code">用户输入的验证码</param>
    /// <returns>验证结果，true表示验证成功，false表示验证失败</returns>
    public bool ValidateCode(string key, string code)
    {
        // 尝试从缓存中获取验证码
        if (_cache.TryGetValue(key, out string? cachedCode))
        {
            // 比较缓存中的验证码和用户输入的验证码
            return cachedCode == code;
        }
        // 如果缓存中没有找到验证码，验证失败
        return false;
    }
}