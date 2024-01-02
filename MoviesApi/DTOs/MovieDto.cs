namespace MoviesApi.DTOs;

public record MovieDto(int Id, string Title, string Description, double AverageScore, string? PictureUri, IEnumerable<ActorDto> Actors);