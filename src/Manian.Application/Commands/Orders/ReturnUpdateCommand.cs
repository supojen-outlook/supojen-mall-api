using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 更新退貨單命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新退貨單所需的資訊
/// 設計模式：實作 IRequest<Return>，表示這是一個會回傳更新後實體的命令
/// 
/// 使用場景：
/// - 核准/拒絕退貨申請
/// - 更新退貨狀態
/// - 處理退款
/// - 記錄收到退貨
/// 
/// 設計特點：
/// - 支援部分更新（PATCH 語意）
/// - 自動設定時間戳（如核准時間、退款時間）
/// - 狀態轉換驗證
/// </summary>
public class ReturnUpdateCommand : IRequest<Return>
{
    /// <summary>
    /// 退貨單唯一識別碼
    /// 
    /// 用途：
    /// - 用於識別要更新的退貨單
    /// - 必須是資料庫中已存在的退貨單 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的退貨單
    /// 
    /// 錯誤處理：
    /// - 如果退貨單不存在，會拋出 Failure.NotFound()
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 退貨狀態
    /// 
    /// 可選值：
    /// - "requested"：申請中
    /// - "approved"：已核准
    /// - "rejected"：已拒絕
    /// - "received"：已收到貨
    /// - "refunded"：已退款
    /// 
    /// 狀態流程：
    /// requested → approved → received → refunded
    ///           ↘ rejected
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// - 必須符合狀態轉換規則
    /// 
    /// 注意事項：
    /// - 狀態轉換會自動設定對應的時間戳
    /// - 拒絕後無法再核准
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// 退款金額
    /// 
    /// 用途：
    /// - 設定實際退款金額
    /// - 可能與訂單項目金額不同（如部分退款）
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議在核准退貨時設定
    /// - 退款金額不能超過訂單項目金額
    /// </summary>
    public decimal? RefundAmount { get; set; }
    
    /// <summary>
    /// 退款方式
    /// 
    /// 可選值：
    /// - "original"：原路退回
    /// - "balance"：退至購物金
    /// - "bank_transfer"：銀行轉帳
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議在核准退貨時設定
    /// - 退款方式影響退款流程
    /// </summary>
    public string? RefundMethod { get; set; }
    
    /// <summary>
    /// 客服/倉管備註
    /// 
    /// 用途：
    /// - 記錄客服或倉管的處理備註
    /// - 可用於內部溝通
    /// 
    /// 更新規則：
    /// - 若為 null，保持原值不變
    /// - 若不為 null，更新為新值
    /// 
    /// 注意事項：
    /// - 建議記錄拒絕原因
    /// - 建議記錄特殊處理說明
    /// </summary>
    public string? StaffNotes { get; set; }
}

/// <summary>
/// 更新退貨單命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ReturnUpdateCommand 命令
/// - 查詢退貨單是否存在
/// - 更新退貨單資訊
/// - 自動設定時間戳
/// - 驗證狀態轉換
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ReturnUpdateCommand, Return> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IOrderRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查退款金額是否超過訂單項目金額
/// - 未檢查退款方式是否有效
/// - 建議在實際專案中加入這些檢查
/// </summary>
public class ReturnUpdateHandler : IRequestHandler<ReturnUpdateCommand, Return>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取退貨單資料
    /// - 提供查詢、更新等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 擴展了 GetReturnAsync、AddReturn 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">訂單倉儲，用於查詢和更新退貨單</param>
    public ReturnUpdateHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理更新退貨單命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢退貨單實體
    /// 2. 驗證退貨單是否存在
    /// 3. 更新退貨單屬性
    /// 4. 處理狀態轉換和時間戳
    /// 5. 儲存變更
    /// 6. 回傳更新後的實體
    /// 
    /// 錯誤處理：
    /// - 退貨單不存在：拋出 Failure.NotFound()
    /// - 無效的狀態轉換：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 更新操作不可逆，建議在 UI 層加入確認對話框
    /// - 狀態轉換會自動設定對應的時間戳
    /// - 拒絕後無法再核准
    /// </summary>
    /// <param name="request">更新退貨單命令物件，包含退貨單 ID 和要更新的欄位</param>
    /// <returns>更新後的退貨單實體</returns>
    public async Task<Return> HandleAsync(ReturnUpdateCommand request)
    {
        // ========== 第一步：根據 ID 查詢退貨單實體 ==========
        // 使用 IOrderRepository.GetReturnAsync() 查詢退貨單
        // 這個方法會從資料庫中取得完整的退貨單實體
        var returnItem = await _repository.GetReturnAsync(request.Id);
        
        // ========== 第二步：驗證退貨單是否存在 ==========
        // 如果找不到退貨單，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 退貨單 ID 不存在
        // - 退貨單已被刪除（軟刪除）
        if (returnItem == null)
            throw Failure.NotFound($"退貨單不存在，ID: {request.Id}");

        // ========== 第三步：更新退貨單屬性 ==========
        // 只更新非 null 的欄位，保持 null 欄位的原值不變
        // 這種設計支援部分更新（PATCH 語意）
        if (request.RefundAmount != null) returnItem.RefundAmount = request.RefundAmount;
        if (request.RefundMethod != null) returnItem.RefundMethod = request.RefundMethod;
        if (request.StaffNotes != null) returnItem.StaffNotes = request.StaffNotes;

        // ========== 第四步：處理狀態轉換和時間戳 ==========
        if (request.Status != null)
        {
            // 驗證狀態轉換
            // 拒絕後無法再核准
            if (returnItem.Status == "rejected" && request.Status != "rejected")
                throw Failure.BadRequest("已拒絕的退貨單無法再核准");
            
            // 已退款後無法再變更狀態
            if (returnItem.Status == "refunded")
                throw Failure.BadRequest("已退款的退貨單無法再變更狀態");
            
            // 更新狀態
            returnItem.Status = request.Status;
            
            // 自動設定時間戳
            if (request.Status == "approved")
                returnItem.ApprovedAt = DateTimeOffset.UtcNow;
            
            if (request.Status == "received")
                returnItem.ReceivedAt = DateTimeOffset.UtcNow;
            
            if (request.Status == "refunded")
                returnItem.RefundedAt = DateTimeOffset.UtcNow;
        }

        // ========== 第五步：儲存變更 ==========
        // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _repository.SaveChangeAsync();

        // ========== 第六步：回傳更新後的實體 ==========
        // 回傳更新後的 Return 實體
        // 包含所有更新後的屬性值
        return returnItem;
    }
}
