using Manian.Application.Models;
using Manian.Domain.Entities.Warehouses;
using Manian.Domain.Repositories.Warehouses;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Warehouses;

/// <summary>
/// 查詢庫存列表的請求物件
/// 
/// 用途：
/// - 查詢指定 SKU 在所有儲位的庫存記錄
/// - 查詢指定儲位的所有庫存記錄
/// - 支援游標分頁（Keyset Pagination）
/// - 用於庫存總覽和庫存調度
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Inventory>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的庫存集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 InventoriesQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - SKU 庫存總覽（查看 SKU 在所有儲位的庫存）
/// - 儲位庫存總覽（查看儲位中所有 SKU 的庫存）
/// - 訂單處理時選擇最佳儲位出貨
/// - 庫存調度（從一個儲位移動到另一個儲位）
/// - 庫存報表生成
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination（基於 CreatedAt 的游標分頁）
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// - 實作：使用 Cursor 參數記錄上一頁最後一筆的 CreatedAt
/// 
/// 設計特點：
/// - 支援按 SKU ID 或儲位 ID 篩選（二選一）
/// - 支援游標分頁（基於 CreatedAt）
/// - 回傳標準化的 Pagination 模型，方便前端處理
/// 
/// 參考實作：
/// - RolesQuery：使用 LastId 作為游標的類似實作
/// - UnitOfMeasuresQuery：使用 LastCreatedAt 作為游標的類似實作
/// </summary>
public class InventoriesQuery : IRequest<Pagination<Inventory>>
{
    /// <summary>
    /// 儲位 ID（可選）
    /// 
    /// 用途：
    /// - 篩選指定儲位的庫存記錄
    /// - 必須是資料庫中已存在的儲位 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的儲位
    /// - 與 SkuId 二選一，不能同時為 null
    /// 
    /// 錯誤處理：
    /// - 如果儲位不存在，會返回空集合
    /// - 如果與 SkuId 同時為 null，會拋出 ArgumentException
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// SKU ID（可選）
    /// 
    /// 用途：
    /// - 識別要查詢的 SKU
    /// - 必須是資料庫中已存在的 SKU ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的 SKU
    /// - 與 LocationId 二選一，不能同時為 null
    /// 
    /// 錯誤處理：
    /// - 如果 SKU 不存在，會返回空集合
    /// - 如果與 LocationId 同時為 null，會拋出 ArgumentException
    /// </summary>
    public int? SkuId { get; set; }

    /// <summary>
    /// 游標（可選）
    /// 
    /// 用途：
    /// - 實現 Keyset Pagination（游標分頁）
    /// - 記錄上一頁最後一筆資料的 CreatedAt
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 CreatedAt
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 CreatedAt 大於此值的資料
    /// 
    /// 格式：
    /// - ISO 8601 格式的日期時間字串
    /// - 範例："2023-01-01T00:00:00Z"
    /// 
    /// 注意事項：
    /// - 首次查詢時應傳 null（取得第一頁）
    /// - 必須配合 OrderBy 使用
    /// - 實作中會轉換為 DateTimeOffset 進行比較
    /// </summary>
    public string? Cursor { get; set; }
    
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
}

/// <summary>
/// 庫存查詢處理器
/// 
/// 職責：
/// - 接收 InventoriesQuery 請求
/// - 根據 LocationId 或 SkuId 查詢庫存記錄
/// - 將查詢結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<InventoriesQuery, Pagination<Inventory>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// - 使用 BuildQuery 模式構建查詢邏輯
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ILocationRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 支援游標分頁（基於 CreatedAt）
/// - 統一回傳格式為 Pagination，方便前端處理
/// - 依賴 Repository 的實作細節
/// - 使用 BuildQuery 方法模式構建查詢邏輯
/// 
/// 參考實作：
/// - RolesQueryHandler：使用 BuildQuery 方法的類似實作
/// - UnitOfMeasuresQueryHandler：使用 BuildQuery 方法的類似實作
/// </summary>
public class InventoriesQueryHandler : IRequestHandler<InventoriesQuery, Pagination<Inventory>>
{
    /// <summary>
    /// 儲位倉儲介面
    /// 
    /// 用途：
    /// - 存取庫存資料
    /// - 提供查詢庫存的方法
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Warehouses/LocationRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 提供專用方法 GetInventoriesByLocationIdAsync 和 GetInventoriesBySkuIdAsync
    /// - 預設按 CreatedAt 排序（由 Repository 實作）
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Warehouses/ILocationRepository.cs
    /// </summary>
    private readonly ILocationRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">儲位倉儲，用於查詢庫存資料</param>
    public InventoriesQueryHandler(ILocationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理庫存查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證 LocationId 或 SkuId 至少有一個有值
    /// 2. 根據 LocationId 或 SkuId 選擇適當的查詢方法
    /// 3. 呼叫 BuildQuery 方法構建查詢邏輯
    /// 4. 呼叫 Repository 的方法取得資料
    /// 5. 將資料包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 根據 LocationId 或 SkuId 過濾庫存
    /// - 按 CreatedAt 排序（由 BuildQuery 實作）
    /// - 支援游標分頁（基於 CreatedAt）
    /// 
    /// 分頁邏輯：
    /// - 如果 Cursor 有值，只查詢 CreatedAt 大於 Cursor 的資料
    /// - 如果 Size 有值，限制回傳筆數
    /// - 如果 Size 為 null，回傳所有符合條件的資料
    /// 
    /// 排序說明：
    /// - 預設按 CreatedAt 升序排列
    /// - CreatedAt 越新越後面
    /// - 由 BuildQuery 實作
    /// 
    /// 錯誤處理：
    /// - 如果 LocationId 和 SkuId 都為 null，拋出 ArgumentException
    /// - 如果沒有庫存記錄，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 效能考量：
    /// - 使用游標分頁比傳統 Skip/Take 更有效率
    /// - 適合大量資料的場景
    /// - 可以考慮加入快取機制
    /// </summary>
    /// <param name="request">庫存查詢請求物件，包含 LocationId、SkuId、Cursor、Size</param>
    /// <returns>包含庫存記錄的分頁模型</returns>
    public async Task<Pagination<Inventory>> HandleAsync(InventoriesQuery request)
    {
        // ========== 第一步：驗證查詢條件 ==========
        // 必須提供 LocationId 或 SkuId 其中之一
        if (!request.LocationId.HasValue && !request.SkuId.HasValue)
        {
            throw new ArgumentException("必須提供 LocationId 或 SkuId");
        }

        IEnumerable<Inventory> inventories = new List<Inventory>();

        // ========== 第二步：根據條件選擇查詢方法 ==========
        if (request.LocationId.HasValue)
        {
            // 呼叫 Repository 的 GetInventoriesByLocationIdAsync 方法查詢庫存
            // 傳入 BuildQuery 方法構建的查詢邏輯
            // BuildQuery 會：
            // 1. 按 CreatedAt 排序
            // 2. 根據 Cursor 過濾（如果有）
            // 3. 限制回傳筆數（如果有 Size）
            inventories = await _repository.GetInventoriesByLocationIdAsync(
                request.LocationId.Value,
                BuildQuery(request)
            );
        }
        else if (request.SkuId.HasValue)
        {
            // 呼叫 Repository 的 GetInventoriesBySkuIdAsync 方法查詢庫存
            // 傳入 BuildQuery 方法構建的查詢邏輯
            inventories = await _repository.GetInventoriesBySkuIdsync(
                request.SkuId.Value,
                BuildQuery(request)
            );
        }
    
        // ========== 第三步：將查詢結果包裝成 Pagination 物件回傳 ==========
        // requestedSize：傳入 request.Size，用於判斷是否還有下一頁
        // cursorSelector：使用 x => x.CreatedAt 作為游標選擇器，會自動轉為字串
        return new Pagination<Inventory>(
            items: inventories,
            requestedSize: request.Size,
            cursorSelector: x => x.CreatedAt.ToString("o")
        );
    }

    /// <summary>
    /// 建構庫存查詢表達式
    /// 
    /// 職責：
    /// - 根據 request 中的條件，動態建立出一個 Func
    /// - 用來篩選 IQueryable<Inventory>
    /// - 將查詢邏輯集中在這裡，保持 Repository 介面的簡潔
    /// 
    /// 設計模式：
    /// - 使用 Func<IQueryable<Inventory>, IQueryable<Inventory>> 構建查詢邏輯
    /// - 與 RolesQuery 的 BuildQuery 模式一致
    /// 
    /// 查詢邏輯：
    /// 1. 按 CreatedAt 排序
    /// 2. 如果 Cursor 有值，只查詢 CreatedAt 大於 Cursor 的資料
    /// 3. 如果 Size 有值，限制回傳筆數
    /// 
    /// 參考實作：
    /// - RolesQuery.BuildQuery：類似的實作模式
    /// - UnitOfMeasuresQuery.BuildQuery：類似的實作模式
    /// </summary>
    /// <param name="request">庫存查詢請求物件，包含 Cursor、Size</param>
    /// <returns>
    /// 回傳一個 Func<IQueryable<Inventory>, IQueryable<Inventory>>
    /// 輸入一個 IQueryable<Inventory>，經過 Where、OrderBy、Take 等操作後，輸出另一個 IQueryable<Inventory>
    /// </returns>
    private static Func<IQueryable<Inventory>, IQueryable<Inventory>> BuildQuery(InventoriesQuery request)
    {
        // 回傳一個委派，這個委派接受原始的 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：套用排序 =====
            // 按 CreatedAt 升序排列
            // CreatedAt 越新越後面
            query = query.OrderBy(x => x.CreatedAt);

            // ===== 第二階段：套用 Keyset Pagination (基於 CreatedAt 的游標分頁) =====
            // 這種分頁方式比傳統的 Skip 更穩定，尤其是資料量大時
            if (!string.IsNullOrEmpty(request.Cursor))
            {
                // 解析 Cursor 參數
                if (DateTimeOffset.TryParse(request.Cursor, out var cursorDateTime))
                {
                    // 只取 CreatedAt 大於 cursorDateTime 的資料
                    query = query.Where(x => x.CreatedAt > cursorDateTime);
                }
                else
                {
                    // 如果 Cursor 格式錯誤，拋出 ArgumentException
                    throw new ArgumentException("Cursor 格式錯誤，必須為有效的日期時間字串");
                }
            }

            // ===== 第三階段：限制回傳筆數 =====
            // 如果 Size 有指定，就只取前 Size 筆
            if (request.Size != null)
            {
                query = query.Take(request.Size.Value);
            }

            // 回傳最終組合好的 IQueryable
            // 注意：此時還沒真的去資料庫執行，要到被 foreach 或 ToList() 時才會實際查詢
            return query;
        };
    }
}
