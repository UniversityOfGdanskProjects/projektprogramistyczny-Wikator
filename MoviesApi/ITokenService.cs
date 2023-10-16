using MoviesApi.Models;

namespace MoviesApi
{
	public interface ITokenService
	{
		string CreateToken(User user);
	}
}
