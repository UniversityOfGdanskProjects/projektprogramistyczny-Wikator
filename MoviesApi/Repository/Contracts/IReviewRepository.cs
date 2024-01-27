using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Models;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IReviewRepository
{
    public Task<ReviewDto> AddReview(IAsyncQueryRunner tx, Guid userId, AddReviewDto reviewDto);
    public Task<ReviewDto> UpdateReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId, UpdateReviewDto reviewDto);
    public Task DeleteReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId);
    Task<bool> ReviewExists(IAsyncQueryRunner tx, Guid id, Guid userId);
    Task<bool> ReviewExistsByMovieId(IAsyncQueryRunner tx, Guid movieId, Guid userId);
    Task<ReviewAverageAndCount> GetAverageAndCount(IAsyncQueryRunner tx, Guid reviewId);
}