using System;
using Manian.Domain.Entities.Products;
using Manian.Domain.Repositories.Products;
using Manian.Infrastructure.Persistence;

namespace Manian.Infrastructure.Repositories.Products;

public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(MainDbContext context) : base(context) {}
}
