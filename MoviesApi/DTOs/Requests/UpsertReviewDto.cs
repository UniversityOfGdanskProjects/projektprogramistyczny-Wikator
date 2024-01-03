using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class UpsertReviewDto
{
    public Guid MovieId { get; init; }
    
    [Range(1, 5)]
    public int Score { get; init; }
}