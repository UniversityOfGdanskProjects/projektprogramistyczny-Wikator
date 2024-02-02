using System.Security.Claims;

namespace MoviesService.Services.Contracts;

public interface IUserClaimsProvider
{
    public Guid GetUserId(ClaimsPrincipal user);
    public Guid? GetUserIdOrDefault(ClaimsPrincipal user);
}