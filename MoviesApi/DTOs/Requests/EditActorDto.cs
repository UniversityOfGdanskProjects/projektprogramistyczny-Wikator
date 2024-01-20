using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class EditActorDto
{
    [Required] public required string FirstName { get; init; }
    [Required] public required string LastName { get; init; }
    [Required] public required DateOnly DateOfBirth { get; init; }
    public string? Biography { get; init; }
}
