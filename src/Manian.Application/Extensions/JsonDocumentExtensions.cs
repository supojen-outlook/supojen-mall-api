using System.Text.Json;

namespace Manian.Application.Extensions;

/// <summary>
/// JsonDocument 的擴充方法，提供深層複製的功能
/// </summary>
internal static class JsonDocumentExtensions
{
    /// <summary>
    /// 建立 JsonDocument 的深層副本
    /// </summary>
    /// <param name="original">要複製的原始 JsonDocument</param>
    /// <returns>全新的 JsonDocument 副本</returns>
    /// <remarks>
    /// 這個方法很重要，因為 JsonDocument 本身是無法直接複製的，
    /// 而且為了避免生命週期管理的問題，我們需要建立獨立的副本。
    /// </remarks>
    public static JsonDocument CopyJsonDocument(JsonDocument original)
    {
        // 步驟 1: 建立 MemoryStream 來暫存 JSON 資料
        // using 確保 stream 在使用完後會被正確釋放
        using var stream = new MemoryStream();
        
        // 步驟 2: 建立 Utf8JsonWriter 來寫入 JSON 到 stream
        // using 確保 writer 在使用完後會被正確釋放
        using (var writer = new Utf8JsonWriter(stream))
        {
            // 步驟 3: 將原始 JsonDocument 的根元素寫入 writer
            // WriteTo 方法會將整個 JSON 結構寫入，包括所有巢狀物件和陣列
            original.RootElement.WriteTo(writer);
            
            // 步驟 4: 強制將緩衝區的資料寫入底層的 stream
            // 確保所有資料都確實寫入 MemoryStream
            writer.Flush();
        }
        
        // 步驟 5: 將 stream 的位置重設到開頭
        // 因為剛剛寫入完後，stream 的位置在結尾，需要重設才能讀取
        stream.Position = 0;
        
        // 步驟 6: 從 stream 解析出全新的 JsonDocument
        // 這個新的 JsonDocument 完全獨立於原始的，有自己的生命週期
        return JsonDocument.Parse(stream);
    }
}