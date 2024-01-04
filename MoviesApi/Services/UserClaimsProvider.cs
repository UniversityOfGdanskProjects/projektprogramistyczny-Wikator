using System.Security.Claims;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Services;

public class UserClaimsProvider : IUserClaimsProvider
{
    public Guid GetUserId(ClaimsPrincipal claimsPrincipal) =>
        Guid.Parse(claimsPrincipal.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value!);
}
