// 引入領域實體
using Manian.Domain.Entities.Products;
// 引入領域的儲存庫介面
using Manian.Domain.Repositories.Products;
// 引入基礎設施層的資料庫上下文
using Manian.Infrastructure.Persistence;
// 引入多對多關聯實體
using Manian.Infrastructure.Persistence.ManyToMany;
// 引入 Entity Framework Core 功能
using Microsoft.EntityFrameworkCore;

namespace Manian.Infrastructure.Repositories.Products;

/// <summary>
/// 類別儲存庫的實作類別
/// 繼承自泛型 Repository<Category>，並實作 ICategoryRepository 介面
/// 負責處理 Category 實體的所有資料庫操作
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    /// <summary>
    /// 建構函式，透過依賴注入取得資料庫上下文
    /// </summary>
    /// <param name="context">MainDbContext 實例</param>
    public CategoryRepository(MainDbContext context) : base(context, "Id") 
    {
        // base(context, "Id") 呼叫父類別建構函式，傳入 context 和主鍵名稱 "Id"
    }

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
    public void AddAttributeKey(int categoryId, int attributeKeyId)
    {
        // 1. [檢查階段] 查詢資料庫是否已存在相同的關聯記錄
        //    context.Set<CategoryAttribute>()：取得關聯表的 DbSet
        //    .Any(...)：發送 SQL 查詢，檢查是否有符合條件的資料
        //    條件：CategoryId 與 AttributeKeyId 必須同時匹配
        var exists = context.Set<CategoryAttribute>()
            .Any(ca => ca.CategoryId == categoryId && ca.AttributeKeyId == attributeKeyId);
        
        // 2. [防禦性編程] 如果關聯已存在，直接返回
        //    好處：
        //    - 避免重複資料（違反唯一約束）
        //    - 避免拋出 DbUpdateException (資料庫主鍵重複錯誤)
        //    - 使方法具備冪等性，重複呼叫不會產生副作用
        if (exists)
        {
            return; 
        }

        // 3. [建構階段] 創建新的關聯實體物件
        //    這時候只是在記憶體中 new 一個物件，還沒寫入資料庫
        var categoryAttribute = new CategoryAttribute
        {
            CategoryId = categoryId,
            AttributeKeyId = attributeKeyId
        };

        // 4. [追蹤階段] 將實體加入 DbContext 的變更追蹤器
        //    狀態會被標記為 "Added"
        //    注意：此時尚未產生 SQL INSERT 指令，必須等待 context.SaveChanges() 執行後才會寫入 DB
        context.Set<CategoryAttribute>().Add(categoryAttribute);
    }

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
    public void RemoveAttributeKey(int categoryId, int attributeKeyId)
    {
        // 1. [查詢階段] 查找現有的關聯實體
        //    .FirstOrDefault(...)：從資料庫查詢符合條件的第一筆資料，若沒有則返回 null
        //    注意：這會執行 SQL SELECT 指令，將實體資料載入到記憶體中
        var categoryAttribute = context.Set<CategoryAttribute>()
            .FirstOrDefault(ca => ca.CategoryId == categoryId && ca.AttributeKeyId == attributeKeyId);

        // 2. [防禦性編程] 檢查查詢結果
        //    如果找不到關聯實體 (null)，表示資料可能已經被刪除或從未存在
        //    直接返回，不執行後續邏輯，避免拋出 NullReferenceException
        if (categoryAttribute == null)
        {
            return;
        }

        // 3. [刪除階段] 將實體從 DbContext 中移除
        //    EF Core 會將該實體的狀態標記為 "Deleted"
        //    當執行 context.SaveChanges() 時，會生成 SQL DELETE 指令
        context.Set<CategoryAttribute>().Remove(categoryAttribute);
    }
}