using System;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Assets;

/// <summary>
/// 資產類別，代表系統中的媒體資源（圖片或影片）。
/// 主要用於記錄上傳至 S3 的檔案資訊，並透過多態關聯 綁定至具體的業務實體（如產品、分類或品牌）。
/// </summary>
public class Asset : IEntity
{
    /// <summary>
    /// 資源的唯一識別碼。
    /// 對應資料庫的主鍵 (BIGSERIAL)。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 關聯目標的類型名稱。
    /// 用於多態關聯，指定此資源屬於哪種實體。
    /// 對應資料庫的 target_type (VARCHAR(50))。
    /// 範例值："product", "category", "brand", "user"。
    /// 若此值為 null，表示該資源目前尚未關聯至任何目標（孤兒資源），通常需由後台定時任務清理。
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// 關聯目標的 ID。
    /// 與 <see cref="TargetType"/> 配合使用，指向具體實體的主鍵 ID。
    /// 對應資料庫的 target_id (BIGINT)。
    /// 範例：若 TargetType 為 "product"，則此欄位為產品表的 ProductId。
    /// 若此值為 null，表示該資源目前尚未關聯至任何目標。
    /// </summary>
    public int? TargetId { get; set; }

    /// <summary>
    /// 媒體檔案的類型。
    /// 用於區分檔案格式，以便前端進行不同的渲染處理。
    /// 對應資料庫的 media_type (VARCHAR(20))。
    /// 範例值："image" (圖片), "video" (影片)。
    /// </summary>
    public string MediaType { get; set; }

    /// <summary>
    /// 檔案的公開存取網址。
    /// 通常由 S3 或 CDN 生成，可直接用於前端 <img> 或 <video> 標籤的 src 屬性。
    /// 對應資料庫的 url (VARCHAR(2048))。
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 檔案的大小，單位為位元組。
    /// 用於顯示檔案大小或進行存儲成本計算。
    /// 對應資料庫的 file_size_bytes (BIGINT)。
    /// </summary>
    public float FileSizeBytes { get; set; }

    /// <summary>
    /// AWS S3 (或其他物件存儲服務) 的存儲桶名稱。
    /// 用於定位檔案所在的具體存儲空間。
    /// 對應資料庫的 s3_bucket (VARCHAR(255))。
    /// </summary>
    public string Bucket { get; set; }

    /// <summary>
    /// AWS S3 (或其他物件存儲服務) 的物件鍵。
    /// 唯一標識存儲桶內的檔案，結合 Bucket 名稱即可完整定位檔案。
    /// 對應資料庫的 s3_key (VARCHAR(500))。
    /// 範例："uploads/2023/10/27/user_123_avatar.jpg"。
    /// </summary>
    public string Key { get; set; }
}
