using System.Security.Claims;

namespace MoviesService.Api.Services.Contracts;

public interface IUserClaimsProvider
{
    public Guid GetUserId(ClaimsPrincipal user);
    public Guid? GetUserIdOrDefault(ClaimsPrincipal user);
}