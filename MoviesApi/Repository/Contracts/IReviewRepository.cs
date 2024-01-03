using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IReviewRepository
{
    public Task<QueryResult> AddReview(Guid userId, UpsertReviewDto reviewDto);
}