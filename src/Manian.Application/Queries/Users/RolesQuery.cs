using Manian.Application.Models;
using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Shared.Mediator.Interface;

namespace Manian.Application.Queries.Users;

/// <summary>
/// 查詢角色列表的請求物件
/// 支援 Cursor 分頁 (僅向後)
/// </summary>
public class RolesQuery : IRequest<Pagination<Role>>
{
    /// <summary>
    /// 每頁筆數 (可選)
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// 當前頁面的游標 (可選)
    /// 用於獲取下一頁的數據
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// 搜尋關鍵字 (可選)
    /// 用於搜尋角色名稱 或 代碼
    /// </summary>
    public string? Search { get; set; }
}

/// <summary>
/// 角色查詢處理器
/// </summary>
public class RolesQueryHandler : IRequestHandler<RolesQuery, Pagination<Role>>
{
    private readonly IRoleRepository _roleRepository;

    public RolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Pagination<Role>> HandleAsync(RolesQuery request)
    {
        // 解析 Cursor (假設 Cursor 是 Role 的 Id)
        long? cursorId = null;
        if (!string.IsNullOrEmpty(request.Cursor) && long.TryParse(request.Cursor, out var id))
        {
            cursorId = id;
        }

        var roles = await _roleRepository.GetAllAsync(query =>
        {
            // 1. 搜尋過濾
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.ToLower();
                query = query.Where(r => 
                    r.Name.ToLower().Contains(term) || 
                    r.Code.ToLower().Contains(term));
            }

            // 2. Cursor 過濾與排序 (固定向後翻頁)
            if (cursorId.HasValue)
            {
                // ID 大於 Cursor
                query = query.Where(r => r.Id > cursorId.Value);
            }

            // 始終按 ID 升序排列
            query = query.OrderBy(r => r.Id);

            // 3. 數量限制
            // 多取一筆用於判斷是否還有下一頁
            var fetchSize = request.Size.HasValue ? request.Size.Value + 1 : int.MaxValue;
            query = query.Take(fetchSize);

            return query;
        });

        // 4. 構建分頁結果
        // 指定 cursorSelector 為 r => r.Id.ToString()
        return new Pagination<Role>(
            items: roles,
            requestedSize: request.Size,
            cursorSelector: r => r.Id.ToString()
        );
    }
}
