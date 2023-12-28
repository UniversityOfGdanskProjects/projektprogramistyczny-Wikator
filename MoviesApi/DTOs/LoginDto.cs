using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs;

public class LoginDto
{
	[EmailAddress]
	public required string Email { get; init; }
	public required string Password { get; init; }
}