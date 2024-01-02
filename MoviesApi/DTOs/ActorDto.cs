namespace MoviesApi.DTOs;

public record ActorDto(int Id, string FirstName, string LastName, DateOnly DateOfBirth, string? Biography,
    string? PictureUri);
