using Manian.Application.Models;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢指定訂單的付款記錄請求物件
/// 
/// 用途：
/// - 查詢特定訂單的付款記錄
/// - 用於訂單詳情頁顯示付款資訊
/// - 支援付款狀態確認
/// 
/// 設計模式：
/// - 實作 IRequest<Payment?>，表示這是一個查詢請求
/// - 回傳該訂單的付款記錄實體（可能為 null）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 PaymentQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 訂單詳情頁顯示付款資訊
/// - 付款狀態確認頁面
/// - 退款處理頁面
/// - 訂單管理後台
/// 
/// 資料關聯：
/// - 根據 sql/04-order/README.md 的五表關係圖
/// - Payment 與 Order 是一對一關係
/// - 一個訂單只有一筆付款記錄
/// - 此方法根據 OrderId 查詢付款記錄
/// 
/// 設計特點：
/// - 簡單直接的查詢，只根據 OrderId 過濾
/// - 不支援分頁（一個訂單只有一筆付款記錄）
/// - 不支援排序（只回傳一筆記錄）
/// 
/// 參考實作：
/// - ShipmentQuery：查詢訂單項目的物流記錄（需要 OrderItemId）
/// - OrderQuery：查詢單一訂單詳情（需要 OrderId）
/// </summary>
public class PaymentQuery : IRequest<Payment?>
{
    /// <summary>
    /// 訂單 ID
    /// 
    /// 用途：
    /// - 識別要查詢付款記錄的訂單
    /// - 作為查詢條件過濾付款記錄
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單
    /// 
    /// 錯誤處理：
    /// - 如果訂單不存在，會返回 null
    /// - 如果訂單沒有付款記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單 ID 為 1001 的付款記錄
    /// var query = new PaymentQuery { OrderId = 1001 };
    /// var payment = await _mediator.SendAsync(query);
    /// if (payment != null)
    /// {
    ///     // 顯示付款資訊
    ///     Console.WriteLine($"付款方式：{payment.Method}");
    ///     Console.WriteLine($"付款狀態：{payment.Status}");
    /// }
    /// else
    /// {
    ///     // 處理沒有付款記錄的情況
    ///     Console.WriteLine("該訂單沒有付款記錄");
    /// }
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Payment 與 Order 是一對一關係
    /// - 一個訂單只有一筆付款記錄
    /// - 此屬性用於關聯 Order 和 Payment
    /// </summary>
    public int OrderId { get; init; }
}

/// <summary>
/// 付款記錄查詢處理器
/// 
/// 職責：
/// - 接收 PaymentQuery 請求
/// - 呼叫 Repository 查詢該訂單的付款記錄
/// - 回傳付款記錄實體或 null
/// 
/// 設計模式：
/// - 實作 IRequestHandler<PaymentQuery, Payment?> 介面
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
/// 設計特點：
/// - 簡單直接的查詢邏輯
/// - 不包含複雜的篩選、排序、分頁
/// - 依賴 Repository 的實作細節
/// 
/// 參考實作：
/// - ShipmentQueryHandler：查詢訂單項目的物流記錄
/// - OrderQueryHandler：查詢單一訂單詳情
/// </summary>
public class PaymentQueryHandler : IRequestHandler<PaymentQuery, Payment?>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單和付款記錄資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 擴展了 GetPaymentAsync 方法專門查詢訂單的付款記錄
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Payment 與 Order 是一對一關係
    /// - 一個訂單只有一筆付款記錄
    /// - GetPaymentAsync 方法根據 OrderId 查詢付款記錄
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢付款記錄資料</param>
    public PaymentQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// 處理付款記錄查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 PaymentQuery 請求
    /// 2. 呼叫 Repository 的 GetPaymentAsync 方法
    /// 3. 回傳該訂單的付款記錄實體或 null
    /// 
    /// 查詢特性：
    /// - 根據 OrderId 過濾付款記錄
    /// - 包含關聯的 Order 實體（由 Repository 實作決定）
    /// - 不支援分頁（一個訂單只有一筆付款記錄）
    /// 
    /// 錯誤處理：
    /// - 如果訂單不存在，會返回 null
    /// - 如果訂單沒有付款記錄，會返回 null
    /// - 建議在 UI 層處理 null 情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單 ID 為 1001 的付款記錄
    /// var query = new PaymentQuery { OrderId = 1001 };
    /// var payment = await _mediator.SendAsync(query);
    /// if (payment != null)
    /// {
    ///     // 顯示付款資訊
    ///     Console.WriteLine($"付款方式：{payment.Method}");
    ///     Console.WriteLine($"付款狀態：{payment.Status}");
    ///     Console.WriteLine($"付款金額：{payment.Amount}");
    ///     Console.WriteLine($"付款時間：{payment.PaidAt}");
    /// }
    /// else
    /// {
    ///     // 處理沒有付款記錄的情況
    ///     Console.WriteLine("該訂單沒有付款記錄");
    /// }
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Payment 與 Order 是一對一關係
    /// - 一個訂單只有一筆付款記錄
    /// - GetPaymentAsync 方法根據 OrderId 查詢付款記錄
    /// </summary>
    /// <param name="request">付款記錄查詢請求物件，包含 OrderId</param>
    /// <returns>該訂單的付款記錄實體，若不存在則回傳 null</returns>
    public async Task<Payment?> HandleAsync(PaymentQuery request)
    {
        // ========== 第一步：查詢付款記錄 ==========
        // 使用 IOrderRepository.GetPaymentAsync() 查詢指定訂單的付款記錄
        // 參數：request.OrderId - 要查詢的訂單 ID
        // 回傳：Payment 實體或 null（如果找不到）
        var payment = await _orderRepository.GetPaymentAsync(
            request.OrderId
        );

        // ========== 第二步：驗證付款記錄是否存在 ==========
        // 如果找不到付款記錄，直接回傳 null
        // 這種情況可能發生在：
        // - 訂單不存在
        // - 訂單還沒有付款記錄
        // - 付款記錄已被刪除
        if (payment == null) return null;

        // ========== 第三步：檢查付款記錄是否過期 ==========
        // 過期條件：
        // 1. payment.ExpiredAt.HasValue - 付款記錄有設定過期日期
        // 2. payment.ExpiredAt > DateOnly.FromDateTimeDateTime.Today) - 過期日期大於今天（表示已過期）
        // 
        // 注意：這裡的邏輯是「過期日期大於今天」表示已過期
        // 這與常見的「過期日期小於今天」表示已過期不同
        // 可能是業務邏輯的特殊需求，或是程式碼需要修正
        if (payment.ExpiredAt.HasValue && payment.ExpiredAt < DateOnly.FromDateTime(DateTime.Today))
        {
            // ========== 第四步：刪除過期的付款記錄 ==========
            // 使用 IOrderRepository.DeletePayment() 標記付款記錄為待刪除
            // 注意：此時尚未寫入資料庫，需要呼叫 SaveChangeAsync()
            _orderRepository.DeletePayment(payment);

            // ========== 第五步：儲存變更 ==========
            // 使用 IOrderRepository.SaveChangeAsync() 將變更寫入資料庫
            // 這會提交所有被追蹤的實體變更，包括刪除操作
            await _orderRepository.SaveChangeAsync();

            // ========== 第六步：回傳 null ==========
            // 因為付款記錄已過期並被刪除，所以回傳 null
            // 呼叫端應該處理這種情況（如顯示「付款已過期」訊息）
            return null;
        }

        // ========== 第七步：回傳有效的付款記錄 ==========
        // 如果付款記錄存在且未過期，回傳該記錄
        return payment;
    }
}
