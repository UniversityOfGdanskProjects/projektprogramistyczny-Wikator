using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class AddActorDto
{
    [Required] public required string FirstName { get; init; }
    [Required] public required string LastName { get; init; }
    [Required] public DateOnly? DateOfBirth { get; init; }
    public string? Biography { get; init; }
    public byte[]? FileContent { get; init; }
}
