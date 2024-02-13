using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class AddReviewDto
{
    [Required] public Guid MovieId { get; init; }
    [Range(1, 5)] [Required] public int Score { get; init; }
}