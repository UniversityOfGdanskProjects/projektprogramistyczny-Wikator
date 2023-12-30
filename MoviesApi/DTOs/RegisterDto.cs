using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs;

public class RegisterDto
{
    [EmailAddress]
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Password { get; init; }
}
