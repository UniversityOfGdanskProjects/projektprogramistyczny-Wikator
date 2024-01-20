using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class UpdateReviewDto
{
    [Range(1, 5), Required] public int Score { get; init; }
}