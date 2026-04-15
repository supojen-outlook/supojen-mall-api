using Manian.Domain.Entities.Memberships;
using Manian.Domain.Repositories.Memberships;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Memberships;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(MainDbContext context, string idPropertyName = "Id") : base(context, idPropertyName) {}
}
