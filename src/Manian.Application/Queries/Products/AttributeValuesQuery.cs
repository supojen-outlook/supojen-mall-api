using System;
using Manian.Application.Models;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Products;

/// <summary>
/// 屬性值查詢請求
/// 
/// 用途：根據屬性鍵 ID 查詢所有關聯的屬性值
/// 
/// 使用場景：
/// - 商品發布時顯示屬性選項（如顏色、尺寸等）
/// - 商品篩選功能
/// - 屬性管理介面
/// 
/// 設計模式：
/// - 實作 CQRS 模式中的 Query 部分
/// - 與 AttributeValuesQueryHandler 配合使用
/// </summary>
public class AttributeValuesQuery : IRequest<Pagination<AttributeValue>>
{
    /// <summary>
    /// 屬性鍵 ID
    /// 
    /// 用途：
    /// - 識別要查詢的屬性鍵
    /// - 必須是資料庫中已存在的屬性鍵 ID
    /// 
    /// 驗證規則：
    /// - 必須為正整數
    /// - 必須對應資料庫中存在的屬性鍵
    /// 
    /// 範例：
    /// - 1：查詢顏色屬性的所有值（紅、藍、黑等）
    /// - 2：查詢尺寸屬性的所有值（S、M、L、XL等）
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// 屬性值查詢處理器
/// 
/// 職責：
/// - 接收 AttributeValuesQuery 請求
/// - 從資料庫取得指定屬性鍵的所有屬性值
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AttributeValuesQuery, IEnumerable<AttributeValue>> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得 IAttributeKeyRepository
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IAttributeKeyRepository
/// - 邏輯清晰，方便單元測試
/// </summary>
public class AttributeValuesQueryHandler : IRequestHandler<AttributeValuesQuery, Pagination<AttributeValue>>
{
    /// <summary>
    /// 屬性鍵倉儲介面
    /// 
    /// 用途：
    /// - 存取屬性鍵相關資料
    /// - 提供 GetValuesAsync 方法查詢屬性值
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Repositories/Products/AttributeKeyRepository.cs
    /// - 使用 EF Core 實作查詢邏輯
    /// </summary>
    private readonly IAttributeKeyRepository _repository;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="repository">屬性鍵倉儲，用於查詢屬性值</param>
    public AttributeValuesQueryHandler(IAttributeKeyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 處理屬性值查詢請求的主要方法
    /// 
    /// 執行流程：
    /// 1. 接收查詢請求（包含屬性鍵 ID）
    /// 2. 呼叫 Repository 的 GetValuesAsync 方法
    /// 3. 回傳符合條件的屬性值集合
    /// 
    /// 返回值：
    /// - IEnumerable<AttributeValue>：屬性值集合
    /// - 無資料時返回空集合（非 null）
    /// </summary>
    /// <param name="request">屬性值查詢請求物件，包含屬性鍵 ID</param>
    /// <returns>符合條件的屬性值集合</returns>
    public async Task<Pagination<AttributeValue>> HandleAsync(AttributeValuesQuery request)
    {
        // 呼叫 Repository 查詢屬性值
        // 見 IAttributeKeyRepository.GetValuesAsync 的實作
        var values = await _repository.GetValuesAsync(request.Id);
    
        return new Pagination<AttributeValue>(
            items: values,
            requestedSize: null,
            cursorSelector: null
        );
    }
}
