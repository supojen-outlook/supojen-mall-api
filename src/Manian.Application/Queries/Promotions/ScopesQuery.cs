using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Promotions;

/// <summary>
/// 查詢促銷活動範圍的請求物件
/// 
/// 用途：
/// - 查詢指定促銷活動的所有適用範圍
/// - 用於促銷活動管理頁面
/// - 支援範圍管理功能
/// 
/// 設計模式：
/// - 實作 IRequest<IEnumerable<PromotionScope>>，表示這是一個查詢請求
/// - 回傳該促銷活動的所有範圍集合
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ScopesQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 促銷活動編輯頁面
/// - 促銷活動範圍總覽
/// - 範圍管理功能
/// 
/// 設計特點：
/// - 簡單直接的查詢，只根據 PromotionId 過濾
/// - 不支援分頁（假設一個促銷活動的範圍數量有限）
/// - 不支援排序（由 Repository 預設按 CreatedAt 排序）
/// 
/// 參考實作：
/// - SkusQuery：查詢特定商品的所有 SKU（需要 ProductId）
/// - AttributeValuesQuery：查詢特定屬性鍵的所有屬性值（需要 AttributeKeyId）
/// </summary>
public class ScopesQuery : IRequest<IEnumerable<PromotionScope>>
{
    /// <summary>
    /// 促銷活動 ID
    /// 
    /// 用途：
    /// - 識別要查詢範圍的促銷活動
    /// - 作為查詢條件過濾範圍
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的促銷活動
    /// 
    /// 錯誤處理：
    /// - 如果促銷活動不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// </summary>
    public int PromotionId { get; set; }
}

/// <summary>
/// 促銷範圍查詢處理器
/// 
/// 職責：
/// - 接收 ScopesQuery 請求
/// - 呼叫 Repository 查詢該促銷活動的所有範圍
/// - 回傳範圍集合
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ScopesQuery, IEnumerable<PromotionScope>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IPromotionRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 簡單直接的查詢邏輯
/// - 不包含複雜的篩選、排序、分頁
/// - 依賴 Repository 的實作細節
/// 
/// 參考實作：
/// - SkusQueryHandler：查詢特定商品的所有 SKU（需要 ProductId）
/// - AttributeValuesQueryHandler：查詢特定屬性鍵的所有屬性值（需要 AttributeKeyId）
/// </summary>
public class ScopesQueryHandler : IRequestHandler<ScopesQuery, IEnumerable<PromotionScope>>
{
    /// <summary>
    /// 促銷活動倉儲介面
    /// 
    /// 用途：
    /// - 存取促銷活動和範圍資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Promotions/PromotionRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 擴展了 GetScopesAsync 方法專門查詢促銷活動的所有範圍
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Promotions/IPromotionRepository.cs
    /// </summary>
    private readonly IPromotionRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">促銷活動倉儲，用於查詢範圍資料</param>
    public ScopesQueryHandler(IPromotionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理促銷範圍查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 ScopesQuery 請求
    /// 2. 呼叫 Repository 的 GetScopesAsync 方法
    /// 3. 回傳該促銷活動的所有範圍集合
    /// 
    /// 查詢特性：
    /// - 根據 PromotionId 過濾範圍
    /// - 按建立時間排序（由 Repository 實作）
    /// - 不支援分頁（假設一個促銷活動的範圍數量有限）
    /// 
    /// 錯誤處理：
    /// - 如果促銷活動不存在，會返回空集合
    /// - 建議在 UI 層處理空集合情況
    /// </summary>
    /// <param name="request">促銷範圍查詢請求物件，包含 PromotionId</param>
    /// <returns>該促銷活動的所有範圍集合</returns>
    public Task<IEnumerable<PromotionScope>> HandleAsync(ScopesQuery request)
    {
        // 呼叫 Repository 的 GetScopesAsync 方法查詢該促銷活動的所有範圍
        // 這個方法會：
        // 1. 從資料庫查詢指定促銷活動 ID 的所有範圍
        // 2. 包含關聯的 Promotion 實體
        // 3. 按建立時間排序
        // 4. 回傳範圍集合
        return _repository.GetScopesAsync(request.PromotionId);
    }
}
