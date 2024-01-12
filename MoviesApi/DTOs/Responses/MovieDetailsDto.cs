namespace MoviesApi.DTOs.Responses;

public record MovieDetailsDto(
    Guid Id,
    string Title,
    string Description,
    bool InTheaters,
    double AverageScore,
    string? TrailerUrl,
    DateOnly ReleaseDate,
    int MinimumAge,
    string? PictureUri,
    bool OnWatchlist,
    bool IsFavorite,
    IEnumerable<ActorDto> Actors,
    IEnumerable<CommentDto> Comments);