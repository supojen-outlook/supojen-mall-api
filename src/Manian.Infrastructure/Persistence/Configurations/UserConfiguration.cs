using Manian.Domain.Entities.Memberships;
using Manian.Infrastructure.Persistence.ManyToMany;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manian.Infrastructure.Persistence.Configurations;

/// <summary>
/// User 實體的 EF Core 配置類別
/// 
/// 這個類別負責定義 User 實體與資料庫的對應關係，
/// 包括資料表名稱、索引、關聯、屬性約束等。
/// 
/// 為什麼需要配置類別？
/// - 將資料庫對應邏輯從 Domain 實體中分離出來
/// - 符合 Persistence Ignorance（持久化無關）原則
/// - 讓 Domain 實體保持乾淨，只包含業務邏輯
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// 配置 User 實體的資料庫對應
    /// </summary>
    /// <param name="builder">用於建構 EntityTypeConfiguration 的 Builder</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // =========================================================================
        // 1. 多對多關係：User <-> Role (透過 UserRole 聯結表)
        // =========================================================================
        builder.HasMany(u => u.Roles)   // User 擁有多個 Role
            .WithMany()                 // Role 擁有多個 User (但這裡沒有導航屬性)
            .UsingEntity<UserRole>(     // 使用 UserRole 作為聯結實體
                
                // 左側：UserRole 關聯到 Role
                j => j.HasOne<Role>()                   // UserRole 有一個 Role
                    .WithMany()                         // Role 擁有多個 UserRole
                    .HasForeignKey(x => x.RoleId)       // 外鍵是 UserRole.RoleId
                    .OnDelete(DeleteBehavior.Restrict), // 禁止刪除 Role 當還有 User 關聯時
                
                // 右側：UserRole 關聯到 User  
                j => j.HasOne<User>()                  // UserRole 有一個 User
                    .WithMany()                        // User 擁有多個 UserRole
                    .HasForeignKey(x => x.UserId)      // 外鍵是 UserRole.UserId
                    .OnDelete(DeleteBehavior.Cascade), // User 刪除時，關聯的 UserRole 也刪除
                
                // 主鍵配置：複合主鍵 (UserId, RoleId)
                j => j.HasKey(x => new { x.UserId, x.RoleId })
            );
        
        // 說明：
        // 這種配置會產生三張表：Users、Roles、UserRoles
        // UserRoles 表只有兩個欄位：user_id 和 role_id，都是外鍵也是複合主鍵
        
        // 說明：
        // 這是一種特殊的「共享主鍵」一對一關係
        // UserProfile 的主鍵 Id 同時也是指向 User 的外鍵
        // 這確保 User 和 UserProfile 的 Id 永遠相同
        // 例如：User(Id=1) 的 Profile(Id=1)

        // =========================================================================
        // 2. 一對一關係：User -> PointAccount (共享主鍵)
        // =========================================================================
        builder.HasOne(x => x.PointAccount)          // User 有一個 PointAccount
            .WithOne()                                // PointAccount 屬於一個 User
            .HasForeignKey<PointAccount>(x => x.Id)   // 外鍵是 PointAccount.Id
            .OnDelete(DeleteBehavior.Cascade);        // User 刪除時，PointAccount 也刪除
        
        // 說明：
        // 和 Profile 一樣是共享主鍵的一對一關係
        // PointAccount 的主鍵 Id 同時也是指向 User 的外鍵

        // =========================================================================
        // 3. 一對多關係：User -> Identities (未指定外鍵)
        // =========================================================================
        builder.HasMany(x => x.Identities)       // User 擁有多個 Identity
            .WithOne()                           // Identity 屬於一個 User
            .HasForeignKey(x => x.UserId);       // 外鍵由 EF Core 慣例決定
        
        // 說明：
        // 這裡的 HasForeignKey() 沒有指定參數，EF Core 會：
        // 1. 尋找 Identity 實體中名為 "UserId" 的屬性
        // 2. 如果沒有，就建立一個名為 "UserId" 的 Shadow Property
        // 3. 在啟用 SnakeCase 的情況下，資料庫欄位會是 "user_id"
        //
        // 建議改為明確指定：
        // .HasForeignKey("UserId") 或 .HasForeignKey(x => x.UserId)

        // =========================================================================
        // 4. 一對多關係：User -> PointTransactions (未指定外鍵)
        // =========================================================================
        builder.HasMany(x => x.PointTransactions) // User 擁有多個 PointTransaction
            .WithOne()                            // PointTransaction 屬於一個 User
            .HasForeignKey(x => x.UserId);        // 外鍵由 EF Core 慣例決定
        
        // 說明：
        // 和 Identities 類似，EF Core 會自動建立 UserId 外鍵
        // PointTransaction 表中會有一個 user_id 欄位指向 User
    }
}
