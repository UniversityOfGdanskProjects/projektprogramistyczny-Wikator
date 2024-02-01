using System.Security.Claims;
using MoviesService.Services.Contracts;

namespace MoviesService.Services;

public class UserClaimsProvider : IUserClaimsProvider
{
    /// <summary>
    /// Gets the user id from the claims principal
    /// </summary>
    /// <param name="user">User's claims principal</param>
    /// <exception cref="NullReferenceException">User not logged in, or id not present in claims</exception>
    /// <returns>User's id</returns>
    public Guid GetUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)!.Value);
    }
}