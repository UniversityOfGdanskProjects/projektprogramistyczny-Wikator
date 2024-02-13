using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class AddActorDto
{
    [Required] public required string FirstName { get; init; }
    [Required] public required string LastName { get; init; }
    [Required] public DateOnly? DateOfBirth { get; init; }
    public string? Biography { get; init; }
    public byte[]? FileContent { get; init; }
}