using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class AddCommentDto
{
    [Required] public Guid MovieId { get; init; }
    [Required] public required string Text { get; init; }
}