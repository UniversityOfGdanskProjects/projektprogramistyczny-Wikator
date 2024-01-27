namespace MoviesApi.Models;

public record ReviewAverageAndCount(Guid MovieId, double Average, int Count);
