using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class AddCommentDto
{
    [Required]
    public Guid MovieId { get; init; }
    public required string Text { get; init; }
}