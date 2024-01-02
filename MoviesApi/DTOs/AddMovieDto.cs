namespace MoviesApi.DTOs;

public class AddMovieDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public IEnumerable<int> ActorIds { get; init; } = Enumerable.Empty<int>();
    public string? FileName { get; init; }
    public byte[]? FileContent { get; init; }
}
