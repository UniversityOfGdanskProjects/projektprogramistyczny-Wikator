namespace MoviesApi.DTOs.Responses;

public record MovieDto(
    Guid Id,
    string Title,
    string Description,
    bool InTheaters,
    double AverageScore,
    string? TrailerUrl,
    DateOnly ReleaseDate,
    int MinimumAge,
    string? PictureUri,
    IEnumerable<ActorDto> Actors,
    IEnumerable<CommentDto> Comments);