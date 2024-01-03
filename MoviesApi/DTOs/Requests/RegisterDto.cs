using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class RegisterDto
{
    [EmailAddress]
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Password { get; init; }
}
