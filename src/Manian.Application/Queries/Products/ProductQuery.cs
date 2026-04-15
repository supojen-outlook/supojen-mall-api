using Manian.Application.Models.Products;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 查詢單一產品的請求物件
/// 
/// 用途：
/// - 根據產品 ID 取得完整的產品資訊
/// - 用於產品詳情頁顯示
/// - 用於產品編輯功能
/// 
/// 設計模式：
/// - 實作 IRequest<ProductResponse>，表示這是一個查詢請求
/// - 回傳 ProductResponse 物件，包含產品完整資訊
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ProductQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 產品詳情頁
/// - 產品編輯表單
/// - 產品資訊確認
/// 
/// 錯誤處理：
/// - 產品不存在：拋出 Failure.NotFound("產品不存在")
/// </summary>
public class ProductQuery : IRequest<Product>
{
    /// <summary>
    /// 產品唯一識別碼
    /// 
    /// 用途：
    /// - 用於查詢指定的產品
    /// - 必須是資料庫中已存在的產品 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的產品
    /// 
    /// 錯誤處理：
    /// - 如果產品不存在，會拋出 Failure.NotFound("產品不存在")
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 產品查詢處理器
/// 
/// 職責：
/// - 接收 ProductQuery 請求
/// - 從資料庫取得指定產品的完整資訊
/// - 將實體轉換為 ProductResponse 回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ProductQuery, ProductResponse> 介面
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
public class ProductQueryHandler : IRequestHandler<ProductQuery, Product>
{
    /// <summary>
    /// 產品倉儲介面
    /// 
    /// 用途：
    /// - 存取產品資料
    /// - 查詢指定 ID 的產品實體
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 查詢實體
    /// - 繼承自 Repository<Product>，獲得通用 CRUD 功能
    /// </summary>
    private readonly IProductRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">產品倉儲，用於查詢產品資料</param>
    public ProductQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理產品查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據產品 ID 查詢產品實體並直接轉換為 ProductResponse
    /// 2. 驗證產品是否存在
    /// 3. 回傳產品回應物件
    /// 
    /// 實體轉換說明：
    /// - 使用 Repository 的泛型 GetByIdAsync<ProductResponse> 方法
    /// - 直接從資料庫查詢並投影為 ProductResponse
    /// - 包含產品的所有基本資訊
    /// - 包含關聯的類別、品牌資訊
    /// - 包含標籤陣列
    /// 
    /// 錯誤處理：
    /// - 產品不存在：拋出 Failure.NotFound("產品不存在")
    /// </summary>
    /// <param name="request">產品查詢請求物件，包含產品 ID</param>
    /// <returns>產品回應物件，包含產品完整資訊</returns>
    public async Task<Product> HandleAsync(ProductQuery request)
    {
        // ========== 第一步：根據產品 ID 查詢並轉換為 ProductResponse ==========
        // 使用 IProductRepository.GetByIdAsync<ProductResponse>() 查詢產品
        // 這個方法會從資料庫中查詢並直接投影為 ProductResponse
        // 使用泛型參數 ProductResponse，避免手動屬性對應
        var product = await _repository.GetByIdAsync<Product>(request.Id);

        // ========== 第二步：驗證產品是否存在 ==========
        // 如果找不到產品，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 產品 ID 不存在
        // - 產品已被刪除（軟刪除）
        if (product == null)
            throw Failure.NotFound(title: "產品不存在");

        // ========== 第三步：回傳產品回應物件 ==========
        return product;
    }
}
