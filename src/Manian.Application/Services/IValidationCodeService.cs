namespace Manian.Application.Services;

/// <summary>
/// 驗證碼服務介面
/// 
/// 定義了系統中生成和驗證驗證碼所需的基本功能。
/// 此介面實現了依賴注入模式，允許不同的驗證碼服務實作來替換實現細節。
/// 主要用途包括用戶註冊、登入、密碼重置等場景的身份驗證。
/// </summary>
public interface IValidationCodeService
{
    /// <summary>
    /// 生成驗證碼
    /// 
    /// 根據指定的鍵值生成一個驗證碼，並將其儲存起來供後續驗證使用。
    /// 驗證碼通常包含數字和字母組合，長度可配置，有效期可設定。
    /// 驗證碼會與指定的鍵值關聯，通常使用使用者ID或電子郵件等唯一識別符作為鍵。
    /// </summary>
    /// <param name="key">驗證碼的鍵值，用於後續驗證時識別對應的驗證碼。通常為使用者ID、電子郵件或手機號碼等唯一識別符。</param>
    /// <returns>生成的驗證碼字串。返回的驗證碼應為不區分大小寫的字串，通常包含數字和字母。</returns>
    /// <exception cref="System.ArgumentNullException">當 key 為 null 或空字串時拋出。</exception>
    /// <exception cref="System.ArgumentException">當 key 長度超過限制時拋出。</exception>
    /// <exception cref="System.InvalidOperationException">當無法生成驗證碼時拋出（例如內部儲存錯誤）。</exception>
    string GenerateCode(string key);

    /// <summary>
    /// 驗證碼有效性檢查
    /// 
    /// 根據指定的鍵值和驗證碼，檢查驗證碼是否有效且在有效期限內。
    /// 驗證通過後，通常會使該驗證碼失效，防止重複使用。
    /// 此方法應該考慮驗證碼的時效性、正確性和一次性使用特性。
    /// </summary>
    /// <param name="key">驗證碼的鍵值，用於識別對應的驗證碼。必須與生成驗證碼時使用的鍵值相同。</param>
    /// <param name="code">待驗證的驗證碼。通常不區分大小寫，應與生成時返回的值一致。</param>
    /// <returns>如果驗證碼有效且在有效期限內，返回 true；否則返回 false。</returns>
    /// <exception cref="System.ArgumentNullException">當 key 或 code 為 null 或空字串時拋出。</exception>
    /// <exception cref="System.ArgumentException">當 key 或 code 長度超過限制時拋出。</exception>
    /// <exception cref="System.InvalidOperationException">當驗證過程中發生內部錯誤時拋出。</exception>
    bool ValidateCode(string key, string code);
}