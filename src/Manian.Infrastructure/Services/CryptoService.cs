using System.Security.Cryptography;
using System.Text;
using Manian.Infrastructure.Extensions;
using Manian.Application.Services;
using Manian.Infrastructure.Settings;

namespace Manian.Infrastructure.Services;

/// <summary>
/// 加解密服務 - 使用 AES-GCM 認證加密模式
/// 
/// AES-GCM (Galois/Counter Mode) 的特點：
/// 1. 同時提供機密性（加密）和完整性（認證）
/// 2. 會產生兩個輸出：密文 (ciphertext) 和認證標籤 (tag)
/// 3. 認證標籤用於驗證密文在傳輸過程中是否被竄改
/// 4. 需要 nonce（12 bytes）來確保相同明文每次加密結果都不同
/// </summary>
public class CryptoService : ICryptoService
{
    /// <summary>
    /// 金鑰物件 - 包含 AES 對稱金鑰
    /// 由 DI 容器注入，確保整個應用程式使用相同的金鑰集合
    /// </summary>
    private readonly Key _key;

    /// <summary>
    /// 建構函式 - 注入金鑰物件
    /// </summary>
    /// <param name="key">包含 AES 金鑰的金鑰管理物件</param>
    public CryptoService(Key key)
    {
        _key = key;
    }
    
    /// <summary>
    /// 將明文加密為二進位資料 (AES-GCM 模式)
    /// 
    /// 輸出格式：
    /// ┌────────────────┬────────────────┬────────────────┐
    /// │   密文 (變長)   │  認證標籤 (16B) │  Nonce (12B)   │
    /// └────────────────┴────────────────┴────────────────┘
    /// 
    /// 為什麼要這樣組合？
    /// 1. 將 nonce 和 tag 與密文一起儲存，解密時不需要額外欄位
    /// 2. nonce 放在最後，解密時可以從結尾取出
    /// 3. 標籤放在中間，用於驗證密文的完整性
    /// </summary>
    /// <param name="plainText">要加密的明文字串</param>
    /// <returns>組合後的位元組陣列（包含密文、認證標籤和 nonce）</returns>
    public byte[] Encrypt(string plainText)
    {
        // 1. 生成隨機 nonce（12 bytes）
        //    nonce = "number used once"（一次性數字）
        //    在 AES-GCM 中，nonce 不需要保密，但絕對不能重複使用
        //    長度固定為 12 bytes（96 bits）是 GCM 模式的推薦值
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);  // 密碼學安全的隨機數產生器

        // 2. 執行 AES-GCM 加密
        //    將明文字串轉為 UTF-8 位元組陣列後呼叫擴充方法 AesEncrypt
        //    AesEncrypt 會回傳一個包含兩個屬性的物件：
        //    - Text: 加密後的密文
        //    - Tag:  認證標籤（16 bytes），用於驗證完整性
        var cipherText = Encoding.UTF8.GetBytes(plainText).AesEncrypt(
            key: _key.AesKey,   // AES 金鑰（從 Key 物件取得）
            nonce: nonce);       // 隨機產生的 nonce

        // 3. 組合最終輸出：密文 + 標籤 + nonce
        //    使用 Combine 擴充方法將三個部分串接成一個位元組陣列
        //    順序很重要：解密時必須按照相同順序拆解
        return cipherText.Text.Combine(cipherText.Tag).Combine(nonce);
    }

    /// <summary>
    /// 將加密後的二進位資料解密回明文 (AES-GCM 模式)
    /// 
    /// 解密流程：
    /// 1. 從組合的位元組陣列中分離出 nonce、標籤和密文
    /// 2. 用 AES-GCM 解密並驗證完整性
    /// 3. 將解密後的位元組轉回 UTF-8 字串
    /// </summary>
    /// <param name="cipherText">Encrypt 方法產生的完整位元組陣列</param>
    /// <returns>原始明文字串</returns>
    /// <exception cref="CryptographicException">
    /// 當資料被竄改或金鑰錯誤時拋出（由 AesDecrypt 內部拋出）
    /// </exception>
    public string Decrypt(byte[] cipherText)
    {
        // 1. 從結尾取出 nonce（最後 12 bytes）
        //    nonce 在加密時被放在最後面
        var nonce = cipherText.SubSet(
            cipherText.Length - 12,  // 起始位置：總長度減12
            cipherText.Length);       // 結束位置：總長度
        
        // 2. 取出認證標籤（倒數第 12+16 到倒數第 12 bytes）
        //    標籤固定長度 16 bytes，放在 nonce 前面
        var tag = cipherText.SubSet(
            cipherText.Length - 12 - 16,  // 起始位置：總長度減28
            cipherText.Length - 12);       // 結束位置：總長度減12
        
        // 3. 取出真正的密文（從開頭到標籤開始之前）
        //    密文的長度 = 總長度 - 12(nonce) - 16(tag)
        var text = cipherText.SubSet(
            0,                                   // 起始位置：開頭
            cipherText.Length - 12 - 16);        // 結束位置：密文結尾
        
        // 4. 執行 AES-GCM 解密
        //    AesDecrypt 會內部驗證 tag 是否正確
        //    如果驗證失敗（資料被竄改或金鑰錯誤），會拋出例外
        var plainText = text.AesDecrypt(
            key: _key.AesKey,    // 相同的 AES 金鑰
            nonce: nonce,         // 相同的 nonce
            tag: tag);            // 用於驗證的認證標籤
        
        // 5. 將解密後的位元組陣列轉回 UTF-8 字串
        //    假設原始明文是 UTF-8 編碼的字串
        return Encoding.UTF8.GetString(plainText);
    }
}