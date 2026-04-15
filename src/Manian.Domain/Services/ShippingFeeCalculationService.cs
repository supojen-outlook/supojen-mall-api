// 路徑: src/Manian.Domain/Services/ShippingFeeCalculationService.cs

using Manian.Domain.Entities.Carts;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Orders;
using Manian.Domain.Repositories.Products;
using Manian.Domain.ValueObjects;

namespace Manian.Domain.Services;

/// <summary>
/// 運費計算服務
/// 
/// 職責：
/// - 根據訂單項目計算運費
/// - 應用運費規則
/// - 處理免運條件
/// - 透過 SKU 查詢計量單位並轉換為基準單位
/// 
/// 設計原則：
/// - 遵循單一職責原則 (SRP)
/// - 依賴 IShippingRuleRepository 查詢運費規則
/// - 依賴 IProductRepository 查詢 SKU
/// - 依賴 IUnitOfMeasureRepository 查詢計量單位
/// - 可單元測試
/// 
/// 架構位置：
/// - 位於 Domain 層（領域層）
/// - 屬於領域服務 (Domain Service)
/// - 不依賴任何基礎設施層的實作
/// 
/// 使用場景：
/// - 購物車頁面計算運費
/// - 結帳頁面計算最終金額
/// - 訂單建立時計算運費
/// 
/// 計算邏輯：
/// 1. 查詢所有啟用的運費規則，按優先級排序
/// 2. 透過 OrderItem.SkuId 查詢 SKU 實體
/// 3. 透過 SKU.UnitOfMeasureId 查詢計量單位
/// 4. 使用計量單位的 ConversionToBase 轉換為基準單位（id=1）
/// 5. 計算訂單總金額和總數量（基準單位）
/// 6. 遍歷每個運費規則，檢查是否符合條件
/// 7. 返回第一個符合條件的規則的運費
/// 8. 如果沒有符合條件的規則，返回預設運費
/// 
/// 規則類型：
/// - quantity：按數量計算（轉換為基準單位 id=1 的數量）
/// - amount：按金額計算（基於訂單總金額，不含運費）
/// 
/// 注意事項：
/// - 按數量計算時，使用 ConversionToBase 轉換為基準單位
/// - 按金額計算時，基於訂單總金額（不含運費）
/// - 規則按優先級排序，數字越小優先級越高
/// </summary>
public class ShippingFeeCalculationService
{
    /// <summary>
    /// 運費規則倉儲介面
    /// 
    /// 用途：
    /// - 查詢運費規則
    /// - 取得啟用的運費規則
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Orders/ShippingRuleRepository.cs）
    /// - 提供泛型方法 GetAllAsync 查詢實體集合
    /// 
    /// 查詢優化：
    /// - 一次性查詢所有啟用的規則
    /// - 按優先級排序
    /// </summary>
    private readonly IShippingRuleRepository _shippingRuleRepository;

    /// <summary>
    /// 商品倉儲介面
    /// 
    /// 用途：
    /// - 查詢 SKU 實體
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/ProductRepository.cs）
    /// - 提供泛型方法 GetSkuAsync 查詢 SKU
    /// 
    /// 查詢優化：
    /// - 批次查詢 SKU，減少資料庫往返
    /// </summary>
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// 計量單位倉儲介面
    /// 
    /// 用途：
    /// - 查詢計量單位實體
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作（見 Infrastructure/Repositories/Products/UnitOfMeasureRepository.cs）
    /// - 提供泛型方法 GetByIdAsync 查詢計量單位
    /// 
    /// 查詢優化：
    /// - 批次查詢計量單位，減少資料庫往返
    /// </summary>
    private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;

    /// <summary>
    /// 建構函式 - 初始化服務並注入依賴
    /// 
    /// 設計原則：
    /// - 使用建構函式注入 (Constructor Injection)
    /// - 依賴介面而非實作，符合依賴倒置原則
    /// - 依賴是 readonly，確保不可變性
    /// </summary>
    /// <param name="shippingRuleRepository">運費規則倉儲，用於查詢運費規則</param>
    /// <param name="productRepository">商品倉儲，用於查詢 SKU</param>
    /// <param name="unitOfMeasureRepository">計量單位倉儲，用於查詢計量單位</param>
    public ShippingFeeCalculationService(
        IShippingRuleRepository shippingRuleRepository,
        IProductRepository productRepository,
        IUnitOfMeasureRepository unitOfMeasureRepository)
    {
        _shippingRuleRepository = shippingRuleRepository;
        _productRepository = productRepository;
        _unitOfMeasureRepository = unitOfMeasureRepository;
    }

    /// <summary>
    /// 根據訂單項目計算運費
    /// 
    /// 執行流程：
    /// 1. 查詢所有啟用的運費規則，按優先級排序
    /// 2. 透過 OrderItem.SkuId 查詢 SKU 實體
    /// 3. 透過 SKU.UnitOfMeasureId 查詢計量單位
    /// 4. 使用計量單位的 ConversionToBase 轉換為基準單位（id=1）
    /// 5. 計算訂單總金額和總數量（基準單位）
    /// 6. 遍歷每個運費規則，檢查是否符合條件
    /// 7. 返回第一個符合條件的規則的運費
    /// 8. 如果沒有符合條件的規則，返回預設運費
    /// 
    /// 設計考量：
    /// - 按優先級排序，確保規則按正確順序匹配
    /// - 支援多種運費計算方式（按數量、按金額）
    /// - 處理免運條件（運費為 0）
    /// - 使用計量單位轉換率，確保數量計算準確
    /// 
    /// 注意事項：
    /// - 按數量計算時，使用 ConversionToBase 轉換為基準單位
    /// - 按金額計算時，基於訂單總金額（不含運費）
    /// - 規則按優先級排序，數字越小優先級越高
    /// </summary>
    /// <param name="cartItems">購物車項目</param>
    /// <returns>計算後的運費金額</returns>
    public async Task<decimal> CalculateShippingFeeAsync(IEnumerable<CartItem> orderItems)
    {
        // ========== 第一步：查詢所有啟用的運費規則，按優先級排序 ==========
        var rules = await _shippingRuleRepository.GetAllAsync(
            q => q.Where(r => r.IsActive)
                  .OrderBy(r => r.Priority));

        // 如果沒有規則，返回預設運費（0）
        if (rules == null || !rules.Any())
            return 0;

        // ========== 第二步：收集所有 SKU ID ==========
        var skuIds = orderItems
            .Select(item => item.SkuId)
            .Select(id => id)
            .Distinct()
            .ToList();

        // ========== 第三步：批次查詢所有 SKU ==========
        var skus = await _productRepository.GetSkusByIdsAsync(skuIds);
        var skuDict = skus.ToDictionary(s => s.Id);

        // ========== 第四步：收集所有計量單位 ID ==========
        var unitOfMeasureIds = skus
            .Where(s => s.UnitOfMeasureId.HasValue)
            .Select(s => s.UnitOfMeasureId!.Value)
            .Distinct()
            .ToList();

        // ========== 第五步：批次查詢所有計量單位 ==========
        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(
            q => q.Where(u => unitOfMeasureIds.Contains(u.Id)));
        var unitOfMeasureDict = unitOfMeasures.ToDictionary(u => u.Id);

        // ========== 第六步：計算訂單總金額和總數量（基準單位） ==========
        decimal totalAmount = 0;
        int totalQuantity = 0;

        foreach (var item in orderItems)
        {
            // 計算訂單總金額
            totalAmount += item.UnitPrice * item.Quantity;

            // 查詢 SKU 和計量單位
            if (!skuDict.TryGetValue(item.SkuId, out var sku) || !sku.UnitOfMeasureId.HasValue)
                continue;

            if (!unitOfMeasureDict.TryGetValue(sku.UnitOfMeasureId.Value, out var unitOfMeasure))
                continue;

            // 轉換為基準單位（id=1）的數量
            // 計算公式：實際數量 * 轉換率
            totalQuantity += item.Quantity * unitOfMeasure.ConversionToBase;
        }

        // ========== 第七步：遍歷每個運費規則，檢查是否符合條件 ==========
        foreach (var rule in rules)
        {
            // 檢查規則條件是否有效
            if (rule.Condition == null || !IsConditionValid(rule.Condition))
                continue;

            // 檢查是否符合條件
            if (MatchesCondition(rule.Condition, totalAmount, totalQuantity))
            {
                // 返回符合條件的規則的運費
                return rule.ShippingFee;
            }
        }

        // ========== 第八步：如果沒有符合條件的規則，返回預設運費 ==========
        // 預設運費可以從設定檔讀取，這裡暫時返回 0
        return 0;
    }

    /// <summary>
    /// 根據訂單項目計算運費
    /// 
    /// 執行流程：
    /// 1. 查詢所有啟用的運費規則，按優先級排序
    /// 2. 透過 OrderItem.SkuId 查詢 SKU 實體
    /// 3. 透過 SKU.UnitOfMeasureId 查詢計量單位
    /// 4. 使用計量單位的 ConversionToBase 轉換為基準單位（id=1）
    /// 5. 計算訂單總金額和總數量（基準單位）
    /// 6. 遍歷每個運費規則，檢查是否符合條件
    /// 7. 返回第一個符合條件的規則的運費
    /// 8. 如果沒有符合條件的規則，返回預設運費
    /// 
    /// 設計考量：
    /// - 按優先級排序，確保規則按正確順序匹配
    /// - 支援多種運費計算方式（按數量、按金額）
    /// - 處理免運條件（運費為 0）
    /// - 使用計量單位轉換率，確保數量計算準確
    /// 
    /// 注意事項：
    /// - 按數量計算時，使用 ConversionToBase 轉換為基準單位
    /// - 按金額計算時，基於訂單總金額（不含運費）
    /// - 規則按優先級排序，數字越小優先級越高
    /// </summary>
    /// <param name="cartItems">購物車項目</param>
    /// <returns>計算後的運費金額</returns>
    public async Task<decimal> CalculateShippingFeeAsync(IEnumerable<OrderItem> orderItems)
    {
        // ========== 第一步：查詢所有啟用的運費規則，按優先級排序 ==========
        var rules = await _shippingRuleRepository.GetAllAsync(
            q => q.Where(r => r.IsActive)
                  .OrderBy(r => r.Priority));

        // 如果沒有規則，返回預設運費（0）
        if (rules == null || !rules.Any())
            return 0;

        // ========== 第二步：收集所有 SKU ID ==========
        var skuIds = orderItems
            .Select(item => item.SkuId)
            .Select(id => id)
            .Distinct()
            .ToList();

        // ========== 第三步：批次查詢所有 SKU ==========
        var skus = await _productRepository.GetSkusByIdsAsync(skuIds);
        var skuDict = skus.ToDictionary(s => s.Id);

        // ========== 第四步：收集所有計量單位 ID ==========
        var unitOfMeasureIds = skus
            .Where(s => s.UnitOfMeasureId.HasValue)
            .Select(s => s.UnitOfMeasureId!.Value)
            .Distinct()
            .ToList();

        // ========== 第五步：批次查詢所有計量單位 ==========
        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(
            q => q.Where(u => unitOfMeasureIds.Contains(u.Id)));
        var unitOfMeasureDict = unitOfMeasures.ToDictionary(u => u.Id);

        // ========== 第六步：計算訂單總金額和總數量（基準單位） ==========
        decimal totalAmount = 0;
        int totalQuantity = 0;

        foreach (var item in orderItems)
        {
            // 計算訂單總金額
            totalAmount += item.UnitPrice * item.Quantity;

            // 查詢 SKU 和計量單位
            if (!skuDict.TryGetValue(item.SkuId, out var sku) || !sku.UnitOfMeasureId.HasValue)
                continue;

            if (!unitOfMeasureDict.TryGetValue(sku.UnitOfMeasureId.Value, out var unitOfMeasure))
                continue;

            // 轉換為基準單位（id=1）的數量
            // 計算公式：實際數量 * 轉換率
            totalQuantity += item.Quantity * unitOfMeasure.ConversionToBase;
        }

        // ========== 第七步：遍歷每個運費規則，檢查是否符合條件 ==========
        foreach (var rule in rules)
        {
            // 檢查規則條件是否有效
            if (rule.Condition == null || !IsConditionValid(rule.Condition))
                continue;

            // 檢查是否符合條件
            if (MatchesCondition(rule.Condition, totalAmount, totalQuantity))
            {
                // 返回符合條件的規則的運費
                return rule.ShippingFee;
            }
        }

        // ========== 第八步：如果沒有符合條件的規則，返回預設運費 ==========
        // 預設運費可以從設定檔讀取，這裡暫時返回 0
        return 0;
    }

    /// <summary>
    /// 檢查規則條件是否有效
    /// </summary>
    /// <param name="condition">運費規則條件</param>
    /// <returns>如果條件有效，返回 true；否則返回 false</returns>
    private bool IsConditionValid(ShippingRuleCondition condition)
    {
        return condition switch
        {
            QuantityShippingCondition q => q.IsValid(),
            AmountShippingCondition a => a.IsValid(),
            _ => false
        };
    }

    /// <summary>
    /// 檢查是否符合條件
    /// </summary>
    /// <param name="condition">運費規則條件</param>
    /// <param name="totalAmount">訂單總金額</param>
    /// <param name="totalQuantity">訂單總數量（基準單位）</param>
    /// <returns>如果符合條件，返回 true；否則返回 false</returns>
    private bool MatchesCondition(ShippingRuleCondition condition, decimal totalAmount, int totalQuantity)
    {
        return condition switch
        {
            QuantityShippingCondition q => q.Matches(totalQuantity),
            AmountShippingCondition a => a.Matches((int)totalAmount),
            _ => false
        };
    }
}
