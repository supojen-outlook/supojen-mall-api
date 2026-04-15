using System.Security.Cryptography;

namespace Manian.Infrastructure.Extensions;

/// <summary>
/// AES-GCM 加解密擴充方法
/// 
/// AES-GCM (Galois/Counter Mode) 是目前最先進的對稱加密模式之一
/// 同時提供「機密性」（加密）和「完整性驗證」（認證標籤）
/// 
/// 為什麼用 GCM 而不是傳統的 CBC？
/// 1. GCM 是認證加密模式 (AEAD)，加密同時產生認證標籤
/// 2. 不需要額外的 HMAC 來驗證完整性
/// 3. 可平行處理，效能更好
/// 4. 在 TLS 1.2/1.3 中被廣泛使用
/// </summary>
internal static class AesGcmExtension
{
    /// <summary>
    /// AES-GCM 加密
    /// 
    /// 加密流程：
    /// 1. 輸入：明文、金鑰、nonce
    /// 2. 輸出：密文 + 認證標籤
    /// 
    /// 安全性特性：
    /// - 相同的明文+金鑰，只要 nonce 不同，就會產生完全不同的密文
    /// - 認證標籤確保密文不會被竄改
    /// </summary>
    /// <param name="plaintext">明文位元組陣列（要保護的原始資料）</param>
    /// <param name="key">AES 金鑰（此處預期為 32 bytes = AES-256）</param>
    /// <param name="nonce">一次性數字（12 bytes），類似 salt，但絕對不能重複使用</param>
    /// <returns>包含密文和認證標籤的 CipherText 物件</returns>
    /// <exception cref="CryptographicException">
    /// 當金鑰長度不正確（非 16/24/32 bytes）或 nonce 長度不正確（非 12 bytes）時拋出
    /// </exception>
    public static CipherText AesEncrypt(this byte[] plaintext, byte[] key, byte[] nonce)
    {
        // 1. 建立存放密文的陣列，長度與明文相同
        //    GCM 模式下，密文長度會等於明文長度（不像 CBC 需要填補）
        byte[] ciphertext = new byte[plaintext.Length];
        
        // 2. 建立存放認證標籤的陣列
        //    標籤固定 16 bytes (128 bits)，用於驗證完整性
        //    如果密文被竄改，解密時驗證標籤會失敗
        byte[] tag = new byte[16];

        // 3. 使用 using 確保 AesGcm 物件被正確釋放
        //    AesGcm 實作 IDisposable，內部可能包含非託管資源
        using (var aesGcm = new AesGcm(key, 16))  // 參數 16 指定標籤長度為 16 bytes
        {
            // 4. 執行 GCM 加密
            //    nonce: 一次性數字，絕對不能重複使用
            //    plaintext: 原始資料
            //    ciphertext: 加密結果（會填入陣列）
            //    tag: 認證標籤（會填入陣列）
            aesGcm.Encrypt(
                nonce: nonce,
                plaintext: plaintext,
                ciphertext: ciphertext,
                tag: tag
            );
        }

        // 5. 將密文和標籤包裝成 CipherText 物件回傳
        return new CipherText()
        {
            Text = ciphertext,
            Tag = tag
        };
    }
    
    /// <summary>
    /// AES-GCM 解密
    /// 
    /// 解密流程：
    /// 1. 輸入：密文、金鑰、nonce、認證標籤
    /// 2. 內部自動驗證標籤是否正確
    /// 3. 輸出：明文（若驗證通過）
    /// 
    /// 安全性特性：
    /// - 如果密文或標籤被竄改，解密會失敗並拋出例外
    /// - 這種設計防止了各種主動攻擊（如 padding oracle）
    /// </summary>
    /// <param name="ciphertext">密文（要解密的資料）</param>
    /// <param name="key">AES 金鑰（必須與加密時相同）</param>
    /// <param name="nonce">一次性數字（必須與加密時相同）</param>
    /// <param name="tag">認證標籤（必須與加密時相同）</param>
    /// <returns>解密後的明文位元組陣列</returns>
    /// <exception cref="CryptographicException">
    /// 當以下情況發生時拋出：
    /// - 金鑰不正確
    /// - 密文被竄改
    /// - 標籤驗證失敗
    /// - nonce 不正確
    /// </exception>
    public static byte[] AesDecrypt(this byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag)
    {
        // 1. 建立存放明文的陣列，長度與密文相同
        byte[] plaintext = new byte[ciphertext.Length];

        // 2. 使用 using 確保 AesGcm 物件被正確釋放
        using (var aesGcm = new AesGcm(key, 16))  // 必須與加密時的標籤長度一致
        {
            // 3. 執行 GCM 解密
            //    這個方法會自動驗證標籤：
            //    - 用金鑰和 nonce 重新計算密文的認證標籤
            //    - 比對計算結果與傳入的 tag 是否相同
            //    - 如果不相同，表示資料被竄改或金鑰錯誤，拋出例外
            aesGcm.Decrypt(
                nonce: nonce,
                ciphertext: ciphertext,
                tag: tag,
                plaintext: plaintext
            );
        }

        // 4. 回傳解密後的明文
        return plaintext;
    }
}

/// <summary>
/// AES-GCM 密文容器
/// 
/// 這個類別用來包裝 AES-GCM 加密的兩個輸出：
/// 1. 密文本體 (Text)
/// 2. 認證標籤 (Tag)
/// 
/// 為什麼需要這個包裝？
/// - 讓 API 更清楚：呼叫端知道需要同時處理密文和標籤
/// - 避免參數順序錯誤：Text 和 Tag 是具名屬性，不易混淆
/// - 便於未來擴充：如果需要加入 additional authenticated data (AAD)
/// </summary>
internal class CipherText
{
    /// <summary>
    /// 密文本體
    /// 
    /// 這是實際的加密資料，長度會與原始明文相同
    /// 雖然是「密文」，但不需要保密（只是看不懂），真正重要的是金鑰
    /// </summary>
    public required byte[] Text { get; set; }

    /// <summary>
    /// 認證標籤 (Authentication Tag)
    /// 
    /// 長度固定為 16 bytes (128 bits)
    /// 功能類似數位簽章：
    /// - 確保密文在傳輸過程中沒有被竄改
    /// - 確保解密時使用的是正確的金鑰
    /// 
    /// 如果攻擊者修改了密文，解密時標籤驗證會失敗
    /// </summary>
    public required byte[] Tag { get; set; }
}