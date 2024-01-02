using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class ReviewsController(IReviewRepository reviewRepository) : BaseApiController
{
    private IReviewRepository ReviewRepository { get; } = reviewRepository;
    
    [HttpPost]
    public async Task<IActionResult> CreateReview(UpsertReviewDto reviewDto)
    {
        var review = await ReviewRepository.AddReview(User.GetUserId(), reviewDto);
    
        return review switch
        {
            QueryResult.Completed => NoContent(),
            QueryResult.NotFound => NotFound("Movie not found"),
            QueryResult.EntityAlreadyExists => BadRequest("Review for this movie already exists"),
            QueryResult.UnexpectedError => BadRequest("Failed to add review"),
            _ => throw new Exception(nameof(review))
        };
    }
}