using MoviesApi.DTOs;

namespace MoviesApi.Repository.Contracts;

public interface IReviewRepository
{
    public Task<bool> AddReview(int userId, UpsertReviewDto reviewDto);
}