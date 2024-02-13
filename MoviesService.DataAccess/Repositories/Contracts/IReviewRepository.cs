using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IReviewRepository
{
    public Task<ReviewDto> AddReview(IAsyncQueryRunner tx, Guid userId, AddReviewDto reviewDto);
    public Task<ReviewDto> UpdateReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId, UpdateReviewDto reviewDto);
    public Task DeleteReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId);
    Task<bool> ReviewExists(IAsyncQueryRunner tx, Guid id, Guid userId);
    Task<Guid?> GetMovieIdFromReviewId(IAsyncQueryRunner tx, Guid reviewId, Guid userId);
    Task<bool> ReviewExistsByMovieId(IAsyncQueryRunner tx, Guid movieId, Guid userId);
    Task<ReviewAverageAndCount> GetAverageAndCountFromReviewId(IAsyncQueryRunner tx, Guid reviewId);
    Task<ReviewAverageAndCount> GetAverageAndCountFromMovieId(IAsyncQueryRunner tx, Guid movieId);
}