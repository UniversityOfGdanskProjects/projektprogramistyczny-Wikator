using System.Security.Claims;

namespace MoviesApi.Services.Contracts;

public interface IUserClaimsProvider
{
    Guid GetUserId(ClaimsPrincipal claimsPrincipal);
}