namespace MoviesApi.DTOs;

public record MovieDto(int Id, string Title, string Description, IEnumerable<ActorDto> Actors);