using MoviesApi.Enums;

namespace MoviesApi.Models
{
	public record User(int Id, string Name, string Email, Role Role);
}
