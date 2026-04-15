using System;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Products;

public class UnitOfMeasureRepository : Repository<UnitOfMeasure>, IUnitOfMeasureRepository
{
    public UnitOfMeasureRepository(MainDbContext context): base(context) {}
}
