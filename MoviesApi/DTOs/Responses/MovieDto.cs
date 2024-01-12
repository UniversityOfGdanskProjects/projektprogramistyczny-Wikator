namespace MoviesApi.DTOs.Responses;

public record MovieDto(Guid Id, string Title, double AverageScore, int MinimumAge, string? PictureUri, bool OnWatchlist,
    bool IsFavourite);
