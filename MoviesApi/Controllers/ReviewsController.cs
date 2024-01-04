using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class ReviewsController(IReviewRepository reviewRepository) : BaseApiController
{
    private IReviewRepository ReviewRepository { get; } = reviewRepository;
    
    [HttpPost]
    public async Task<IActionResult> CreateReview(AddReviewDto reviewDto)
    {
        var review = await ReviewRepository.AddReview(User.GetUserId(), reviewDto);
    
        return review switch
        {
            null => BadRequest("Either the movie you are trying to review doesn't exists, or you already reviewed this movie"),
            _ => Ok(review)
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, UpdateReviewDto reviewDto)
    {
        var review = await ReviewRepository.UpdateReview(User.GetUserId(), id, reviewDto);

        return review switch
        {
            null => NotFound(
                "Either the review you are trying to edit doesn't exist, or you don't have permission to edit it"),
            _ => Ok(review)
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var result = await ReviewRepository.DeleteReview(User.GetUserId(), id);

        return result switch
        {
            QueryResult.NotFound => NotFound(
                "Either the review you are trying to delete doesn't exist, or you don't have permission to delete it"),
            QueryResult.Completed => NoContent(),
            _ => throw new Exception("This shouldn't have happened")
        };
    }
}
