using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Users;

/// <summary>
/// 新增點數交易命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增點數交易所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// - 處理點數的增減操作
/// 
/// 設計模式：
/// - 實作 IRequest<PointTransaction>，表示這是一個會回傳 PointTransaction 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PointTransactionAddHandler 配合使用，完成新增點數交易的業務邏輯
/// 
/// 使用場景：
/// - 訂單完成後贈送點數
/// - 退款時扣除點數
/// - 促銷活動贈送點數
/// - 手動調整用戶點數
/// 
/// 設計特點：
/// - 包含點數變動資訊（Delta、Reason）
/// - 包含參考資訊（RefType、RefId）
/// - 支援額外資訊（Metadata）
/// 
/// 參考實作：
/// - PromotionAddCommand：類似的命令設計
/// - CouponAddCommand：類似的命令設計
/// </summary>
public class PointTransactionAddCommand : IRequest<PointTransaction>
{
    /// <summary>
    /// 用戶 ID（必填）
    /// 
    /// 用途：
    /// - 識別要新增點數交易的用戶
    /// - 作為查詢條件過濾點數交易
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的用戶
    /// 
    /// 錯誤處理：
    /// - 如果用戶不存在，會拋出 Failure.NotFound("找不到指定的用戶")
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 點數變動量（必填）
    /// 
    /// 用途：
    /// - 表示積分數量的變化
    /// - 正數表示增加積分，負數表示減少積分
    /// 
    /// 驗證規則：
    /// - 不能為 0
    /// - 建議範圍：-10000 到 10000
    /// 
    /// 範例：
    /// - 100：增加 100 點
    /// - -50：扣除 50 點
    /// </summary>
    public int Delta { get; set; }

    /// <summary>
    /// 交易原因（必填）
    /// 
    /// 用途：
    /// - 描述積分變動的原因
    /// - 用於點數歷史記錄顯示
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-200 字元
    /// 
    /// 範例：
    /// - "訂單完成贈送點數"
    /// - "退款扣除點數"
    /// - "促銷活動贈送點數"
    /// - "手動調整點數"
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// 參考類型（可選）
    /// 
    /// 用途：
    /// - 標識積分交易關聯的業務類型
    /// - 用於追蹤點數變動的來源
    /// 
    /// 可選值：
    /// - "order"：訂單相關
    /// - "refund"：退款相關
    /// - "promotion"：促銷活動相關
    /// - "adjustment"：手動調整
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不關聯任何業務
    /// - 如果有值，則必須是預定義的類型之一
    /// </summary>
    public string? RefType { get; set; }

    /// <summary>
    /// 參考 ID（可選）
    /// 
    /// 用途：
    /// - 關聯的業務 ID
    /// - 用於追蹤具體的業務來源
    /// 
    /// 驗證規則：
    /// - 預設為 null，表示不關聯任何業務
    /// - 如果有值，則必須是有效的業務 ID
    /// - 通常與 RefType 一起使用
    /// 
    /// 範例：
    /// - "100"：訂單 ID 為 100
    /// - "PROMO-001"：促銷活動 ID 為 PROMO-001
    /// </summary>
    public string? RefId { get; set; }
}

/// <summary>
/// 新增點數交易命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 PointTransactionAddCommand 命令
/// - 建立新的 PointTransaction 實體
/// - 更新用戶的點數帳戶餘額
/// - 將實體儲存到資料庫
/// - 回傳儲存後的 PointTransaction 實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PointTransactionAddCommand, PointTransaction> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// 
/// 設計特點：
/// - 使用交易確保資料一致性
/// - 自動更新用戶點數帳戶餘額
/// - 支援點數增減操作
/// 
/// 參考實作：
/// - PromotionAddHandler：類似的命令處理器設計
/// - CouponAddHandler：類似的命令處理器設計
/// </summary>
internal class PointTransactionAddHandler : IRequestHandler<PointTransactionAddCommand, PointTransaction>
{
    /// <summary>
    /// 用戶倉儲介面
    /// 
    /// 用途：
    /// - 存取用戶資料
    /// - 查詢用戶的點數帳戶
    /// - 新增點數交易記錄
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Memberships/UserRepository.cs）
    /// - 提供泛型方法 AddAsync、GetByIdAsync 等
    /// - 繼承自 Repository<User>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Memberships/IUserRepository.cs
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 唯一識別碼產生器服務
    /// 
    /// 用途：
    /// - 產生全域唯一的整數 ID
    /// - 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/Snowflake.cs
    /// - 註冊為單例模式 (Singleton)
    /// 
    /// 設計考量：
    /// - 確保在分散式環境下的唯一性
    /// - 避免使用資料庫自增 ID（不適合分散式環境）
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="userRepository">用戶倉儲，用於查詢用戶和新增點數交易</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生點數交易 ID</param>
    public PointTransactionAddHandler(
        IUserRepository userRepository,
        IUniqueIdentifier uniqueIdentifier)
    {
        _userRepository = userRepository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增點數交易命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 驗證用戶是否存在
    /// 2. 建立新的 PointTransaction 實體
    /// 3. 更新用戶的點數帳戶餘額
    /// 4. 將實體加入倉儲
    /// 5. 儲存變更到資料庫
    /// 6. 回傳儲存後的實體
    /// 
    /// 返回值：
    /// - PointTransaction：儲存後的點數交易實體，包含自動生成的 ID
    /// 
    /// 錯誤處理：
    /// - 用戶不存在：拋出 Failure.NotFound("找不到指定的用戶")
    /// - 點數餘額不足：拋出 Failure.BadRequest("點數餘額不足")
    /// 
    /// 注意事項：
    /// - 新增後的實體會包含自動生成的 ID
    /// - 建議在 UI 層顯示新增成功的訊息
    /// - 使用交易確保資料一致性
    /// </summary>
    /// <param name="request">新增點數交易命令物件，包含點數交易的所有資訊</param>
    /// <returns>儲存後的點數交易實體，包含自動生成的 ID</returns>
    public async Task<PointTransaction> HandleAsync(PointTransactionAddCommand request)
    {
        // ========== 第一步：驗證用戶是否存在 ==========
        // 使用 IUserRepository.GetByIdAsync() 查詢用戶
        // 如果找不到用戶，拋出 404 錯誤
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw Failure.NotFound(title: "找不到指定的用戶");

        // ========== 第二步：驗證點數餘額是否足夠 ==========
        // 如果 Delta 為負數（扣除點數），需要檢查餘額是否足夠
        if (request.Delta < 0 && user.PointAccount.Balance < Math.Abs(request.Delta))
            throw Failure.BadRequest(title: "點數餘額不足");

        // ========== 第三步：建立新的 PointTransaction 實體 ==========
        var transaction = new PointTransaction
        {
            // 產生全域唯一的整數 ID
            // 使用雪花演算法 (Snowflake) 確保分散式環境下的唯一性
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定用戶 ID
            UserId = request.UserId,
            
            // 設定點數變動量
            Delta = request.Delta,
            
            // 設定交易原因
            Reason = request.Reason,
            
            // 設定參考類型（如果提供了）
            RefType = request.RefType ?? "adjustment",
            
            // 設定參考 ID（如果提供了）
            RefId = request.RefId ?? string.Empty,
            
            // 設定交易時間
            OccurredAt = DateTimeOffset.UtcNow,
        };

        // ========== 第四步：更新用戶的點數帳戶餘額 ==========
        // 直接修改用戶的點數帳戶餘額
        // EF Core 會自動追蹤這個變更
        user.PointAccount.Balance += request.Delta;
        
        // 更新點數帳戶的最後更新時間
        user.PointAccount.UpdatedAt = DateTimeOffset.UtcNow;

        // ========== 第五步：將實體加入倉儲 ==========
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        // 需要呼叫 SaveChangeAsync 才會實際執行 INSERT SQL
        _userRepository.AddPointTransaction(transaction);

        // ========== 第六步：儲存變更到資料庫 ==========
        // 這會將所有被追蹤的實體變更寫入資料庫
        // 包括新增、修改、刪除的實體
        await _userRepository.SaveChangeAsync();

        // ========== 第七步：回傳儲存後的實體 ==========
        // 回傳儲存後的 PointTransaction 實體
        // 包含所有屬性值，包括自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return transaction;
    }
}
