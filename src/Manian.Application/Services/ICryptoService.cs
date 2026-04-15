namespace Manian.Application.Services;

/// <summary>
/// 加密服務介面 - 定義應用程式中對稱加解密的契約
/// 
/// 這個介面位於應用層 (Application Layer)，定義了「加密」這個業務需求
/// 但不關心具體的實作細節（如使用 AES-GCM、ChaCha20 或其他演算法）
/// 
/// 為什麼要抽象化？
/// 1. 隔離加解密演算法的實作細節
/// 2. 方便單元測試（可以 Mock 加解密行為）
/// 3. 未來可替換演算法（例如從 AES-GCM 換成 ChaCha20）而不影響上層程式碼
/// 
/// 使用 AES-GCM 的原因：
/// - 同時提供機密性（加密）和完整性（認證）
/// - 是目前業界推薦的認證加密模式
/// - 被 TLS 1.2/1.3 廣泛使用
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// 將明文字串加密為二進位資料
    /// 
    /// 這個方法封裝了整個加密流程：
    /// 1. 產生隨機 nonce（一次性數字）
    /// 2. 使用 AES-GCM 進行認證加密
    /// 3. 組合密文、認證標籤和 nonce 為單一位元組陣列
    /// 
    /// 適用場景：
    /// - 儲存敏感資料到資料庫（如個資、金融資訊）
    /// - 產生安全的 API Token
    /// - 保護傳輸中的敏感資訊
    /// 
    /// 安全性保證：
    /// - 相同明文每次加密結果都不同（因為隨機 nonce）
    /// - 任何對密文的竄改都會導致解密失敗
    /// </summary>
    /// <param name="plainText">要保護的明文字串</param>
    /// <returns>加密後的二進位資料（包含密文、認證標籤和 nonce）</returns>
    byte[] Encrypt(string plainText);

    /// <summary>
    /// 將加密後的二進位資料解密回明文字串
    /// 
    /// 這個方法執行反向操作：
    /// 1. 從輸入的位元組陣列中分離出 nonce、認證標籤和密文
    /// 2. 使用 AES-GCM 進行解密並驗證完整性
    /// 3. 將解密後的位元組轉回 UTF-8 字串
    /// 
    /// 安全特性：
    /// - 如果資料被竄改，解密會失敗並拋出例外
    /// - 驗證失敗時不應洩漏任何部分解密的資訊
    /// 
    /// 例外處理：
    /// 當發生以下情況時，實作應該拋出 CryptographicException：
    /// - 金鑰不正確
    /// - 密文被竄改（標籤驗證失敗）
    /// - nonce 不正確
    /// - 輸入格式錯誤
    /// </summary>
    /// <param name="cipherText">Encrypt 方法產生的完整位元組陣列</param>
    /// <returns>解密後的原始明文字串</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// 解密失敗時拋出，例如資料被竄改、金鑰錯誤等
    /// </exception>
    public string Decrypt(byte[] cipherText);
}