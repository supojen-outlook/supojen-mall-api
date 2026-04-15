using Manian.Domain.Entities.Carts;
using Manian.Domain.Entities.Products;

namespace Manian.Domain.ValueObjects;

/// <summary>
/// 折扣計算結果（值物件）
/// 
/// 職責：
/// - 封裝促銷折扣計算的結果資訊
/// - 提供折扣相關的唯讀資料結構
/// 
/// 設計原則：
/// - 作為值物件 (Value Object)，強調不可變性
/// - 與實體 (Entity) 分離，專注於資料傳遞
/// - 放置於 ValueObjects 資料夾，符合 DDD 分層架構
/// 
/// 使用場景：
/// - 促銷計算服務 (PromotionCalculationService) 的返回值
/// - 購物車折扣查詢 (DiscountQuery) 的回應資料
/// - 前端展示折扣資訊的資料傳輸物件
/// </summary>
public record Discount
{
    /// <summary>
    /// 促銷活動 ID
    /// 
    /// 用途：
    /// - 識別折扣來源於哪個促銷活動
    /// - 可用於追蹤促銷效果
    /// - 關聯至 Promotion 實體
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// 促銷活動名稱
    /// 
    /// 用途：
    /// - 前端展示促銷活動名稱
    /// - 提供使用者友善的折扣說明
    /// - 範例：「雙11全館88折」、「滿千送百」
    /// </summary>
    public string PromotionName { get; set; }

    /// <summary>
    /// 促銷規則 ID
    /// 
    /// 用途：
    /// - 識別折扣來源於哪個具體規則
    /// - 支援多規則促銷活動的精確追蹤
    /// - 關聯至 PromotionRule 實體
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// 促銷規則類型
    /// 
    /// 可能值：
    /// - full_reduction：滿額減（滿 X 元減 Y 元）
    /// - discount：折扣（滿 X 元打 Y 折）
    /// - gift：贈品（滿 X 件送 Y 件）
    /// - free_shipping：免運（滿 X 元免運費）
    /// 
    /// 用途：
    /// - 決定折扣計算方式
    /// - 前端展示不同類型的折扣資訊
    /// - 系統邏輯分支判斷
    /// </summary>
    public string RuleType { get; set; }

    /// <summary>
    /// 規則標籤名稱
    /// 
    /// 用途：
    /// - 前端展示規則名稱
    /// - 提供更具體的折扣說明
    /// - 範例：「滿千送百」、「雙11折扣」
    /// 
    /// 與 PromotionName 的區別：
    /// - PromotionName：促銷活動整體名稱
    /// - TabName：具體規則的名稱（一個活動可能有多個規則）
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// 折扣金額
    /// 
    /// 計算方式：
    /// - full_reduction：直接返回 DiscountAmount
    /// - discount：總金額 × 折扣率
    /// - gift：0（贈品無金額折扣）
    /// - free_shipping：0（免運無金額折扣）
    /// 
    /// 用途：
    /// - 計算訂單最終金額
    /// - 展示折扣金額給使用者
    /// - 排序促銷優先級（折扣金額高者優先）
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 是否免運
    /// 
    /// 說明：
    /// - 當 RuleType = 'free_shipping' 時為 true
    /// - 其他情況為 false
    /// 
    /// 用途：
    /// - 判斷訂單是否享有免運優惠
    /// - 前端顯示免運標示
    /// - 計算訂單運費
    /// </summary>
    public bool IsFreeShipping { get; set; }

    /// <summary>
    /// 贈品商品 ID
    /// 
    /// 說明：
    /// - 當 RuleType = 'gift' 時有值
    /// - 其他情況為 null
    /// 
    /// 用途：
    /// - 識別贈品商品
    /// - 關聯至 Product 實體
    /// - 前端展示贈品資訊
    /// 
    /// 注意事項：
    /// - 需搭配 GiftQuantity 使用
    /// - 贈品數量由門檻最高的 Gift 規則決定
    /// </summary>
    public int? GiftItemId { get; set; }

    /// <summary>
    /// 適用的購物車項目
    /// 
    /// 用途：
    /// - 識別哪些商品享有此折扣
    /// - 支援部分商品折扣的精確計算
    /// - 前端展示折扣適用範圍
    /// 
    /// 設計考量：
    /// - 使用 IEnumerable<CartItem> 避免修改原始資料
    /// - 支援多種促銷範圍（商品、分類、品牌）
    /// - 與 PromotionScope 實體關聯
    /// </summary>
    public IEnumerable<CartItem> ApplicableItems { get; set; }
}
