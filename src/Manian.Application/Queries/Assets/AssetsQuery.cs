using Manian.Application.Models;
using Manian.Domain.Entities.Assets;
using Manian.Domain.Repositories.Assets;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Assets;

/// <summary>
/// 查詢資產列表的請求物件
/// 
/// 用途：
/// - 查詢符合條件的媒體資產列表
/// - 支援依據 TargetType 與 TargetId 是否為空進行篩選
/// - 支援游標分頁（Keyset Pagination）
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Asset>>，表示這是一個查詢請求
/// - 回傳 Pagination<Asset> 包含資料列表和游標
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 AssetsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 查詢特定產品的所有圖片
/// - 查詢特定品牌的 Logo
/// - 清理未關聯 (孤兒) 的資源
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination（基於 Id 的游標分頁）
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 Cursor 參數記錄上一頁最後一筆的 Id
/// 
/// 注意事項：
/// - 固定使用 Id 作為排序依據，不支援自訂排序
/// - Cursor 為上一頁最後一筆資料的 Id
/// - IsTargetIdNull 用於決定查詢已關聯或未關聯的資源
/// 
/// 參考實作：
/// - BrandsQuery：類似的實作模式，用於查詢品牌列表
/// - ProductsQuery：類似的實作模式，用於查詢產品列表
/// </summary>
public class AssetsQuery : IRequest<Pagination<Asset>>
{
    /// <summary>
    /// 關聯目標類型（可選）
    /// 
    /// 用途：
    /// - 篩選特定類型的資源
    /// 
    /// 可選值：
    /// - "product"：產品
    /// - "category"：分類
    /// - "brand"：品牌
    /// - null：查詢所有類型
    /// 
    /// 篩選邏輯：
    /// - 如果提供此值，只回傳 TargetType 等於此值的資源
    /// - 如果為 null，回傳所有類型的資源（不篩選類型）
    /// 
    /// 範例：
    /// - TargetType = "product"：只回傳產品類型的資源
    /// - TargetType = null：回傳所有類型的資源
    /// </summary>
    public string? TargetType { get; init; }

    /// <summary>
    /// 是否篩選未關聯資源（可選）
    /// 
    /// 用途：
    /// - 決定查詢邏輯：true 查詢孤兒資源，false 查詢已關聯資源
    /// 
    /// 定義：
    /// - true：TargetId 為 null（未關聯）
    /// - false：TargetId 不為 null（已關聯）
    /// 
    /// 篩選邏輯：
    /// - 如果為 true，只回傳 TargetId 為 null 的資源
    /// - 如果為 false，只回傳 TargetId 不為 null 的資源
    /// - 如果為 null，不進行此篩選（回傳所有）
    /// 
    /// 使用場景：
    /// - IsTargetIdNull = true：查詢未關聯的資源（用於清理）
    /// - IsTargetIdNull = false：查詢已關聯的資源（用於顯示）
    /// 
    /// 範例：
    /// - IsTargetIdNull = true：只回傳孤兒資源
    /// - IsTargetIdNull = false：只回傳已關聯資源
    /// - IsTargetIdNull = null：回傳所有資源
    /// </summary>
    public bool? IsTargetIdNull { get; init; }

    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 實現 Keyset Pagination（游標分頁）
    /// - 記錄上一頁最後一筆資料的 Id
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 Id
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 Id 大於此值的資料
    /// 
    /// 為什麼用 Id？
    /// - Id 是主鍵，預設有索引，查詢效率高
    /// - Id 是遞增的，天然適合分頁
    /// - 避免使用 Skip 造成的大數據量效能問題
    /// 
    /// 注意事項：
    /// - 首次查詢時應傳 null（取得第一頁）
    /// - 必須配合 OrderBy 使用
    /// - 實作中會轉換為整數進行比較
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// 每頁資料筆數（可選）
    /// 
    /// 預設值：無（由 Repository 決定）
    /// 最大值：無（由呼叫端控制）
    /// 
    /// 設計考量：
    /// - 預設不限制筆數，方便批次處理
    /// - 建議前端傳入適當的 Size 以避免回傳過多資料
    /// - 適合前端實作無限滾動（Infinite Scroll）
    /// 
    /// 使用建議：
    /// - 一般列表：建議 20-50
    /// - 相簿展示：建議 10-20
    /// - 清理任務：可傳入較大值或 null
    /// 
    /// 範例：
    /// - Size = 10：每頁回傳 10 筆資料
    /// - Size = null：回傳所有符合條件的資料（不限制筆數）
    /// </summary>
    public int? Size { get; init; }
}

/// <summary>
/// 資產查詢處理器
/// 
/// 職責：
/// - 接收 AssetsQuery 請求
/// - 建構查詢條件（篩選、分頁）
/// - 從資料庫取得符合條件的資產
/// - 回傳 Pagination<Asset> 包含資料列表和游標
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AssetsQuery, Pagination<Asset>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 Repository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例（Transient）
/// 
/// 測試性：
/// - 可輕易 Mock IAssetRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查 TargetType 的有效性（可能輸入不存在的類型）
/// - 未處理 Cursor 格式錯誤的情況（非數字字串）
/// </summary>
public class AssetsQueryHandler : IRequestHandler<AssetsQuery, Pagination<Asset>>
{
    /// <summary>
    /// 資產倉儲介面
    /// 
    /// 用途：
    /// - 存取資產資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Assets/AssetRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly IAssetRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">資產倉儲，用於查詢資料</param>
    public AssetsQueryHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理資產查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 將結果轉換為 Pagination<Asset>
    /// 4. 設定游標為最後一筆資料的 Id
    /// 5. 回傳 Pagination<Asset>
    /// 
    /// 查詢特性：
    /// - 支援多條件組合篩選
    /// - 使用 Keyset Pagination 分頁
    /// - 按 Id 排序
    /// </summary>
    /// <param name="request">資產查詢請求物件，包含篩選與分頁參數</param>
    /// <returns>包含資料列表和游標的 Pagination<Asset></returns>
    public async Task<Pagination<Asset>> HandleAsync(AssetsQuery request)
    {
        // 呼叫 BuildQuery 建構查詢條件，然後傳給 Repository 執行
        var assets = await _repository.GetAllAsync(BuildQuery(request));
    
        // 建立回傳物件
        // 使用建構函式自動計算 Cursor，無需手動判斷
        var result = new Pagination<Asset>(
            items: assets,
            requestedSize: request.Size,
            cursorSelector: x => x.Id.ToString()
        );
        
        return result;
    }

    /// <summary>
    /// 建構資產查詢表達式
    /// 
    /// 職責：
    /// - 將請求參數轉換為 LINQ 查詢表達式
    /// - 組合篩選、排序、分頁條件
    /// 
    /// 設計優勢：
    /// - 將查詢邏輯集中管理
    /// - 保持 Repository 介面簡潔
    /// - 方便單元測試（可測試查詢邏輯）
    /// 
    /// 查詢流程：
    /// 1. 應用 TargetType 篩選（如果有 TargetType）
    /// 2. 應用 TargetId 篩選（如果有 IsTargetIdNull）
    /// 3. 應用分頁條件（如果有 Cursor）
    /// 4. 按 Id 排序
    /// 5. 限制回傳筆數（如果有 Size）
    /// </summary>
    /// <param name="request">資產查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Asset>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Asset>
    /// </returns>
    private static Func<IQueryable<Asset>, IQueryable<Asset>> BuildQuery(AssetsQuery request)
    {
        // 回傳一個委派，接受原始 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：應用 TargetType 篩選 =====
            if (!string.IsNullOrEmpty(request.TargetType))
            {
                // 篩選指定類型的資源
                // "product"：產品
                // "category"：分類
                // "brand"：品牌
                query = query.Where(x => x.TargetType == request.TargetType);
            }

            // ===== 第二階段：應用 TargetId 篩選 =====
            if (request.IsTargetIdNull.HasValue)
            {
                if (request.IsTargetIdNull.Value)
                {
                    // 若為 true，查詢未關聯的資源 (TargetId 為 null)
                    query = query.Where(x => x.TargetId == null);
                }
                else
                {
                    // 若為 false，查詢已關聯的資源 (TargetId 不為 null)
                    query = query.Where(x => x.TargetId != null);
                }
            }

            // ===== 第三階段：應用分頁條件 =====
            // Keyset Pagination：只取 Id 大於上一頁最後一筆的資料
            if (request.Cursor != null)
            {
                // 將 Cursor 轉換為整數
                // 然後篩選 Id 大於此值的資產
                if (int.TryParse(request.Cursor, out var cursorId))
                {
                    query = query.Where(x => x.Id > cursorId);
                }
                // 若轉換失敗（格式錯誤），則忽略 Cursor 條件（回傳第一頁）
            }

            // 按 Id 排序（數字越小越前面）
            query = query.OrderBy(x => x.Id);

            // ===== 第四階段：限制回傳筆數 =====
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
