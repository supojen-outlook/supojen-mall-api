using System.ComponentModel.DataAnnotations;
using Manian.Domain.ValueObjects;
using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 商品實體 (SPU - Standard Product Unit)
/// 用途：定義商品的核心資訊，包含基本資料、價格、分類、多媒體等
/// 設計考量：
/// - 與 SKU 表為一對多關係，一個商品可以有多個規格組合
/// - price 為基礎價格，實際售價可由 SKU 覆蓋
/// - specs 使用 List<Specification> 存儲彈性規格參數
/// - tags 使用字串陣列儲存商品標籤
/// - detail_images 使用字串陣列儲存多張圖片
/// </summary>
public class Product : IEntity
{
    /// <summary>
    /// 商品唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// SPU 編碼，用於商品識別
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 長度限制：1-50 字元
    /// - 全域唯一（由資料庫唯一約束保證）
    /// 
    /// 範例：
    /// - "SPU-CERAMIC-001"：陶瓷類別的第一個商品
    /// - "SPU-GLASS-001"：玻璃類別的第一個商品
    /// </summary>
    public string SpuCode { get; set; }

    /// <summary>
    /// 商品顯示名稱
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 長度限制：1-200 字元
    /// 
    /// 範例：
    /// - "青花瓷茶具組"：商品名稱
    /// - "手工玻璃花器"：商品名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 商品詳細描述
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 商品詳細頁面
    /// - SEO 優化
    /// - 商品搜尋
    /// 
    /// 注意事項：
    /// - 建議長度限制：0-5000 字元
    /// - 可包含 HTML 標籤（需注意 XSS 防護）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 商品基礎價格
    /// 
    /// 用途：
    /// - SKU 價格可覆蓋此價格
    /// - 用於價格計算和比較
    /// 
    /// 驗證規則：
    /// - 必須大於等於 0
    /// - 精度：小數點後兩位
    /// 
    /// 範例：
    /// - 2990.00：商品價格
    /// - 5800.50：商品價格
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 所屬類別 ID
    /// 
    /// 用途：
    /// - 關聯到 categories 表
    /// - 用於商品分類和篩選
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 商品分類顯示
    /// - 商品篩選功能
    /// - 商品推薦系統
    /// 
    /// 注意事項：
    /// - 刪除類別時設為 NULL
    /// - 建議在 UI 層提供類別選擇器
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 所屬品牌 ID
    /// 
    /// 用途：
    /// - 關聯到 brands 表
    /// - 用於品牌篩選和顯示
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 品牌專區顯示
    /// - 品牌篩選功能
    /// - 品牌推薦系統
    /// 
    /// 注意事項：
    /// - 刪除品牌時設為 NULL
    /// - 建議在 UI 層提供品牌選擇器
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// 商品主圖網址
    /// 
    /// 用途：
    /// - 商品列表顯示
    /// - 商品詳細頁主圖
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 商品列表頁
    /// - 商品詳細頁
    /// - 搜尋結果頁
    /// 
    /// 注意事項：
    /// - 建議長度限制：0-500 字元
    /// - 建議使用 CDN 加速
    /// - 建議提供多尺寸圖片
    /// </summary>
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// 商品詳情圖陣列
    /// 
    /// 用途：
    /// - 商品詳細頁圖片輪播
    /// - 商品詳細頁圖片展示
    /// 
    /// 預設值：
    /// - 空陣列
    /// 
    /// 使用場景：
    /// - 商品詳細頁
    /// - 商品圖片輪播
    /// - 商品圖片放大查看
    /// 
    /// 注意事項：
    /// - 建議最多 10 張圖片
    /// - 建議使用 CDN 加速
    /// - 建議提供多尺寸圖片
    /// </summary>
    public string[] DetailImages { get; set; }

    /// <summary>
    /// 商品介紹影片網址
    /// 
    /// 用途：
    /// - 商品詳細頁影片播放
    /// - 商品推廣影片
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 使用場景：
    /// - 商品詳細頁
    /// - 商品推廣頁
    /// - 社群媒體分享
    /// 
    /// 注意事項：
    /// - 建議長度限制：0-500 字元
    /// - 建議使用影片平台（YouTube、Vimeo）
    /// - 建議提供多解析度影片
    /// </summary>
    public string? VideoUrl { get; set; }

    /// <summary>
    /// 商品規格參數
    /// 
    /// 用途：
    /// - 儲存彈性規格參數
    /// - 用於商品篩選和比較
    /// 
    /// 預設值：
    /// - 空列表
    /// 
    /// 使用場景：
    /// - 商品詳細頁規格展示
    /// - 商品篩選功能
    /// - 商品比較功能
    /// 
    /// 範例：
    /// - [{"KeyId":"1", "ValueId":"100", "Name":"重量", "Value":"1.2kg", "Unit":"kg"}]
    /// - [{"KeyId":"2", "ValueId":"200", "Name":"材質", "Value":"陶瓷", "Unit":null}]
    /// 
    /// 注意事項：
    /// - 使用 List<Specification> 存儲
    /// - 建議提供規格範本
    /// - 建議在 UI 層提供規格編輯器
    /// </summary>
    public List<Specification> Specs { get; set; }

    /// <summary>
    /// 商品標籤
    /// 
    /// 用途：
    /// - 商品標籤分類
    /// - 商品推薦系統
    /// 
    /// 預設值：
    /// - 空陣列
    /// 
    /// 使用場景：
    /// - 商品標籤篩選
    /// - 商品推薦系統
    /// - 行銷活動選品
    /// 
    /// 範例：
    /// - ["新品", "手工製作", "限量"]
    /// - ["熱銷", "職人手作"]
    /// 
    /// 注意事項：
    /// - 建議最多 10 個標籤
    /// - 建議使用統一的標籤系統
    /// - 建議在 UI 層提供標籤選擇器
    /// </summary>
    public string[] Tags { get; set; }

    /// <summary>
    /// 商品狀態
    /// 
    /// 可選值：
    /// - "draft"：草稿
    /// - "pending"：審核中
    /// - "active"：上架
    /// - "inactive"：下架
    /// 
    /// 預設值：
    /// - "draft"
    /// 
    /// 使用場景：
    /// - 商品上架流程
    /// - 商品審核流程
    /// - 商品下架流程
    /// 
    /// 注意事項：
    /// - 只能接受 "draft"、"pending"、"active" 或 "inactive" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// </summary>
    private string _status = "draft";

    /// <summary>
    /// 商品狀態：draft草稿/pending審核中/active上架/inactive下架
    /// 
    /// 驗證規則：
    /// - 只能接受 "draft"、"pending"、"active" 或 "inactive" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// product.Status = "draft";    // 正確
    /// product.Status = "pending";  // 正確
    /// product.Status = "active";   // 正確
    /// product.Status = "inactive"; // 正確
    /// product.Status = "deleted";  // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "draft"、"pending"、"active" 或 "inactive" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "draft" && value != "pending" && value != "active" && value != "inactive")
                throw new ArgumentException("Status 必須是 'draft'、'pending'、'active' 或 'inactive'");
            
            _status = value;
        }
    }

    /// <summary>
    /// 商品建立時間
    /// 
    /// 預設值：
    /// - 目前 UTC 時間
    /// 
    /// 使用場景：
    /// - 商品排序
    /// - 商品統計
    /// - 商品分析
    /// 
    /// 注意事項：
    /// - 使用協調世界時 (UTC) 記錄建立時間
    /// - 避免時區問題，便於跨時區系統使用
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
