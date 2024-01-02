using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs;

public class UpsertReviewDto
{
    public Guid MovieId { get; init; }
    
    [Range(1, 5)]
    public int Score { get; init; }
}