namespace Manian.Infrastructure.Extensions;

/// <summary>
/// 位元組陣列擴充方法
/// 
/// 提供一組實用的位元組陣列操作，包括：
/// - 合併兩個位元組陣列
/// - 取出子陣列
/// - 與字串之間的編碼轉換
/// 
/// internal 存取層級表示這些方法僅限於基礎設施層內部使用
/// 不應該暴露給上層（如 Application 或 Presentation 層）
/// </summary>
internal static class ByteExt
{
    /// <summary>
    /// 合併兩個位元組陣列
    /// 
    /// 這個方法在密碼學和加解密場景中特別常用
    /// 例如：將鹽(Salt)和雜湊值(Hash)合併儲存，或將密文(CipherText)和認證標籤(Tag)合併
    /// </summary>
    /// <param name="first">第一個位元組陣列（將放在結果的前面）</param>
    /// <param name="second">第二個位元組陣列（將放在結果的後面）</param>
    /// <returns>合併後的新位元組陣列，長度為 first.Length + second.Length</returns>
    /// <example>
    /// byte[] a = { 0x01, 0x02 };
    /// byte[] b = { 0x03, 0x04, 0x05 };
    /// byte[] result = a.Combine(b); // 結果：{ 0x01, 0x02, 0x03, 0x04, 0x05 }
    /// </example>
    public static byte[] Combine(this byte[] first, byte[] second)
    {
        // 1. 建立一個新陣列，大小為兩個陣列的長度總和
        var rv = new byte[first.Length + second.Length];
        
        // 2. 使用 Buffer.BlockCopy 將第一個陣列複製到新陣列的開頭
        //    Buffer.BlockCopy 是低階的記憶體複製操作，效能比 Array.Copy 更好
        //    參數說明：來源陣列, 來源起始位置, 目標陣列, 目標起始位置, 要複製的位元組數
        Buffer.BlockCopy(first, 0, rv, 0, first.Length);
        
        // 3. 將第二個陣列複製到新陣列中第一個陣列之後的位置
        //    目標起始位置設為 first.Length，剛好接在第一個陣列後面
        Buffer.BlockCopy(second, 0, rv, first.Length, second.Length);
        
        // 4. 回傳合併後的陣列
        return rv; 
    }

    /// <summary>
    /// 從位元組陣列中取出子集（切割陣列）
    /// 
    /// 常用於從合併的資料中還原出各個組成部分
    /// 例如：從「鹽 + 雜湊值」的合併陣列中取出鹽的部分
    /// </summary>
    /// <param name="bytes">原始位元組陣列</param>
    /// <param name="startIndex">起始索引（包含）</param>
    /// <param name="endIndex">結束索引（不包含，類似 Python 的 slice 語法）</param>
    /// <returns>從 startIndex 到 endIndex-1 的子陣列</returns>
    /// <example>
    /// byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
    /// byte[] sub = data.SubSet(1, 4); // 結果：{ 0x02, 0x03, 0x04 }
    /// </example>
    /// <remarks>
    /// 注意：endIndex 參數是「不包含」的設計，這是為了與 C# 的範圍語法保持一致
    /// 也讓計算長度更方便：長度 = endIndex - startIndex
    /// </remarks>
    public static byte[] SubSet(this byte[] bytes, int startIndex = 0, int endIndex = 0)
    {
        // 使用 LINQ 的 Skip 跳過 startIndex 個元素
        // 再用 Take 取 (endIndex - startIndex) 個元素
        // 最後用 ToArray 將 IEnumerable<byte> 轉回 byte[]
        // 
        // 注意：這種實作方式雖然簡潔，但效能較差（因為會產生多個迭代器）
        // 如果是高效能場景，建議改用 Array.Copy 或 Buffer.BlockCopy
        return bytes.Skip(startIndex).Take(endIndex - startIndex).ToArray();
    }
    
    /// <summary>
    /// 將字串轉換為 UTF-8 編碼的位元組陣列
    /// 
    /// 為什麼用 UTF-8？
    /// 1. UTF-8 是網際網路最通用的編碼，相容性最好
    /// 2. 對於 ASCII 字元只佔用 1 byte，節省空間
    /// 3. .NET 內部字串是 UTF-16，但外部交換常用 UTF-8
    /// </summary>
    /// <param name="text">要轉換的原始字串</param>
    /// <returns>UTF-8 編碼的位元組陣列</returns>
    /// <example>
    /// string text = "Hello 世界";
    /// byte[] bytes = text.StringToBytes(); // UTF-8 編碼的位元組
    /// </example>
    public static byte[] StringToBytes(this string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
    
    /// <summary>
    /// 將 UTF-8 編碼的位元組陣列還原為字串
    /// 
    /// 這是 StringToBytes 的逆向操作
    /// 必須確保原始位元組確實是 UTF-8 編碼，否則會出現亂碼
    /// </summary>
    /// <param name="bytes">UTF-8 編碼的位元組陣列</param>
    /// <returns>解碼後的字串</returns>
    /// <example>
    /// byte[] bytes = { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
    /// string text = bytes.BytesToString(); // "Hello"
    /// </example>
    public static string BytesToString(this byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}