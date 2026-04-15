using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Services;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Orders;

/// <summary>
/// 新增退貨申請命令 (CQRS 模式中的 Command)
/// 
/// 用途：
/// - 封裝新增退貨申請所需的所有資訊
/// - 作為前端與後端之間的資料傳輸物件 (DTO)
/// 
/// 設計模式：
/// - 實作 IRequest<Return>，表示這是一個會回傳 Return 實體的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 ReturnAddHandler 配合使用，完成新增退貨申請的業務邏輯
/// 
/// 使用場景：
/// - 客戶申請退貨
/// - 系統自動建立退貨記錄
/// - API 端點接收退貨申請請求
/// 
/// 設計特點：
/// - 包含退貨基本資訊（訂單項目 ID、數量、原因等）
/// - 包含客戶備註
/// - 狀態預設為 "requested"
/// 
/// 注意事項：
/// - 退款資訊（RefundAmount、RefundMethod）由後續流程更新
/// - 時間戳（ApprovedAt、ReceivedAt、RefundedAt）由後續流程設定
/// </summary>
public class ReturnAddCommand : IRequest<Return>
{
    /// <summary>
    /// 訂單項目 ID
    /// 
    /// 用途：
    /// - 識別要退貨的訂單項目
    /// - 必須是資料庫中已存在的訂單項目 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單項目
    /// - 該訂單項目不能已有進行中的退貨申請
    /// 
    /// 錯誤處理：
    /// - 如果訂單項目不存在，會拋出 Failure.NotFound()
    /// - 如果訂單項目已有進行中的退貨申請，會拋出 Failure.BadRequest()
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// 退貨數量
    /// 
    /// 用途：
    /// - 記錄客戶要退貨的數量
    /// - 用於庫存管理和退款計算
    /// 
    /// 驗證規則：
    /// - 必須大於 0
    /// - 不能超過訂單項目的購買數量
    /// - 不能超過訂單項目的可退貨數量（考慮部分退貨）
    /// 
    /// 錯誤處理：
    /// - 如果退貨數量無效，會拋出 Failure.BadRequest()
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 退貨原因
    /// 
    /// 用途：
    /// - 記錄客戶退貨的原因
    /// - 用於退貨原因統計和分析
    /// 
    /// 可選值：
    /// - "product_defect"：商品瑕疵
    /// - "wrong_item"：商品錯誤
    /// - "not_as_described"：商品與描述不符
    /// - "changed_mind"：改變心意
    /// - "other"：其他原因
    /// 
    /// 驗證規則：
    /// - 不能為空白或僅包含空白字元
    /// - 建議長度限制：1-500 字元
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// 客戶備註
    /// 
    /// 用途：
    /// - 記錄客戶對退貨的補充說明
    /// - 用於客服處理退貨申請
    /// 
    /// 預設值：
    /// - 空字串（如果未提供）
    /// 
    /// 驗證規則：
    /// - 建議長度限制：0-1000 字元
    /// </summary>
    public string? CustomerNotes { get; set; }
}

/// <summary>
/// 新增退貨申請命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 ReturnAddCommand 命令
/// - 驗證訂單項目是否存在
/// - 驗證退貨數量是否有效
/// - 驗證是否已有進行中的退貨申請
/// - 建立新的 Return 實體
/// - 自動設定時間戳
/// - 將實體儲存到資料庫
/// - 回傳儲存後的實體
/// 
/// 設計模式：
/// - 實作 IRequestHandler<ReturnAddCommand, Return> 介面
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
/// - 未檢查訂單狀態是否允許退貨
/// - 未檢查訂單項目是否已退貨
/// - 未處理並發退貨申請
/// - 建議在實際專案中加入這些檢查
/// </summary>
internal class ReturnAddHandler : IRequestHandler<ReturnAddCommand, Return>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單和退貨資料
    /// - 提供查詢、新增等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetByIdAsync、SaveChangeAsync 等
    /// - 擴展了 GetOrderItemAsync、GetReturnAsync、AddReturn 等方法
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// </summary>
    private readonly IOrderRepository _repository;

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
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">訂單倉儲，用於查詢訂單項目和新增退貨記錄</param>
    /// <param name="uniqueIdentifier">唯一識別碼產生器，用於產生退貨單 ID</param>
    public ReturnAddHandler(
        IOrderRepository repository,
        IUniqueIdentifier uniqueIdentifier)
    {
        _repository = repository;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增退貨申請命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 查詢訂單項目是否存在
    /// 2. 查詢是否已有進行中的退貨申請
    /// 3. 驗證退貨數量是否有效
    /// 4. 產生退貨單編號
    /// 5. 建立新的 Return 實體
    /// 6. 設定實體屬性
    /// 7. 將實體加入倉儲
    /// 8. 儲存變更到資料庫
    /// 9. 回傳儲存後的實體
    /// 
    /// 錯誤處理：
    /// - 訂單項目不存在：拋出 Failure.NotFound()
    /// - 已有進行中的退貨申請：拋出 Failure.BadRequest()
    /// - 退貨數量無效：拋出 Failure.BadRequest()
    /// 
    /// 注意事項：
    /// - 狀態預設為 "requested"
    /// - 自動設定 RequestedAt 和 CreatedAt
    /// - 退款資訊由後續流程更新
    /// </summary>
    /// <param name="request">新增退貨申請命令物件，包含退貨的所有資訊</param>
    /// <returns>儲存後的 Return 實體，包含資料庫自動生成的欄位</returns>
    public async Task<Return> HandleAsync(ReturnAddCommand request)
    {
        // ========== 第一步：查詢訂單項目是否存在 ==========
        // 使用 IOrderRepository.GetOrderItemAsync() 查詢訂單項目
        // 這個方法會從資料庫中取得完整的訂單項目實體
        var orderItem = await _repository.GetOrderItemAsync(request.OrderItemId);
        
        // ========== 第二步：驗證訂單項目是否存在 ==========
        // 如果找不到訂單項目，拋出 404 錯誤
        // 這種情況可能發生在：
        // - 訂單項目 ID 不存在
        // - 訂單項目已被刪除
        if (orderItem == null)
            throw Failure.NotFound($"訂單項目不存在，ID: {request.OrderItemId}");

        // ========== 第三步：查詢是否已有進行中的退貨申請 ==========
        // 使用 IOrderRepository.GetReturnAsync() 查詢退貨記錄
        // 這個方法會從資料庫中取得該訂單項目的退貨記錄
        var existingReturn = await _repository.GetReturnAsync(request.OrderItemId);
        
        // ========== 第四步：驗證是否已有進行中的退貨申請 ==========
        // 如果已有退貨記錄且狀態不是 "rejected"（已拒絕），則拋出錯誤
        // 這種情況可能發生在：
        // - 客戶已申請退貨且尚未完成
        // - 客戶已申請退貨且已核准
        // - 客戶已申請退貨且已收到貨
        // - 客戶已申請退貨且已退款
        if (existingReturn != null && existingReturn.Status != "rejected")
            throw Failure.BadRequest($"訂單項目已有進行中的退貨申請，退貨單號：{existingReturn.ReturnNumber}");

        // ========== 第五步：驗證退貨數量是否有效 ==========
        // 退貨數量必須大於 0
        if (request.Quantity <= 0)
            throw Failure.BadRequest("退貨數量必須大於 0");

        // 退貨數量不能超過訂單項目的購買數量
        if (request.Quantity > orderItem.Quantity)
            throw Failure.BadRequest($"退貨數量不能超過購買數量（購買數量：{orderItem.Quantity}）");

        // 計算可退貨數量（考慮部分退貨）
        // 如果已有被拒絕的退貨記錄，可退貨數量為購買數量
        // 如果沒有退貨記錄，可退貨數量為購買數量
        // 如果已有退貨記錄且狀態為 "rejected"，可退貨數量為購買數量
        var returnableQuantity = orderItem.Quantity;
        
        // 退貨數量不能超過可退貨數量
        if (request.Quantity > returnableQuantity)
            throw Failure.BadRequest($"退貨數量不能超過可退貨數量（可退貨數量：{returnableQuantity}）");

        // ========== 第六步：產生退貨單編號 ==========
        // 格式：RET-{年月日}-{序號}
        // 範例：RET-20240101-0001
        var returnNumber = $"RET-{DateTimeOffset.UtcNow:yyyyMMdd}-{_uniqueIdentifier.NextInt().ToString().Substring(0, 4)}";

        // ========== 第七步：建立新的 Return 實體 ==========
        var returnItem = new Return
        {
            // 產生全域唯一的整數 ID
            Id = _uniqueIdentifier.NextInt(),
            
            // 設定基本屬性
            OrderItemId = request.OrderItemId,
            ReturnNumber = returnNumber,
            Quantity = request.Quantity,
            Reason = request.Reason,
            
            // 設定狀態為申請中
            Status = "requested",
            
            // 設定客戶備註（如果未提供，使用空字串）
            CustomerNotes = request.CustomerNotes ?? string.Empty,
            
            // 設定申請時間為目前 UTC 時間
            RequestedAt = DateTimeOffset.UtcNow,
            
            // 設定建立時間為目前 UTC 時間
            CreatedAt = DateTimeOffset.UtcNow
        };

        // ========== 第八步：將實體加入倉儲 ==========
        // 使用 IOrderRepository.AddReturn() 新增退貨記錄
        // 這只會將實體加入追蹤，不會立即寫入資料庫
        _repository.AddReturn(returnItem);

        // ========== 第九步：儲存變更到資料庫 ==========
        // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會執行 INSERT SQL 語句，並自動生成 ID
        await _repository.SaveChangeAsync();

        // ========== 第十步：回傳儲存後的實體 ==========
        // 回傳儲存後的 Return 實體
        // 包含所有屬性值，包括自動生成的 ID
        // 呼叫者可以使用這個實體進行後續操作
        return returnItem;
    }
}
