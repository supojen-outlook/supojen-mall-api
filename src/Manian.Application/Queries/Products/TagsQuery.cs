using Manian.Application.Models;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢所有標籤的請求物件
/// 
/// 用途：
/// - 取得系統中所有商品標籤列表
/// - 用於商品標籤選擇器
/// - 支援商品分類和行銷活動
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<Tag>>，表示這是一個查詢請求
/// - 回傳包裝在 Pagination 模型中的標籤集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 TagsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 商品編輯頁面的標籤選擇器
/// - 商品篩選條件
/// - 行銷活動標籤設定
/// - 報表統計
/// 
/// 設計特點：
/// - 簡單直接的查詢，不包含任何參數
/// - 回傳標準化的 Pagination 模型，方便前端處理
/// - 不支援分頁（假設標籤數量有限）
/// - 不支援排序（由 Repository 預設按 SortOrder 排序）
/// 
/// 與 SkusQuery 的對比：
/// - SkusQuery：查詢特定商品的所有 SKU（需要 ProductId）
/// - TagsQuery：查詢所有標籤（不需要參數）
/// 
/// 與 ProductsQuery 的對比：
/// - ProductsQuery：查詢商品列表（支援搜尋、分頁、排序）
/// - TagsQuery：查詢標籤列表（簡單查詢，不支援參數）
/// </summary>
public class TagsQuery : IRequest<Pagination<Tag>>
{
    // TagsQuery 不需要任何屬性
    // 這是因為標籤數量通常有限（通常 < 100）
    // 不需要分頁或篩選功能
}

/// <summary>
/// 標籤查詢處理器
/// 
/// 職責：
/// - 接收 TagsQuery 請求
/// - 呼叫 Repository 查詢所有標籤
/// - 將查詢結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<TagsQuery, Pagination<Tag>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock ITagRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 簡單直接的查詢邏輯
/// - 統一回傳格式為 Pagination，方便前端處理
/// - 依賴 Repository 的實作細節
/// 
/// 與 SkusQueryHandler 的對比：
/// - SkusQueryHandler：查詢特定商品的所有 SKU（需要 ProductId）
/// - TagsQueryHandler：查詢所有標籤（不需要參數）
/// </summary>
public class TagsQueryHandler : IRequestHandler<TagsQuery, Pagination<Tag>>
{
    /// <summary>
    /// 標籤倉儲介面
    /// 
    /// 用途：
    /// - 存取標籤資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/TagRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 預設按 SortOrder 排序（由資料庫約束保證）
    /// 
    /// 資料庫約束：
    /// - 主鍵約束：pk_tags (Id)
    /// - 唯一約束：uk_tags_name (Name)
    /// - 檢查約束：ck_tags_sort_order (SortOrder >= 0)
    /// </summary>
    private readonly ITagRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">標籤倉儲，用於查詢標籤資料</param>
    public TagsQueryHandler(ITagRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理標籤查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 TagsQuery 請求（不包含任何參數）
    /// 2. 呼叫 Repository 的 GetAllAsync 方法取得資料
    /// 3. 將資料包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 不包含任何篩選條件
    /// - 按排序順序排序（由 Repository 實作）
    /// - 雖然回傳 Pagination 模型，但此查詢目前會回傳所有標籤
    /// 
    /// 排序說明：
    /// - 預設按 SortOrder 升序排列
    /// - SortOrder 越小越前面
    /// - 由資料庫約束保證 SortOrder >= 0
    /// 
    /// 錯誤處理：
    /// - 如果沒有標籤，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 效能考量：
    /// - 標籤數量通常有限（< 100）
    /// - 不需要分頁或延遲載入
    /// - 可以考慮加入快取機制
    /// </summary>
    /// <param name="request">標籤查詢請求物件（不包含任何屬性）</param>
    /// <returns>包含所有標籤的分頁模型</returns>
    public async Task<Pagination<Tag>> HandleAsync(TagsQuery request)
    {
        // 呼叫 Repository 的 GetAllAsync 方法查詢所有標籤
        // 這個方法會：
        // 1. 從資料庫查詢所有標籤
        // 2. 按 SortOrder 排序（由 Repository 實作）
        // 3. 回傳標籤集合
        var tags = await _repository.GetAllAsync();
    
        // 將查詢結果包裝成 Pagination 物件回傳
        // requestedSize 設為 null 表示不限制回傳數量 (全量回傳)
        // cursorSelector 設為 null 表示不需要遊標分頁邏輯
        return new Pagination<Tag>(
            items: tags,
            requestedSize: null,
            cursorSelector: null
        );
    }
}
