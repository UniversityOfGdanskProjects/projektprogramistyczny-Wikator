using System.Security.Claims;
using MoviesService.Api.Services.Contracts;

namespace MoviesService.Api.Services;

public class UserClaimsProvider : IUserClaimsProvider
{
    /// <summary>
    ///     Gets the user id from the claims principal
    /// </summary>
    /// <param name="user">User's claims principal</param>
    /// <exception cref="NullReferenceException">User not logged in, or id not present in claims</exception>
    /// <returns>User's id</returns>
    public Guid GetUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)!.Value);
    }

    /// <summary>
    ///     Gets the user id from the claims principal, or null if not present
    /// </summary>
    /// <param name="user">User's claims principal</param>
    /// <returns></returns>
    public Guid? GetUserIdOrDefault(ClaimsPrincipal user)
    {
        var userId = user.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        return userId is null ? null : Guid.Parse(userId);
    }
}