using Manian.Application.Models;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢指定商品的所有 SKU 請求物件
/// 
/// 用途：
/// - 查詢特定商品的所有規格（SKU）
/// - 用於商品詳情頁顯示規格選擇器
/// - 支援庫存管理和價格比較
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Sku>>，表示這是一個查詢請求
/// - 回傳該商品的所有 SKU 集合（包裝於分頁模型中）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SkusQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 商品詳情頁顯示所有規格
/// - 購物車頁面顯示 SKU 資訊
/// - 訂單頁面顯示 SKU 資訊
/// - 庫存管理頁面
/// 
/// 設計特點：
/// - 支援兩種查詢方式：按商品 ID 查詢或按 SKU ID 集合查詢
/// - 兩種查詢方式互斥（二選一）
/// - 目前不支援傳入分頁參數（預設回傳全部）
/// - 不支援排序（由 Repository 預設按 CreatedAt 排序）
/// 
/// 參考實作：
/// - ProductsQuery：查詢商品列表的類似實作
/// - InventoriesQuery：支援多種查詢條件的類似實作
/// </summary>
public class SkusQuery : IRequest<Pagination<Sku>>
{
    /// <summary>
    /// 商品 ID（可選）
    /// 
    /// 用途：
    /// - 識別要查詢 SKU 的商品
    /// - 作為查詢條件過濾 SKU
    /// - 與 ids 參數二選一，不能同時為 null
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的商品
    /// 
    /// 錯誤處理：
    /// - 如果商品不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢商品 ID 為 100 的所有 SKU
    /// var query = new SkusQuery { ProductId = 100 };
    /// </code>
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// SKU ID 集合（可選）
    /// 
    /// 用途：
    /// - 過濾特定 SKU
    /// - 支援多個 SKU ID
    /// - 與 ProductId 參數二選一，不能同時為 null
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不過濾
    /// - 如果有值，則只回傳指定 SKU ID 的資料
    /// 
    /// 錯誤處理：
    /// - 如果指定 SKU ID 不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢 SKU ID 為 1001, 1002, 1003 的資料
    /// var query = new SkusQuery { ids = new[] { 1001, 1002, 1003 } };
    /// </code>
    /// 
    /// 注意事項：
    /// - 與 ProductId 互斥，兩者只能選一個
    /// - 如果兩者都為 null，會返回空集合
    /// </summary>
    public IEnumerable<int>? Ids { get; set; }
}

/// <summary>
/// SKU 查詢處理器
/// 
/// 職責：
/// - 接收 SkusQuery 請求
/// - 根據 ProductId 或 ids 查詢 SKU
/// - 將查詢結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SkusQuery, Pagination<Sku>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IProductRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 支援兩種查詢方式：按商品 ID 查詢或按 SKU ID 集合查詢
/// - 統一回傳格式為 Pagination，方便前端處理
/// - 依賴 Repository 的實作細節
/// 
/// 參考實作：
/// - ProductsQueryHandler：查詢商品列表的類似實作
/// - InventoriesQueryHandler：支援多種查詢條件的類似實作
/// </summary>
public class SkusQueryHandler : IRequestHandler<SkusQuery, Pagination<Sku>>
{
    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 存取商品和 SKU 資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 擴展了 GetSkusByProductIdAsync 和 GetSkusByIdsAsync 方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Products/IProductRepository.cs
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="productRepository">商品倉儲，用於查詢 SKU 資料</param>
    public SkusQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// 處理 SKU 查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 SkusQuery 請求
    /// 2. 判斷查詢方式（按商品 ID 或按 SKU ID 集合）
    /// 3. 呼叫 Repository 的對應方法取得資料
    /// 4. 將資料包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 支援兩種查詢方式：按商品 ID 查詢或按 SKU ID 集合查詢
    /// - 按建立時間排序（由 Repository 實作）
    /// - 雖然回傳 Pagination 模型，但此查詢目前會回傳所有符合條件的 SKU
    /// 
    /// 錯誤處理：
    /// - 如果商品不存在，會返回包含空集合的 Pagination 物件
    /// - 如果指定 SKU ID 不存在，會返回包含空集合的 Pagination 物件
    /// - 如果兩個查詢條件都為 null，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 範例 1：查詢商品 ID 為 100 的所有 SKU
    /// var query1 = new SkusQuery { ProductId = 100 };
    /// var result1 = await _mediator.SendAsync(query1);
    /// 
    /// // 範例 2：查詢 SKU ID 為 1001, 1002, 1003 的資料
    /// var query2 = new SkusQuery { ids = new[] { 1001, 1002, 1003 } };
    /// var result2 = await _mediator.SendAsync(query2);
    /// </code>
    /// </summary>
    /// <param name="request">SKU 查詢請求物件，包含 ProductId 或 ids</param>
    /// <returns>包含符合條件 SKU 的分頁模型</returns>
    public async Task<Pagination<Sku>> HandleAsync(SkusQuery request)
    {
        // ========== 第一步：初始化 SKU 集合 ==========
        // 建立空的 SKU 集合，用於儲存查詢結果
        IEnumerable<Sku> skus = new List<Sku>();

        // ========== 第二步：判斷查詢方式並執行查詢 ==========
        // 查詢方式 1：按商品 ID 查詢
        if( request.ProductId.HasValue)
        {
            // 呼叫 Repository 的 GetSkusByProductIdAsync 方法
            // 查詢指定商品的所有 SKU
            // 見 Infrastructure/Repositories/Products/ProductRepository.cs 的實作
            skus = await _productRepository.GetSkusByProductIdAsync(request.ProductId.Value);
        }
        // 查詢方式 2：按 SKU ID 集合查詢
        else if(request.Ids != null && request.Ids.Any())
        {
            // 呼叫 Repository 的 GetSkusByIdsAsync 方法
            // 查詢指定 SKU ID 集合的資料
            // 見 Infrastructure/Repositories/Products/ProductRepository.cs 的實作
            skus = await _productRepository.GetSkusByIdsAsync(request.Ids);
        }
        // 如果兩個查詢條件都為 null，skus 保持為空集合
    
        // ========== 第三步：將查詢結果包裝成 Pagination 物件回傳 ==========
        // requestedSize 設為 null 表示不限制回傳數量 (全量回傳)
        // cursorSelector 設為 null 表示不需要遊標分頁邏輯
        // 這樣設計是因為此查詢通常用於顯示所有規格，不需要分頁
        return new Pagination<Sku>(
            items: skus,
            requestedSize: null,
            cursorSelector: null
        );
    }
}
