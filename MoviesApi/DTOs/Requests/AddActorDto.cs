using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class AddActorDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    [Required] public DateOnly? DateOfBirth { get; init; }
    public string? Biography { get; init; }
    public string? FileName { get; init; }
    public byte[]? FileContent { get; init; }
}
