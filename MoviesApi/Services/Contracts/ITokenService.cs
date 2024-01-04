using MoviesApi.Models;

namespace MoviesApi.Services.Contracts;

public interface ITokenService
{
	string CreateToken(User user);
}