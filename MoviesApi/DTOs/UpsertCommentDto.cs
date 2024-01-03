using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs;

public class UpsertCommentDto
{
    [Required]
    public Guid MovieId { get; init; }
    public required string Text { get; init; }
}