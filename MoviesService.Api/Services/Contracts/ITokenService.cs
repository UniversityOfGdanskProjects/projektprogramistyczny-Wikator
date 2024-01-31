using MoviesService.Models;

namespace MoviesService.Services.Contracts;

public interface ITokenService
{
	string CreateToken(User user);
}