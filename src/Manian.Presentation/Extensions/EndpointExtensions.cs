using Manian.Presentation.Endpoints.Memberships;


namespace Manian.Presentation.Extensions;

public static class EndpointExtensions
{
    public static void MapMembershipRelatedEndpoint(this IEndpointRouteBuilder app)
    {
        // 註冊所有會員相關的 API 端點
        app.MapMemberships();

        // 註冊所有身份認證相關的 API 端點
        app.MapIdentityEndpoints();

        // 註冊所有點數交易相關的 API 端點
        app.MapPointTransactionEndpoints();
    }

    public static void MapOrderRelatedEndpoints(this IEndpointRouteBuilder app)
    {
        
    }
}
