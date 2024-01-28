using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class CreateMessageDto
{
    [Required] public required string Content { get; set; }
}