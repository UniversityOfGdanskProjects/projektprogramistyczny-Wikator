using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class UpdateReviewDto
{
    [Range(1, 5)] [Required] public int Score { get; init; }
}