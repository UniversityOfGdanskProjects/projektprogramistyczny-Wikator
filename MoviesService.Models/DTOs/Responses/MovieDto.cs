namespace MoviesService.Models.DTOs.Responses;

public record MovieDto(
    Guid Id,
    string Title,
    double AverageScore,
    int MinimumAge,
    string? PictureUri,
    bool OnWatchlist,
    bool IsFavourite,
    ReviewIdAndScoreDto? UserReview,
    int ReviewsCount,
    List<string> Genres);