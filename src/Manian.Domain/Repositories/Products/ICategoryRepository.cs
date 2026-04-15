using System;
using Manian.Domain.Entities.Products;

namespace Manian.Domain.Repositories.Products;

public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// 為指定類別新增一個屬性鍵關聯
    /// 
    /// 業務邏輯：
    /// - 在類別與屬性鍵的多對多關聯表中建立一筆新記錄
    /// - 確保不會重複添加相同的關聯（冪等性設計）
    /// 
    /// 執行流程：
    /// 1. 檢查資料庫中是否已存在該組合的關聯
    /// 2. 若不存在，則建立實體並加入追蹤
    /// 3. 等待外部呼叫 SaveChanges 持久化到資料庫
    /// </summary>
    /// <param name="categoryId">目標類別的 ID</param>
    /// <param name="attributeKeyId">要關聯的屬性鍵 ID</param>
    void AddAttributeKey(int categoryId, int attributeKeyId);

    /// <summary>
    /// 移除指定類別的某個屬性鍵關聯
    /// 
    /// 業務邏輯：
    /// - 從類別與屬性鍵的多對多關聯表中刪除指定記錄
    /// - 採用「安靜失敗」策略，如果刪除的目標不存在，不拋出異常
    /// 
    /// 執行流程：
    /// 1. 從資料庫查詢目標關聯實體
    /// 2. 如果找到，將其狀態標記為刪除
    /// 3. 等待外部呼叫 SaveChanges 持久化到資料庫
    /// </summary>
    /// <param name="categoryId">目標類別的 ID</param>
    /// <param name="attributeKeyId">要移除關聯的屬性鍵 ID</param>
    void RemoveAttributeKey(int categoryId, int attributeKeyId);
}
