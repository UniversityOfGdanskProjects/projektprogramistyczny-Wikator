﻿using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class EditMovieDto
{
    [Required]  public required string Title { get; init; }
    [Required]  public required string Description { get; init; }
    [Required] public bool? InTheaters { get; init; }
    [Required] public DateOnly? ReleaseDate { get; init; }
    [Range(0, 18), Required] public int? MinimumAge { get; init; }
    [Url] public string? TrailerUrl { get; init; }
    public IEnumerable<Guid> ActorIds { get; init; } = Enumerable.Empty<Guid>();
    public IEnumerable<string> Genres { get; init; } = Enumerable.Empty<string>();
}