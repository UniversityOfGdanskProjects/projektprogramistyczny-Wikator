using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;

namespace MoviesApi.Repository.Contracts;

public interface IReviewRepository
{
    public Task<QueryResult<ReviewDto>> AddReview(Guid userId, AddReviewDto reviewDto);
    public Task<QueryResult<ReviewDto>> UpdateReview(Guid userId, Guid reviewId, UpdateReviewDto reviewDto);
    public Task<QueryResult> DeleteReview(Guid userId, Guid reviewId);
}