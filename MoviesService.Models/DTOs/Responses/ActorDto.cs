namespace MoviesService.Models.DTOs.Responses;

public record ActorDto(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Biography,
    string? PictureUri);