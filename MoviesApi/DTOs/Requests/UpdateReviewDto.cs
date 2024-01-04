using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class UpdateReviewDto
{
    [Range(1, 5)]
    public int Score { get; init; }
}