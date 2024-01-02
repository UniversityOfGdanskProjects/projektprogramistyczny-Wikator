namespace MoviesApi.DTOs;

public record ActorDto(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, string? Biography,
    string? PictureUri);
