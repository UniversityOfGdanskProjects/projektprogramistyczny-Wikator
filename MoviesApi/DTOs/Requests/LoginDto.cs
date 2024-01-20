using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class LoginDto
{
	[EmailAddress] public required string Email { get; init; }
	[Required, MinLength(6)] public required string Password { get; init; }
}