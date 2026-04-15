using Manian.Domain.Entities.Carts;
using Manian.Domain.Repositories.Carts;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Carts;

public class CartItemRepository : Repository<CartItem>, ICartItemRepository
{
    public CartItemRepository(MainDbContext context) : base(context) {}
}
