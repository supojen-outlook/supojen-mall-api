namespace Manian.Application.Models;

/// <summary>
/// 泛型分頁類別，用於封裝分頁查詢結果
/// 
/// 設計目的：
/// 1. 統一分頁資料的回傳格式
/// 2. 支援 Keyset Pagination（游標分頁）模式
/// 3. 提供型別安全的泛型實作
/// 
/// 使用場景：
/// - 列表頁面的資料載入
/// - 無限滾動（Infinite Scroll）功能
/// - 需要分頁的 API 回應
/// 
/// 設計模式：
/// - 泛型類別：支援任意資料型別
/// - DTO 模式：專門用於資料傳輸
/// 
/// 與傳統分頁的差異：
/// - 傳統分頁：使用 PageNumber 和 PageSize
/// - 此類別：使用 Cursor（游標）實現更高效的分頁
/// 
/// 參考實作：
/// - UnitOfMeasuresQuery：使用 LastCreatedAt 作為游標
/// - RolesQuery：使用 LastId 作為游標
/// </summary>
/// <typeparam name="T">
/// 資料型別參數
/// 
/// 使用範例：
/// - Pagination<Product>：商品分頁
/// - Pagination<User>：用戶分頁
/// - Pagination<Order>：訂單分頁
/// </typeparam>
public class Pagination<T>
{
    /// <summary>
    /// 建構函式：初始化列表並自動計算 Cursor
    /// </summary>
    /// <param name="items">當前頁的數據列表</param>
    /// <param name="requestedSize">請求的數量</param>
    /// <param name="cursorSelector">選擇哪個屬性作為 Cursor (例如: x => x.SortOrder)</param>
    public Pagination(
        IEnumerable<T> items, 
        int? requestedSize, 
        Func<T, string?>? cursorSelector)
    {
        // 1. 將傳入的數據列表直接賦值給類別內部的 List 屬性。
        //    這樣外部就可以通過 pagination.List 獲取數據。
        List = items;

        // 2. 開始計算 Cursor 的邏輯。
        //    Cursor (遊標) 通常用於標記當前頁的最後一條數據，以便前端請求「下一頁」時使用。
        
        // 條件判斷：
        // requestedSize == null  -> 如果沒有指定每頁大小（通常意味著獲取全部），則需要計算 Cursor。
        // items.Count >= requestedSize -> 如果當前返回的數量 >= 請求的數量，說明可能還有下一頁數據，需要計算 Cursor。
        if (requestedSize == null || items.Count() >= requestedSize)
        {
            // 進入此區塊代表「可能還有下一頁」或「需要標記當前位置」。
            
            // 再次檢查列表中是否有數據
            if (items.Any())
            {
                // 獲取列表中的最後一項數據
                var lastItem = items.Last();

                // 核心邏輯：使用委派 cursorSelector 從最後一項中提取特定屬性值，並轉為字串。
                // 例如：cursorSelector 可能是 x => x.SortOrder，這裡就會取出最後一個項目的 SortOrder 值。
                Cursor = cursorSelector != null ? cursorSelector(lastItem)?.ToString() : null;
            }
            // 如果 items 是空的，Cursor 會保持為 null (默認值)
        }
        else
        {
            // 進入此區塊代表：requestedSize 有值 且 items.Count < requestedSize。
            // 這意味著請求了 10 筆，但只返回了 5 筆（或少於請求數），說明已經到達數據末尾了。
            // 因此，將 Cursor 設為 null，告訴前端「沒有下一頁了」。
            Cursor = null;
        }
    }

    /// <summary>
    /// 分頁資料集合
    /// 
    /// 職責：
    /// - 存儲當前頁面的資料項目
    /// - 支援任意型別的資料集合
    /// 
    /// 資料特性：
    /// - 可能為空集合（無資料時）
    /// - 筆數由查詢時的 Size 參數決定
    /// - 通常按時間或 ID 排序
    /// 
    /// 使用範例：
    /// <code>
    /// var products = new Pagination<Product>
    /// {
    ///     List = new List<Product> { product1, product2, product3 },
    ///     Cursor = "2023-01-01T00:00:00Z"
    /// };
    /// </code>
    /// 
    /// 注意事項：
    /// - 建議在 UI 層處理空集合情況
    /// - 不應修改此集合（唯讀設計會更好）
    /// </summary>
    public IEnumerable<T> List { get; set; }

    /// <summary>
    /// 游標字串（用於 Keyset Pagination）
    /// 
    /// 職責：
    /// - 記錄當前頁最後一筆資料的標識
    /// - 用於取得下一頁資料
    /// 
    /// 游標類型：
    /// - 時間戳記：如 "2023-01-01T00:00:00Z"
    /// - ID：如 "12345"
    /// - 編碼字串：如 "eyJpZCI6MTIzNDV9"
    /// 
    /// 工作原理：
    /// 1. 前端記錄當前頁最後一筆資料的游標
    /// 2. 下一頁請求時將此游標傳回後端
    /// 3. 後端根據游標查詢下一批資料
    /// 
    /// 使用範例：
    /// <code>
    /// // 第一次請求（無游標）
    /// GET /api/products?size=10
    /// 
    /// // 回應
    /// {
    ///   "list": [...],
    ///   "cursor": "2023-01-01T00:00:00Z"
    /// }
    /// 
    /// // 第二次請求（帶游標）
    /// GET /api/products?size=10&cursor=2023-01-01T00:00:00Z
    /// </code>
    /// 
    /// 注意事項：
    /// - 為 null 時表示沒有下一頁
    /// - 應加密或編碼以防止客戶端篡改
    /// - 前端不應解析此值，直接傳回即可
    /// </summary>
    public string? Cursor { get; set; }
}
