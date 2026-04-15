namespace Manian.Application.Models.Products;

/// <summary>
/// 產品基礎資料模型
/// 
/// 用途：
/// - 作為產品 相關 DTO (Data Transfer Object) 的基類
/// - 封裝產品的核心欄位，避免在不同 DTO (如 ProductDetail, ProductListItem) 中重複定義
/// - 定義了產品實體在 Application 層傳遞時的標準結構
/// 
/// 設計原則：
/// - 包含產品最基本且通用的屬性
/// - 不包含複雜的導航屬性 (如 Category, Brand 物件)，僅保留 ID
/// - 使用可讀性高的屬性名稱
/// </summary>
public class ProductBase
{
    /// <summary>
    /// 產品 ID
    /// 
    /// 說明：
    /// - 產品在資料庫中的唯一識別碼
    /// - 通常由資料庫自動生成 (主鍵)
    /// 
    /// 類型：int
    /// - 適用於自增長 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 產品編號 (SPU Code)
    /// 
    /// 說明：
    /// - 標準產品單元 的編碼
    /// - 用於系統間對接、庫存管理或訂單關聯
    /// - 應在系統內保持唯一性
    /// 
    /// 類型：string
    /// - 支援字母數字組合，比純數字 ID 更具業務意義
    /// </summary>
    public string SpuCode { get; set; }

    /// <summary>
    /// 產品名稱
    /// 
    /// 說明：
    /// - 產品的顯示名稱
    /// - 用於列表頁、詳情頁展示
    /// 
    /// 類型：string
    /// - 支援多語言或特殊字符
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 產品價格
    /// 
    /// 說明：
    /// - 產品的基礎售價
    /// - 注意：通常不包含折扣後的價格，折扣價格應由計算邏輯得出
    /// 
    /// 類型：decimal
    /// - 使用 decimal 而非 double 或 float 以確保金額計算的精確度，避免浮點數誤差
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 類別 ID (可選)
    /// 
    /// 說明：
    /// - 產品所屬的分類 ID
    /// - 對應到 Category 實體
    /// 
    /// 類型：int? (可為空)
    /// - 允許 null 表示產品尚未分類
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 品牌 ID (可選)
    /// 
    /// 說明：
    /// - 產品所屬的品牌 ID
    /// - 對應到 Brand 實體
    /// 
    /// 類型：int? (可為空)
    /// - 允許 null 表示產品未關聯品牌
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// 主圖 URL (可選)
    /// 
    /// 說明：
    /// - 產品主要展示圖片的網址
    /// - 用於列表縮圖或詳情頁首圖
    /// 
    /// 類型：string? (可為空)
    /// - 允許 null 表示尚未上傳圖片
    /// </summary>
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// 標籤陣列
    /// 
    /// 說明：
    /// - 用於標記產品特性的關鍵字 (如 "熱銷", "新品", "限時")
    /// - 可用於前端篩選或推薦邏輯
    /// 
    /// 類型：string[]
    /// - 陣列形式，支援一個產品擁有多個標籤
    /// </summary>
    public string[] Tags { get; set; }

    /// <summary>
    /// 產品狀態
    /// 
    /// 說明：
    /// - 表示產品當前的生命週期狀態
    /// 
    /// 常見值：
    /// - "active"：上架 (可購買)
    /// - "inactive"：下架 (不可購買)
    /// - "draft"：草稿 (未發布)
    /// 
    /// 類型：string
    /// - 使用字串列舉而非 int，提高可讀性與除錯便利性
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 建立時間
    /// 
    /// 說明：
    /// - 產品資料建立的時間戳
    /// - 用於排序、日誌記錄或數據分析
    /// 
    /// 類型：DateTimeOffset
    /// - 比 DateTime 更適合處理跨時區的場景
    /// - 包含 UTC 時間與時區偏移量資訊
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
