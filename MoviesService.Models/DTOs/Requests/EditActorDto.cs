using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class EditActorDto
{
    [Required] public required string FirstName { get; init; }
    [Required] public required string LastName { get; init; }
    [Required] public required DateOnly DateOfBirth { get; init; }
    public string? Biography { get; init; }
}