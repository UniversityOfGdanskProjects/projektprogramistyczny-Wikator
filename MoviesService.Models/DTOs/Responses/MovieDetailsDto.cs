namespace MoviesService.Models.DTOs.Responses;

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
    bool IsFavourite,
    ReviewIdAndScoreDto? UserReview,
    int ReviewsCount,
    IEnumerable<ActorDto> Actors,
    IEnumerable<CommentDto> Comments,
    IEnumerable<string> Genres);