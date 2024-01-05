using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class ReviewsController(IReviewRepository reviewRepository, IUserClaimsProvider claimsProvider) : BaseApiController
{
    private IReviewRepository ReviewRepository { get; } = reviewRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = claimsProvider;

    [HttpPost]
    public async Task<IActionResult> CreateReview(AddReviewDto reviewDto)
    {
        var review = await ReviewRepository
            .AddReview(UserClaimsProvider.GetUserId(User), reviewDto);
    
        return review.Status switch
        {
            QueryResultStatus.RelatedEntityDoesNotExists => BadRequest("Movie you are trying to review does not exist"),
            QueryResultStatus.EntityAlreadyExists => BadRequest("You already reviewed this movie"),
            QueryResultStatus.Completed => Ok(review.Data),
            _ => throw new Exception("Unexpected status returned")
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, UpdateReviewDto reviewDto)
    {
        var review = await ReviewRepository
            .UpdateReview(UserClaimsProvider.GetUserId(User), id, reviewDto);

        return review.Status switch
        {
            QueryResultStatus.NotFound => NotFound("Review does not exist"),
            QueryResultStatus.Completed => Ok(review.Data),
            _ => throw new Exception("Unexpected status returned")
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var result = await ReviewRepository.DeleteReview(UserClaimsProvider.GetUserId(User), id);

        return result.Status switch
        {
            QueryResultStatus.NotFound => NotFound("Review does not exist"),
            QueryResultStatus.Completed => NoContent(),
            _ => throw new Exception("Unexpected status returned")
        };
    }
}
