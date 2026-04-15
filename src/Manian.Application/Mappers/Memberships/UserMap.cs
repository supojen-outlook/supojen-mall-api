using System;
using Manian.Application.Models.Memberships;
using Manian.Domain.Entities.Memberships;
using Mapster;

namespace Manian.Application.Mappers.Memberships;

public class UserMap : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, ProfileResponse>()
            .Map(dest => dest.Points, src => src.PointAccount.Balance);
    }
}
