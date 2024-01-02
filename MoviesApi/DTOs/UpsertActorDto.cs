namespace MoviesApi.DTOs;

public class UpsertActorDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string? Biography { get; init; }
    public string? FileName { get; init; }
    public byte[]? FileContent { get; init; }
}
