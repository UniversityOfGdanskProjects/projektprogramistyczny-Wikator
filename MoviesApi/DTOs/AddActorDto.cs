namespace MoviesApi.DTOs;

public class AddActorDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string DateOfBirth { get; init; }
    public string? Biography { get; init; }
}
