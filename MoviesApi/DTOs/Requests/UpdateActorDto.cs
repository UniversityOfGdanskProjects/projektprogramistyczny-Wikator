namespace MoviesApi.DTOs.Requests;

public class UpdateActorDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Biography { get; init; }
    public string? FileName { get; init; }
    public byte[]? FileContent { get; init; }
}
