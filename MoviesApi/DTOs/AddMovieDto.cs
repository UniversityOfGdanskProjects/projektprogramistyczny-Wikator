using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs;

public class AddMovieDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    [Required] public bool? InTheaters { get; init; }
    [Required] public DateOnly? ReleaseDate { get; init; }
    [Range(0, 18), Required] public int? MinimumAge { get; init; }
    [Url] public string? TrailerUrl { get; init; }
    public string? FileName { get; init; }
    public byte[]? FileContent { get; init; }
    public IEnumerable<int> ActorIds { get; init; } = Enumerable.Empty<int>();
}
