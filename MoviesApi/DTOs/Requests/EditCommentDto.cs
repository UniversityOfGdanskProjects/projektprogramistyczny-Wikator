using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class EditCommentDto
{
     [Required]  public required string Text { get; init; }
}