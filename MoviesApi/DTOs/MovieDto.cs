namespace MoviesApi.DTOs;

public record MovieDto(int Id, string Title, string Description, double AverageScore, IEnumerable<ActorDto> Actors);