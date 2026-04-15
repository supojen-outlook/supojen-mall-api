using Manian.Domain.Entities.Products;

namespace Manian.Domain.Repositories.Products;

/// <summary>
/// 屬性鍵倉儲介面
/// 
/// 職責：
/// - 定義屬性鍵相關的資料存取操作
/// - 繼承泛型 IRepository<AttributeKey> 獲得通用 CRUD 功能
/// - 擴展特定查詢方法（如 GetValuesAsync）
/// 
/// 設計模式：
/// - Repository 模式：抽象資料存取邏輯
/// - 介面隔離原則（ISP）：只定義特定方法，不暴露不必要功能
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 由 Infrastructure 層的 AttributeKeyRepository 實作
/// - 被 Application 層的 Query/Command 使用
/// 
/// 使用場景：
/// - 商品屬性管理（新增、查詢、更新屬性鍵）
/// - 類別屬性關聯管理
/// - 商品發布時的屬性選擇
/// </summary>
public interface IAttributeKeyRepository : IRepository<AttributeKey>
{
    /// <summary>
    /// 為指定屬性鍵新增單一屬性值
    /// </summary>
    /// <param name="attributeKeyId">屬性鍵 ID</param>
    /// <param name="value">屬性值內容</param>
    /// <param name="sortOrder">排序順序，預設為 0</param>
    /// <param name="description">屬性值描述，可為 null</param>
    /// <returns>新增的屬性值實體</returns>
    AttributeValue AddValue(int attributeKeyId, string value, int sortOrder = 0, string? description = null);

    /// <summary>
    /// 刪除指定的屬性值並標記為待刪除狀態
    /// 
    /// 不會立即寫入資料庫，必須呼叫 SaveChangeAsync() 才會實際執行 DELETE SQL
    /// </summary>
    /// <param name="attributeValue">要刪除的屬性值實體</param>
    void Delete(AttributeValue attributeValue);

    /// <summary>
    /// 根據 ID 查詢單一屬性值
    /// </summary>
    /// <param name="attributeValueId">屬性值 ID</param>
    /// <returns>查詢到的屬性值實體，若不存在則回傳 null</returns>
    Task<AttributeValue?> GetValueAsync(int attributeValueId);

    /// <summary>
    /// 根據屬性鍵 ID 查詢所有關聯的屬性值
    /// </summary>
    /// <param name="attributeKeyId">屬性鍵 ID</param>
    /// <returns>符合條件的屬性值集合</returns>
    Task<IEnumerable<AttributeValue>> GetValuesAsync(int attributeKeyId);

    /// <summary>
    /// 根據類別 ID 與銷售狀態，非同步取得該類別關聯的屬性鍵集合。
    /// </summary>
    /// <param name="categoryId">要查詢的類別 ID</param>
    /// <param name="forSales">
    /// 銷售狀態篩選條件 (可選)。
    /// true: 僅回傳用於銷售的屬性。
    /// false: 僅回傳非用於銷售的屬性。
    /// null: 回傳所有屬性 (不進行篩選)。
    /// </param>
    /// <returns>符合條件的屬性鍵集合 (AttributeKey)</returns>
    Task<IEnumerable<AttributeKey>> GetCategoryAttributesAsync(int categoryId, bool? forSales = null);
}
