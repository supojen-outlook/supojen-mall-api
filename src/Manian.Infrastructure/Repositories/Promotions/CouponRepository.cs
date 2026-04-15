using System;
using Manian.Domain.Entities.Promotions;
using Manian.Domain.Repositories.Promotions;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Promotions;

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(MainDbContext context) : base(context) {}
}
