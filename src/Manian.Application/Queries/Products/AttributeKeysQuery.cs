using Manian.Application.Models;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢屬性鍵列表的請求物件
/// 
/// 用途：
/// - 查詢符合條件的屬性鍵列表
/// - 支援多種篩選條件（狀態、ID 列表）
/// - 支援關鍵字搜尋
/// - 支援游標分頁（Keyset Pagination）
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<AttributeKey>>，表示這是一個查詢請求
/// - 回傳 Pagination<AttributeKey> 包含資料列表和游標
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 AttributeKeysQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 屬性管理頁面
/// - 屬性下拉選單
/// - 屬性統計報表
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination（基於 SortOrder 的游標分頁）
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 Cursor 參數記錄上一頁最後一筆的 SortOrder
/// 
/// 注意事項：
/// - Search 只搜尋屬性名稱和描述（不含代碼）
/// - Ids 參數優先級最高，一旦提供就忽略其他條件
/// - Size 預設值為 1000，最大值為 1000
/// 
/// 參考實作：
/// - BrandsQuery：類似的實作模式，用於查詢品牌列表
/// - CategoriesQuery：類似的實作模式，用於查詢類別列表
/// </summary>
public class AttributeKeysQuery : IRequest<Pagination<AttributeKey>>
{
    /// <summary>
    /// 每頁資料筆數（可選）
    /// 
    /// 預設值：1000
    /// 最大值：1000
    /// 
    /// 設計考量：
    /// - 設定較大的預設值減少請求次數
    /// - 限制最大值防止一次性載入過多資料
    /// - 適合前端實作無限滾動（Infinite Scroll）
    /// 
    /// 使用建議：
    /// - 一般列表：使用預設值 1000
    /// - 行動裝置：可考慮降低至 50-100
    /// - 匯出功能：不應使用此參數
    /// 
    /// 範例：
    /// - Size = 10：每頁回傳 10 筆資料
    /// - Size = null：回傳所有符合條件的資料（不限制筆數）
    /// </summary>
    public int? Size { get; set; }
    
    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 實現 Keyset Pagination（游標分頁）
    /// - 記錄上一頁最後一筆資料的 SortOrder
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 SortOrder
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 SortOrder 大於此值的資料
    /// 
    /// 為什麼用 SortOrder 而不是 Id？
    /// - SortOrder 更有業務意義（按排序順序）
    /// - 避免暴露內部 Id 給前端
    /// - 支援自訂排序順序
    /// 
    /// 注意事項：
    /// - 首次查詢時應傳 null（取得第一頁）
    /// - 必須配合 OrderBy 使用
    /// - 實作中會轉換為整數進行比較
    /// </summary>
    public string? Cursor { get; set; }
    
    /// <summary>
    /// 搜尋關鍵字（可選）
    /// 
    /// 用途：
    /// - 在屬性名稱和描述中搜尋包含關鍵字的屬性鍵
    /// - 支援模糊搜尋
    /// 
    /// 搜尋範圍：
    /// - 屬性名稱（Name）
    /// - 屬性描述（Description）
    /// - 注意：目前實作不搜尋屬性代碼（Code）
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫（查詢時會轉為小寫）
    /// - 支援模糊搜尋（使用 Contains）
    /// - 會自動去除前後空白
    /// 
    /// 使用範例：
    /// - "color" → 會找到名稱或描述中包含 "color" 的屬性鍵
    /// - "COLOR" → 同樣會找到（不區分大小寫）
    /// - " color " → 會自動去除前後空白後搜尋
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// 屬性狀態（可選）
    /// 
    /// 用途：
    /// - 篩選指定狀態的屬性鍵
    /// 
    /// 可選值：
    /// - "active"：啟用狀態
    /// - "inactive"：停用狀態
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 Status 等於此值的屬性鍵
    /// - 如果為 null，回傳所有狀態的屬性鍵（不篩選狀態）
    /// 
    /// 使用場景：
    /// - 只顯示啟用的屬性鍵（Status = "active"）
    /// - 查詢已停用的屬性鍵（Status = "inactive"）
    /// 
    /// 範例：
    /// - Status = "active"：只回傳啟用的屬性鍵
    /// - Status = null：回傳所有狀態的屬性鍵
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 屬性鍵 ID 列表（可選）
    /// 
    /// 用途：
    /// - 篩選指定的屬性鍵
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 ID 在此列表中的屬性鍵
    /// - 如果為 null 或空陣列，不進行 ID 篩選
    /// 
    /// 優先級：
    /// - 此參數優先級最高
    /// - 一旦提供 Ids，會忽略 Search、Status、Cursor 等其他條件
    /// 
    /// 使用場景：
    /// - 查詢多個指定的屬性鍵
    /// - 根據類別關聯查詢屬性鍵
    /// - 批次查詢屬性鍵資訊
    /// 
    /// 範例：
    /// - Ids = [1, 5, 8]：只回傳 ID 為 1、5、8 的屬性鍵
    /// - Ids = null：不進行 ID 篩選
    /// - Ids = []：不進行 ID 篩選（空陣列）
    /// </summary>
    public int[]? Ids { get; set; }

    /// <summary>
    /// 類別 ID（可選）
    /// 
    /// 用途：
    /// - 查詢指定類別關聯的所有屬性鍵
    /// - 用於商品發布時顯示該類別可用的屬性選項
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳該類別關聯的屬性鍵
    /// - 如果為 null，回傳所有屬性鍵（不進行類別篩選）
    /// 
    /// 優先級：
    /// - 此參數優先級高於 Ids、Search、Status、Cursor
    /// - 一旦提供 CategoryId，會忽略其他所有條件
    /// 
    /// 使用場景：
    /// - 商品發布頁面：根據選擇的類別動態載入屬性選項
    /// - 類別管理：查看某個類別關聯的所有屬性
    /// - 屬性配置：為類別分配屬性鍵
    /// 
    /// 範例：
    /// - CategoryId = 5：回傳類別 ID 為 5 的所有關聯屬性鍵
    /// - CategoryId = null：不進行類別篩選
    /// 
    /// 實作說明：
    /// - 使用 IAttributeKeyRepository.GetCategoryAttributesAsync() 方法
    /// - 見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs
    /// - 查詢 CategoryAttribute 聯結表取得關聯的屬性鍵
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 是否僅供銷售使用（可選）
    /// 
    /// 用途：
    /// - 篩選僅供銷售使用的屬性鍵
    /// - 用於商品發布時顯示可用的屬性選項
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 ForSales 等於此值的屬性鍵
    /// - 如果為 null，回傳所有 ForSales 狀態的屬性鍵（不進行篩選）
    /// 
    /// 使用場景：
    /// - 商品發布頁面：根據選擇的類別動態載入屬性選項
    /// - 類別管理：查看某個類別關聯的所有屬性
    /// - 屬性配置：為類別分配屬性鍵
    /// 
    /// 範例：
    /// - ForSales = true：回傳僅供銷售使用的屬性鍵
    /// - ForSales = null：不進行 ForSales 篩選
    /// </summary>
    public bool? ForSales { get; set; }
}

/// <summary>
/// 屬性鍵查詢處理器
/// 
/// 職責：
/// - 接收 AttributeKeysQuery 請求
/// - 建構查詢條件（搜尋、篩選、分頁）
/// - 從資料庫取得符合條件的屬性鍵
/// - 回傳 Pagination<AttributeKey> 包含資料列表和游標
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeKeysQuery, Pagination<AttributeKey>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例（Transient）
/// 
/// 測試性：
/// - 可輕易 Mock IAttributeKeyRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - Ids 參數優先級過高，可能不符合預期
/// - Search 只搜尋名稱和描述，不搜尋代碼
/// - CategoryId 路徑不支援分頁
/// </summary>
public class AttributeKeysQueryHandler : IRequestHandler<AttributeKeysQuery, Pagination<AttributeKey>>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性鍵資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/AttributeKeyRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly IAttributeKeyRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">屬性鍵倉儲，用於查詢資料</param>
    public AttributeKeysQueryHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理屬性鍵查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 判斷是否為類別查詢路徑（有 CategoryId）
    /// 2. 若是，呼叫專用方法並忽略其他參數
    /// 3. 若否，建構通用查詢條件
    /// 4. 執行查詢並封裝結果
    /// 
    /// 查詢特性：
    /// - 支援多條件組合篩選
    /// - 支援關鍵字搜尋
    /// - 使用 Keyset Pagination 分頁
    /// - 按 SortOrder 排序
    /// </summary>
    /// <param name="request">屬性鍵查詢請求物件，包含搜尋、篩選、分頁參數</param>
    /// <returns>包含資料列表和游標的 Pagination<AttributeKey></returns>
    public async Task<Pagination<AttributeKey>> HandleAsync(AttributeKeysQuery request)
    {
        IEnumerable<AttributeKey> keys;

        // ========== 路徑一：查詢指定類別的屬性鍵 ==========
        // 當請求中包含 CategoryId 時，執行此路徑
        // 優先級最高，會忽略其他所有查詢條件（Ids、Search、Status、Cursor、Size）
        if(request.CategoryId.HasValue)
        {
            // 呼叫 Repository 的 GetCategoryAttributesAsync 方法
            // 見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs
            // 查詢 CategoryAttribute 聯結表，取得該類別關聯的所有屬性鍵
            // 
            // 使用場景：
            // - 商品發布頁面：根據選擇的類別動態載入屬性選項
            // - 類別管理：查看某個類別關聯的所有屬性
            // 
            // 注意事項：
            // - 此路徑不應用任何搜尋、篩選、分頁條件
            // - 返回該類別的所有關聯屬性鍵（無排序限制）
            keys = await _repository.GetCategoryAttributesAsync(request.CategoryId.Value,request.ForSales);
        }
        // ========== 路徑二：通用查詢（應用搜尋、篩選、分頁） ==========
        // 當請求中未指定 CategoryId 時，執行此路徑
        // 會應用 BuildQuery 中定義的所有查詢條件
        else
        {
            // 呼叫 BuildQuery 建構查詢條件
            // BuildQuery 會根據 request 中的參數動態組合 LINQ 查詢表達式
            // 
            // 查詢條件應用順序（見 BuildQuery 方法）：
            // 1. ID 篩選（Ids）- 優先級最高
            // 2. 搜尋條件（Search）- 在 Name 和 Description 中模糊搜尋
            // 3. 狀態篩選（Status）- 篩選 active/inactive
            // 4. 分頁條件（Cursor）- Keyset Pagination
            // 5. 排序（OrderBy）- 按 SortOrder 排序
            // 6. 數量限制（Size）- 限制回傳筆數
            // 
            // 將建構好的查詢表達式傳給 Repository 的 GetAllAsync 方法執行
            keys = await _repository.GetAllAsync(BuildQuery(request));
        }

        // 建立回傳物件
        // 使用建構函式自動計算 Cursor，無需手動判斷
        return new Pagination<AttributeKey>(
            items: keys,
            requestedSize: request.Size,
            cursorSelector: k => k.SortOrder.ToString()  
        );
    }

    /// <summary>
    /// 建構屬性鍵查詢表達式
    /// 
    /// 職責：
    /// - 將請求參數轉換為 LINQ 查詢表達式
    /// - 組合搜尋、篩選、排序、分頁條件
    /// 
    /// 設計優勢：
    /// - 將查詢邏輯集中管理
    /// - 保持 Repository 介面簡潔
    /// - 方便單元測試（可測試查詢邏輯）
    /// 
    /// 查詢流程：
    /// 1. 應用 ID 篩選（如果有 Ids）
    /// 2. 應用搜尋條件（如果有 Search）
    /// 3. 應用狀態篩選（如果有 Status）
    /// 4. 應用分頁條件（如果有 Cursor）
    /// 5. 按 SortOrder 排序
    /// 6. 限制回傳筆數（如果有 Size）
    /// </summary>
    /// <param name="request">屬性鍵查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<AttributeKey>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<AttributeKey>
    /// </returns>
    private static Func<IQueryable<AttributeKey>, IQueryable<AttributeKey>> BuildQuery(AttributeKeysQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用 ID 篩選（優先級最高）=====
            if(request.Ids != null && request.Ids.Length > 0)
            {
                // 篩選出 ID 存在於 request.Ids 陣列中的屬性鍵
                query = query.Where(x => request.Ids.Contains(x.Id));
                
                // 直接回傳，不繼續執行下面的搜尋、狀態、分頁條件
                // 這是一個設計選擇，但可能不符合預期
                return query;
            }

            // ===== 第二階段：應用搜尋條件 =====
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                // 1. 去除前後空白並轉為小寫
                var searchTerm = request.Search.Trim().ToLower();
                
                // 2. 在 Name 和 Description 欄位中進行模糊搜尋
                //    使用 OR 邏輯，只要任一欄位包含關鍵字即符合
                //    ToLower() 確保不區分大小寫
                query = query.Where(x => 
                    x.Name.ToLower().Contains(searchTerm) || 
                    (x.Description != null && x.Description.ToLower().Contains(searchTerm))
                );
            }
            
            // ===== 第三階段：應用狀態篩選 =====
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                // 篩選指定狀態的屬性鍵
                // "active"：啟用狀態
                // "inactive"：停用狀態
                query = query.Where(x => x.Status.ToString().ToLower() == request.Status);
            }

            // ===== 第四階段：應用分頁條件 =====
            if(request.ForSales != null)
            {
                // 篩選是否適用於銷售的屬性鍵
                // true：適用於銷售
                // false：不適用於銷售
                query = query.Where(x => x.ForSales == request.ForSales.Value);
            }
            
            // ===== 第五階段：應用分頁條件 =====
            // Keyset Pagination：只取 SortOrder 大於上一頁最後一筆的資料
            if (request.Cursor != null)
            {
                // 將 Cursor 轉換為整數
                // 然後篩選 SortOrder 大於此值的屬性鍵
                query = query.Where(x => x.SortOrder > int.Parse(request.Cursor));
            }
            
            // 按 SortOrder 排序（數字越小越前面）
            // 若 SortOrder 相同，則按 CreatedAt 降序排列（新建立的在前）
            query = query.OrderBy(c => c.SortOrder).ThenByDescending(c => c.CreatedAt);

            // ===== 第六階段：限制回傳筆數 =====
            if (request.Size != null)
            {
                // 只取前 Size 筆資料
                query = query.Take(request.Size.Value);
            }
            
            // 回傳最終組合好的查詢表達式
            // 注意：此時還沒真正執行查詢，要到被 foreach 或 ToList() 時才會執行
            return query;
        };
    }
}
