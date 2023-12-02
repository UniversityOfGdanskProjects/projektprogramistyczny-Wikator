using MoviesApi.Models;

namespace MoviesApi.DTOs;

public class UserDto
{
	public required string Name { get; init; }
	public Role Role { get; init; }
	public required string Token { get; init; }
}