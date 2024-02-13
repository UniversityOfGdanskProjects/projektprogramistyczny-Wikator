using MoviesService.Models;

namespace MoviesService.Api.Services.Contracts;

public interface ITokenService
{
    string CreateToken(User user);
}