using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IReviewRepository
{
    public Task<ReviewDto?> AddReview(Guid userId, AddReviewDto reviewDto);
    public Task<ReviewDto?> UpdateReview(Guid userId, Guid reviewId, UpdateReviewDto reviewDto);
    public Task<QueryResult> DeleteReview(Guid userId, Guid reviewId);
}