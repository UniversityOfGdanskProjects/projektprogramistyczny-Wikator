using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class RegisterDto
{
    [EmailAddress] [Required] public required string Email { get; init; }
    [Required] public required string Name { get; init; }
    [Required] [MinLength(6)] public required string Password { get; init; }
}