using System;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Memberships;

/// <summary>
/// 查詢角色列表的請求物件
/// 繼承自 IRequest<IEnumerable<Role>>，表示這個請求預期回傳一個 Role 的集合
/// </summary>
public class RolesQuery : IRequest<IEnumerable<Role>>
{
    /// <summary>
    /// 每頁筆數
    /// 可為 null，表示不限制回傳筆數
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// 最後一筆資料的 ID（通常用於 keyset pagination / seek method）
    /// 用來取得「大於這個 ID」的下一批資料，比傳統的 Skip 分頁更有效率
    /// </summary>
    public int? LastId { get; set; }

    /// <summary>
    /// 搜尋關鍵字 (角色名稱或代碼)
    /// 會同時搜尋 Name、Code 和 Description 欄位
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// 角色代碼篩選
    /// 如果提供此陣列，只回傳代碼有在陣列內的角色
    /// 注意：目前的實作邏輯中，Codes 的優先級高於 Search（一旦有 Codes 就忽略 Search）
    /// </summary>
    public string[]? Codes { get; set; }
}

/// <summary>
/// 角色查詢處理器 (Handler)
/// 負責處理 RolesQuery 請求，並回傳 IEnumerable<Role>
/// 實作 MediatR 的 IRequestHandler 介面
/// </summary>
public class RolesQueryHandler : IRequestHandler<RolesQuery, IEnumerable<Role>>
{
    /// <summary>
    /// 角色仓储介面，用於資料存取操作
    /// 透過依賴注入在建構子中取得
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 建構函式，透過依賴注入取得角色仓储實例
    /// </summary>
    /// <param name="roleRepository">角色仓储介面</param>
    public RolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// 非同步處理角色查詢請求
    /// 這是 MediatR 模式的核心方法，當有人發送 RolesQuery 時會自動呼叫此方法
    /// </summary>
    /// <param name="request">角色查詢請求物件，包含過濾、搜尋、分頁條件</param>
    /// <returns>回傳符合條件的角色集合</returns>
    public Task<IEnumerable<Role>> HandleAsync(RolesQuery request)
    {
        // 呼叫 BuildQuery 方法建立篩選邏輯，然後傳給 Repository 去資料庫抓資料
        return _roleRepository.GetAllAsync(BuildQuery(request));
    }

    /// <summary>
    /// 建構角色查詢表達式
    /// 根據 request 中的條件，動態建立出一個 Func，用來篩選 IQueryable<Role>
    /// 這種寫法可以將查詢邏輯集中在這裡，保持 Repository 介面的簡潔（Repository 只需接受篩選條件）
    /// </summary>
    /// <param name="request">角色查詢請求物件，內含所有篩選條件</param>
    /// <returns>
    /// 回傳一個 Func<IQueryable<Role>, IQueryable<Role>>
    /// 輸入一個 IQueryable<Role>，經過 Where、Take 等操作後，輸出另一個 IQueryable<Role>
    /// 這讓 Repository 的 GetAllAsync 方法可以保持單純，只需執行這個 Func 即可
    /// </returns>
    private static Func<IQueryable<Role>, IQueryable<Role>> BuildQuery(RolesQuery request)
    {
        // 回傳一個委派，這個委派接受原始的 query，回傳處理後的 query
        return query =>
        {
            // ===== 第一階段：套用代碼篩選 (優先度最高) =====
            // 注意：一旦有 Codes 條件，就會直接回傳，後面的 Search 和分頁條件都會被忽略！
            // 這可能是設計上的選擇（如果指定特定代碼，就不需要分頁和搜尋），但也有可能是 Bug。
            if (request.Codes != null && request.Codes.Length > 0)
            {
                // 篩選出 Code 存在於 request.Codes 陣列中的角色
                query = query.Where(x => request.Codes.Contains(x.Code));
                return query; // 直接回傳，不繼續執行下面的搜尋和分頁
            }

            // ===== 第二階段：套用搜尋條件 =====
            // 如果 Search 參數有值，就在 Name、Code、Description 中進行模糊搜尋
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower(); // 前後去空白並轉小寫，達到不區分大小寫的效果
                query = query.Where(x =>
                    x.Name.ToLower().Contains(searchTerm) ||          // 角色名稱包含關鍵字
                    x.Code.ToLower().Contains(searchTerm) ||          // 角色代碼包含關鍵字
                    (x.Description != null && x.Description.ToLower().Contains(searchTerm)) // 描述如果不為空，也檢查是否包含關鍵字
                );
            }

            // ===== 第三階段：套用 Keyset Pagination (基於最後一筆 ID 的分頁) =====
            // 這種分頁方式比傳統的 Skip 更穩定，尤其是資料量大時
            if (request.LastId != null)
            {
                // 只取 ID 大於 LastId 的資料（假設 ID 是遞增的）
                // 這意味著前端需要記住最後一筆資料的 ID，並在下一頁請求時傳回來
                query = query.Where(x => x.Id > request.LastId.Value);
            }

            // ===== 第四階段：限制回傳筆數 =====
            // 如果 Size 有指定，就只取前 Size 筆
            if (request.Size != null)
            {
                query = query.Take(request.Size.Value);
            }

            // 回傳最終組合好的 IQueryable
            // 注意：此時還沒真的去資料庫執行，要到被 foreach 或 ToList() 時才會實際查詢
            return query;
        };        
    }
}