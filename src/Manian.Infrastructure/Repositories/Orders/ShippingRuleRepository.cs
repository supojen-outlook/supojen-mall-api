using System;
using Manian.Domain.Entities.Orders;
using Manian.Domain.Repositories.Orders;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Orders;

public class ShippingRuleRepository : Repository<ShippingRule>, IShippingRuleRepository
{
    public ShippingRuleRepository(MainDbContext context) : base(context) {}
}
