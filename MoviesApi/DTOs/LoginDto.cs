namespace MoviesApi.DTOs;

public class LoginDto
{
	public required string Name { get; init; }
	public required string Password { get; init; }
}