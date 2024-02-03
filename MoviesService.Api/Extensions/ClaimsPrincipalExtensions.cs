using System.Security.Claims;

namespace MoviesService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    ///     Gets the user id from the claims principal
    /// </summary>
    /// <param name="claimsPrincipal">User's claims principal</param>
    /// <exception cref="NullReferenceException">User not logged in, or id not present in claims</exception>
    /// <returns>User's id</returns>
    public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return Guid.Parse(claimsPrincipal.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)!.Value);
    }
}