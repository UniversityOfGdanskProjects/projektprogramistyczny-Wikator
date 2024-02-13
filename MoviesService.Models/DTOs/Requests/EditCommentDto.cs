using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class EditCommentDto
{
    [Required] public required string Text { get; init; }
}