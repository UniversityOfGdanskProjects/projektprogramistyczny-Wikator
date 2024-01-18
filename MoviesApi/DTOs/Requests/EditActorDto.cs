namespace MoviesApi.DTOs.Requests;

public class EditActorDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public string? Biography { get; init; }
}
