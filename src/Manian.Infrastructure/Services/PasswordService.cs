using System.Collections;
using System.Security.Cryptography;
using System.Text;

using Manian.Infrastructure.Extensions;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Manian.Application.Services;

namespace Manian.Infrastructure.Services;

/// <summary>
/// 密碼服務 - 負責密碼的雜湊儲存、驗證和隨機生成
/// 
/// 安全設計原則：
/// 1. 絕不儲存明碼密碼，只儲存加鹽雜湊值
/// 2. 使用 PBKDF2 金鑰衍生函數，增加暴力破解難度
/// 3. 每個密碼使用獨立的隨機鹽，防止彩虹表攻擊
/// 4. 使用 HMACSHA512 作為底層雜湊演算法，提供足夠的安全性
/// </summary>
public class PasswordService : IPasswordService
{
    /// <summary>
    /// 鹽的位元組數 - 16 bytes = 128 bits
    /// 鹽是用來與密碼組合後一起雜湊的隨機值
    /// 128 bits 的鹽在目前技術下被認為是安全的，且足夠防止碰撞
    /// </summary>
    public const int SaltBytes = 16;

    /// <summary>
    /// 密文（雜湊結果）的位元組數 - 32 bytes = 256 bits
    /// HMACSHA512 會輸出 512 bits (64 bytes) 的雜湊值
    /// 但我們只取前 256 bits 作為最終結果，在安全性與儲存空間間取得平衡
    /// </summary>
    public const int CipherTextBytes = 32;
    
    /// <summary>
    /// PBKDF2 迭代次數 - 1000 次
    /// 迭代次數越高，暴力破解的成本越高
    /// 注意：1000 次在目前標準下可能偏低，生產環境建議使用 10000-100000 次
    /// 此處設為 1000 可能是為了相容性考量或較早的實作
    /// </summary>
    public const int IterationCount = 1000;

    /// <summary>
    /// 底層偽隨機函數 (PRF) 的選擇 - HMACSHA512
    /// HMAC (Hash-based Message Authentication Code) 結合金鑰的雜湊函數
    /// SHA512 提供 512 bits 的輸出，安全性高於 SHA256
    /// </summary>
    public const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA512;
    
    /// <summary>
    /// 將明碼密碼進行加鹽雜湊，產生安全的密文
    /// 
    /// 儲存格式：Base64( Salt (16 bytes) + PBKDF2 Result (32 bytes) )
    /// 總共 48 bytes，轉為 Base64 後約 64 字元
    /// 
    /// 為什麼要這樣設計？
    /// 1. 將鹽和雜湊結果儲存在一起，驗證時不需要額外的鹽欄位
    /// 2. Base64 編碼讓密文可以當作字串儲存在資料庫中
    /// 3. 每個密碼使用獨立的隨機鹽，即使兩個使用者密碼相同，密文也不同
    /// </summary>
    /// <param name="password">使用者輸入的明碼密碼</param>
    /// <returns>Base64 編碼的完整密文（含鹽）</returns>
    public string Hash(string password)
    {
        // 1. 生成密碼專屬的隨機鹽
        //    鹽必須是密碼學安全的隨機數，不能用 Random 類
        var salt = new byte[SaltBytes];
        using (var generator = RandomNumberGenerator.Create())
        {
            // RandomNumberGenerator.GetBytes 會填入密碼學強度的隨機位元組
            generator.GetBytes(salt);
        }

        // 2. 使用 PBKDF2 金鑰衍生函數產生最終雜湊
        //    PBKDF2 的作用：將密碼和鹽組合後，反覆進行 HMAC 運算 IterationCount 次
        //    這樣做的好處是大幅增加暴力破解的時間成本
        var pbkdf2Result = KeyDerivation.Pbkdf2(
            password: password,           // 原始密碼（明碼）
            salt: salt,                   // 剛生成的隨機鹽
            prf: Prf,                     // HMACSHA512
            iterationCount: IterationCount, // 1000 次迭代
            numBytesRequested: CipherTextBytes); // 輸出 32 bytes
        
        // 3. 將鹽和 PBKDF2 結果合併成一個位元組陣列
        //    使用自訂的 Combine 擴充方法（在 Manian.Infrastructure.Extensions 中）
        //    合併順序：前面 16 bytes 是鹽，後面 32 bytes 是雜湊值
        var passwordBytes = salt.Combine(pbkdf2Result);

        // 4. 轉換為 Base64 字串儲存
        //    Base64 是將二進位資料編碼成可列印字元的安全方式
        //    DB 中的密碼欄位可以直接儲存這個字串
        return Convert.ToBase64String(passwordBytes);
    }

    /// <summary>
    /// 驗證使用者輸入的密碼是否與儲存的密文相符
    /// 
    /// 驗證流程：
    /// 1. 從密文中取出鹽（前 16 bytes）
    /// 2. 用同樣的鹽、同樣的迭代次數，對輸入密碼重新計算 PBKDF2
    /// 3. 比對重新計算的結果是否與原密文的雜湊部分相同
    /// </summary>
    /// <param name="password">使用者輸入的明碼密碼</param>
    /// <param name="cipherText">資料庫中儲存的 Base64 密文</param>
    /// <returns>true: 密碼正確；false: 密碼錯誤</returns>
    public bool Validate(string password, string cipherText)
    {
        // 1. 將 Base64 密文解碼回原始位元組陣列
        //    這個陣列的前 SaltBytes 是鹽，後面是雜湊值
        var passwordBytes = Convert.FromBase64String(cipherText);

        // 2. 取出鹽的部分（前 16 bytes）
        //    SubSet 應該是自訂的擴充方法，從指定索引開始取 SaltBytes 長度
        var salt = passwordBytes.SubSet(startIndex: 0, endIndex: SaltBytes);
        
        // 3. 用相同的參數重新計算 PBKDF2
        //    使用同一個鹽，確保能產生相同的雜湊值
        var pbkdf2Result = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: Prf,
            iterationCount: IterationCount,
            numBytesRequested: CipherTextBytes);

        // 4. 將鹽和重新計算的雜湊值合併
        //    目的是與原始的 passwordBytes 進行比對
        var computeResult = salt.Combine(pbkdf2Result);

        // 5. 比對兩個位元組陣列是否完全相同
        //    IStructuralComparable 可以用結構化的方式比較集合
        //    CompareTo 回傳 0 代表兩個陣列完全相等
        //    
        //    為什麼不用 SequenceEqual？
        //    這裡可能是為了展示另一種比較方式，但 SequenceEqual 更直觀
        return ((IStructuralComparable) passwordBytes).CompareTo(computeResult, Comparer<byte>.Default) == 0;
    }

    /// <summary>
    /// 生成隨機密碼
    /// 用於「忘記密碼」功能、建立新帳號時產生初始密碼等場景
    /// </summary>
    /// <param name="default_length">密碼長度，預設 14 字元</param>
    /// <returns>隨機生成的密碼字串</returns>
    public string Generate(int default_length = 14)
    {
        // 可用字元池：大小寫英文字母加上底線
        // 注意：這個字元池沒有包含數字和特殊符號，安全性較低
        // 建議加入 0-9 和 !@#$% 等符號增加密碼複雜度
        const string char_pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_";
        
        var passwordBuilder = new StringBuilder();
        
        // 迴圈產生指定長度的密碼
        for (var i = 0; i < default_length; i++)
        {
            // 1. 產生一個隨機位元組
            //    RandomNumberGenerator.GetBytes(1) 回傳長度為 1 的 byte 陣列
            var randomByte = RandomNumberGenerator.GetBytes(1);
            
            // 2. 將隨機位元組映射到字元池的索引範圍
            //    randomByte[0] 的值範圍是 0-255
            //    % char_pool.Length 確保索引值在 0 到字元池長度-1 之間
            var index = randomByte[0] % char_pool.Length;
            
            // 3. 從字元池取出對應的字元，加入密碼字串
            passwordBuilder.Append(char_pool[index]);
        }

        // 回傳生成的隨機密碼
        return passwordBuilder.ToString();
    }
}