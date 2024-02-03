using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class AddMovieDto
{
    [Required] public required string Title { get; init; }
    [Required] public required string Description { get; init; }
    [Required] public bool? InTheaters { get; init; }
    [Required] public DateOnly? ReleaseDate { get; init; }
    [Range(0, 18)] [Required] public int? MinimumAge { get; init; }
    [Url] public string? TrailerUrl { get; init; }
    public byte[]? FileContent { get; init; }
    public IEnumerable<Guid> ActorIds { get; init; } = Enumerable.Empty<Guid>();
    public IEnumerable<string> Genres { get; init; } = Enumerable.Empty<string>();
}