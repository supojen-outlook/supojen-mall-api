using Manian.Application.Models;
using Manian.Application.Models.Products;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 產品列表查詢請求物件
/// 
/// 用途：
/// - 查詢產品列表，支援多種篩選條件
/// - 用於產品列表頁顯示
/// - 支援分頁、排序、搜尋功能
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<ProductBase>>，表示這是一個查詢請求
/// - 回傳 ProductBase 物件集合，包含產品基本資訊
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ProductsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 產品列表頁
/// - 產品搜尋頁
/// - 產品篩選功能
/// 
/// 分頁策略：
/// - 使用 Keyset Pagination (基於 CreatedAt 的游標分頁)
/// - 優點：比傳統 Skip/Take 更穩定，適合大數據量場景
/// </summary>
public class ProductsQuery : IRequest<Pagination<ProductBase>>
{
    /// <summary>
    /// 游標 (上一頁最後一筆的 CreatedAt)
    /// 
    /// 格式: "2023-01-01T00:00:00.0000000+00:00"
    /// 
    /// 用途：實現 Keyset Pagination（游標分頁）
    /// 
    /// 工作原理：
    /// 1. 前端記錄上一頁最後一筆資料的 CreatedAt
    /// 2. 下一頁請求時將此值傳回
    /// 3. 查詢時只取 CreatedAt 小於此值的資料
    /// 
    /// 注意事項：
    /// - 首次查詢時應傳 null（取得最新資料）
    /// - 必須配合 OrderByDescending 使用
    /// </summary>
    public string? Cursor { get; set; }
    
    /// <summary>
    /// 每頁資料筆數
    /// 
    /// 預設值：20
    /// 最大值：100
    /// 
    /// 設計考量：
    /// - 設定合理的預設值平衡效能與使用者體驗
    /// - 限制最大值防止一次性載入過多資料
    /// - 適合前端實作無限滾動（Infinite Scroll）
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// 關鍵字搜尋
    /// 
    /// 搜尋範圍：
    /// - 產品名稱 (Name)
    /// - 產品描述 (Description)
    /// - 產品代碼 (SpuCode)
    /// 
    /// 搜尋特性：
    /// - 不區分大小寫
    /// - 支援模糊搜尋
    /// - 會自動去除前後空白
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// 類別 ID
    /// 
    /// 用途：篩選特定類別的產品
    /// 
    /// 使用場景：
    /// - 類別頁面顯示該類別的所有產品
    /// - 麵包屑導航篩選
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 品牌 ID
    /// 
    /// 用途：篩選特定品牌的產品
    /// 
    /// 使用場景：
    /// - 品牌頁面顯示該品牌的所有產品
    /// - 品牌篩選功能
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// 產品狀態
    /// 
    /// 可能的值：
    /// - "active": 上架中
    /// - "inactive": 下架中
    /// - "draft": 草稿
    /// 
    /// 用途：篩選特定狀態的產品
    /// 
    /// 使用場景：
    /// - 前台只顯示上架中的產品
    /// - 後台管理可查看所有狀態的產品
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 排序方向
    /// 
    /// 可能的值：
    /// - "asc": 升序
    /// - "desc": 降序（預設）
    /// 
    /// 用途：控制排序方向
    /// </summary>
    public string? SortDirection { get; set; } 

    /// <summary>
    /// 排序欄位
    /// 
    /// 可能的值：
    /// - "createdAt": 按建立時間排序（預設）
    /// - "price": 按價格排序
    /// 
    /// 用途：指定排序依據
    /// 
    /// 注意事項：
    /// - 必須配合 SortDirection 使用
    /// - 預設按建立時間降序排列
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 產品 ID 集合
    /// 
    /// 用途：篩選特定 ID 的產品
    /// 
    /// 使用場景：
    /// - 產品頁面顯示特定產品
    /// - 產品比較功能
    /// </summary>
    public IEnumerable<int>? Ids { get; set; }
}

/// <summary>
/// 產品列表查詢處理器
/// 
/// 職責：
/// - 接收 ProductsQuery 請求
/// - 建構查詢條件（搜尋、分頁、排序）
/// - 從資料庫取得符合條件的產品
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ProductsQuery, IEnumerable<ProductBase>> 介面
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
/// </summary>
public class ProductsQueryHandler : IRequestHandler<ProductsQuery, Pagination<ProductBase>>
{
    /// <summary>
    /// 產品倉儲介面
    /// 
    /// 用途：
    /// - 存取產品資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// </summary>
    private readonly IProductRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">產品倉儲，用於查詢產品資料</param>
    public ProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理產品列表查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據請求參數建構查詢條件
    /// 2. 呼叫 Repository 執行查詢
    /// 3. 回傳符合條件的產品集合
    /// 
    /// 查詢特性：
    /// - 支援多欄位模糊搜尋
    /// - 支援多條件篩選
    /// - 使用 Keyset Pagination 分頁
    /// - 支援多欄位排序
    /// </summary>
    /// <param name="request">產品列表查詢請求物件，包含搜尋、分頁、排序參數</param>
    /// <returns>符合條件的產品集合</returns>
    public async Task<Pagination<ProductBase>> HandleAsync(ProductsQuery request)
    {
        // 呼叫 BuildQuery 建構查詢條件，然後傳給 Repository 執行
        var products =  await _repository.GetAllAsync<ProductBase>(BuildQuery(request));
    
        return new Pagination<ProductBase>(
            items: products,
            requestedSize: request.Size,
            cursorSelector: p =>
            {
                if(request.SortBy == "price")
                    return p.Price.ToString();
                else
                    return p.CreatedAt.ToString();
            }
        );
    }

    /// <summary>
    /// 建構產品查詢表達式
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
    /// 1. 應用搜尋條件（如果有 Search）
    /// 2. 應用篩選條件（CategoryId、BrandId、Status）
    /// 3. 應用排序條件（SortBy、SortDirection）
    /// 4. 應用分頁條件（Cursor、Size）
    /// </summary>
    /// <param name="request">產品列表查詢請求物件</param>
    /// <returns>
    /// 查詢表達式委派
    /// 輸入：IQueryable<Product>
    /// 輸出：經過篩選、排序、分頁後的 IQueryable<Product>
    private static Func<IQueryable<Product>, IQueryable<Product>> BuildQuery(ProductsQuery request)
    {
        return query =>
        {
            // ===== 第一階段：應用搜尋條件 =====
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(x => 
                    x.Name.ToLower().Contains(searchTerm) || 
                    (x.Description != null && x.Description.ToLower().Contains(searchTerm)) ||
                    x.SpuCode.ToLower().Contains(searchTerm));
            }

            // ===== 第二階段：應用篩選條件 =====
            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId.Value);
            }

            if (request.BrandId.HasValue)
            {
                query = query.Where(x => x.BrandId == request.BrandId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }

            if(request.Ids != null && request.Ids.Any())
            {
                query = query.Where(x => request.Ids.Contains(x.Id));
            }

            // ===== 第三階段：應用排序條件 =====
            var sortBy = request.SortBy?.ToLower() ?? "createdat";
            var sortDirection = request.SortDirection?.ToLower() ?? "desc";

            // 根據 SortBy 參數選擇排序欄位
            query = sortBy switch
            {
                "price" => sortDirection == "asc" 
                    ? query.OrderBy(x => x.Price) 
                    : query.OrderByDescending(x => x.Price),
                _ => sortDirection == "asc" 
                    ? query.OrderBy(x => x.CreatedAt) 
                    : query.OrderByDescending(x => x.CreatedAt)
            };

            // ===== 第四階段：應用分頁條件 =====
            // 根據排序欄位動態選擇游標
            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                if (sortBy == "price" && decimal.TryParse(request.Cursor, out var priceCursor))
                {
                    // 按價格排序時，使用價格作為游標
                    query = sortDirection == "asc"
                        ? query.Where(x => x.Price > priceCursor)
                        : query.Where(x => x.Price < priceCursor);
                }
                else if (DateTimeOffset.TryParse(request.Cursor, out var timeCursor))
                {
                    // 按建立時間排序時，使用 CreatedAt 作為游標
                    query = sortDirection == "asc"
                        ? query.Where(x => x.CreatedAt > timeCursor)
                        : query.Where(x => x.CreatedAt < timeCursor);
                }
            }

            // ===== 第五階段：限制回傳筆數 =====
            var size = request.Size ?? 20;
            if (size > 0)
            {
                query = query.Take(size);
            }

            return query;
        };
    }
}
