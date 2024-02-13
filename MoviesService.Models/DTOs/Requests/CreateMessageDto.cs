using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class CreateMessageDto
{
    [Required] public required string Content { get; set; }
}