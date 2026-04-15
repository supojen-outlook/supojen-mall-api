using Manian.Application.Models;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Orders;

/// <summary>
/// 查詢指定訂單的所有訂單項目請求物件
/// 
/// 用途：
/// - 查詢特定訂單的所有訂單項目
/// - 用於訂單詳情頁顯示訂單項目列表
/// - 支援訂單項目狀態追蹤
/// 
/// 設計模式：
/// - 實作 IRequest<Pagination<OrderItem>>，表示這是一個查詢請求
/// - 回傳該訂單的所有訂單項目集合（包裝於分頁模型中）
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 OrderItemsQueryHandler 配合使用，完成查詢
/// 
/// 使用場景：
/// - 訂單詳情頁顯示所有訂單項目
/// - 訂單項目狀態管理
/// - 退貨處理頁面
/// - 訂單管理後台
/// 
/// 設計特點：
/// - 簡單直接的查詢，只需要 OrderId
/// - 回傳標準化的 Pagination 模型，方便前端處理
/// - 不支援分頁（假設一個訂單的項目數量有限）
/// - 不支援排序（由 Repository 預設按 CreatedAt 排序）
/// 
/// 參考實作：
/// - SkusQuery：查詢商品的所有 SKU（需要 ProductId）
/// - OrdersQuery：查詢訂單列表（支援搜尋、分頁、排序）
/// </summary>
public class OrderItemsQuery : IRequest<Pagination<OrderItem>>
{
    /// <summary>
    /// 訂單 ID
    /// 
    /// 用途：
    /// - 識別要查詢訂單項目的訂單
    /// - 作為查詢條件過濾訂單項目
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的訂單
    /// 
    /// 錯誤處理：
    /// - 如果訂單不存在，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單 ID 為 1001 的所有訂單項目
    /// var query = new OrderItemsQuery { OrderId = 1001 };
    /// var result = await _mediator.SendAsync(query);
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 與 OrderItem 是一對多關係
    /// - 一個訂單可以有多個訂單項目
    /// - 此屬性用於關聯 Order 和 OrderItem
    /// </summary>
    public int OrderId { get; init; }
}

/// <summary>
/// 訂單項目查詢處理器
/// 
/// 職責：
/// - 接收 OrderItemsQuery 請求
/// - 呼叫 Repository 查詢該訂單的所有訂單項目
/// - 將查詢結果包裝成統一的 Pagination 模型回傳
/// 
/// 設計模式：
/// - 實作 IRequestHandler<OrderItemsQuery, Pagination<OrderItem>> 介面
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
/// - 統一回傳格式為 Pagination，方便前端處理
/// - 依賴 Repository 的實作細節
/// 
/// 參考實作：
/// - SkusQueryHandler：查詢商品的所有 SKU
/// - OrdersQueryHandler：查詢訂單列表
/// </summary>
public class OrderItemsQueryHandler : IRequestHandler<OrderItemsQuery, Pagination<OrderItem>>
{
    /// <summary>
    /// 訂單倉儲介面
    /// 
    /// 用途：
    /// - 存取訂單和訂單項目資料
    /// - 執行資料庫查詢
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/OrderRepository.cs）
    /// - 提供泛型方法 GetAllAsync 支援自訂查詢
    /// - 擴展了 GetOrderItemsAsync 方法專門查詢訂單的項目
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Orders/IOrderRepository.cs
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 與 OrderItem 是一對多關係
    /// - 一個訂單可以有多個訂單項目
    /// - GetOrderItemsAsync 方法根據 OrderId 查詢訂單項目
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="orderRepository">訂單倉儲，用於查詢訂單項目資料</param>
    public OrderItemsQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// 處理訂單項目查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收 OrderItemsQuery 請求
    /// 2. 呼叫 Repository 的 GetOrderItemsAsync 方法
    /// 3. 將查詢結果包裝成 Pagination 物件回傳
    /// 
    /// 查詢特性：
    /// - 根據 OrderId 過濾訂單項目
    /// - 按建立時間排序（由 Repository 實作）
    /// - 雖然回傳 Pagination 模型，但此查詢目前會回傳所有符合條件的訂單項目
    /// 
    /// 錯誤處理：
    /// - 如果訂單不存在，會返回包含空集合的 Pagination 物件
    /// - 如果訂單沒有項目，會返回包含空集合的 Pagination 物件
    /// - 建議在 UI 層處理空集合情況
    /// 
    /// 使用範例：
    /// <code>
    /// // 查詢訂單 ID 為 1001 的所有訂單項目
    /// var query = new OrderItemsQuery { OrderId = 1001 };
    /// var result = await _mediator.SendAsync(query);
    /// 
    /// // 處理查詢結果
    /// foreach (var item in result.List)
    /// {
    ///     Console.WriteLine($"商品名稱：{item.ProductName}");
    ///     Console.WriteLine($"單價：{item.UnitPrice}");
    ///     Console.WriteLine($"數量：{item.Quantity}");
    ///     Console.WriteLine($"狀態：{item.Status}");
    /// }
    /// </code>
    /// 
    /// 資料關聯：
    /// - 根據 sql/04-order/README.md 的五表關係圖
    /// - Order 與 OrderItem 是一對多關係
    /// - 一個訂單可以有多個訂單項目
    /// - GetOrderItemsAsync 方法根據 OrderId 查詢訂單項目
    /// </summary>
    /// <param name="request">訂單項目查詢請求物件，包含 OrderId</param>
    /// <returns>包含該訂單所有訂單項目的分頁模型</returns>
    public async Task<Pagination<OrderItem>> HandleAsync(OrderItemsQuery request)
    {
        // ========== 第一步：呼叫 Repository 查詢訂單項目 ==========
        // 呼叫 Repository 的 GetOrderItemsAsync 方法查詢該訂單的所有項目
        // 這個方法會：
        // 1. 從資料庫查詢指定訂單 ID 的所有訂單項目
        // 2. 按建立時間排序（由 Repository 實作）
        // 3. 回傳訂單項目集合
        var orderItems = await _orderRepository.GetOrderItemsAsync(request.OrderId);

        // ========== 第二步：將查詢結果包裝成 Pagination 物件回傳 ==========
        // requestedSize 設為 null 表示不限制回傳數量 (全量回傳)
        // cursorSelector 設為 null 表示不需要遊標分頁邏輯
        // 這樣設計是因為此查詢通常用於顯示訂單的所有項目，不需要分頁
        return new Pagination<OrderItem>(
            items: orderItems,
            requestedSize: null,
            cursorSelector: null
        );
    }
}
