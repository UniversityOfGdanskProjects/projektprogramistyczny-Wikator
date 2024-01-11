using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ReviewController(IDriver driver, IMovieRepository movieRepository, IReviewRepository reviewRepository,
    IUserClaimsProvider claimsProvider) : BaseApiController(driver)
{
    private IReviewRepository ReviewRepository { get; } = reviewRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = claimsProvider;
    

    [HttpPost]
    public async Task<IActionResult> CreateReview(AddReviewDto reviewDto)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, reviewDto.MovieId))
                return BadRequest("Movie you are trying to review does not exist");

            var userId = UserClaimsProvider.GetUserId(User);

            if (await ReviewRepository.ReviewExistsByMovieId(tx, reviewDto.MovieId, userId))
                return BadRequest("You already reviewed this movie");

            var review = await ReviewRepository.AddReview(tx, userId, reviewDto);
            return Ok(review);
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, UpdateReviewDto reviewDto)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = UserClaimsProvider.GetUserId(User);

            if (!await ReviewRepository.ReviewExists(tx, id, userId))
                return NotFound("Review does not exist, or you don't have permission to edit it");

            var review = await ReviewRepository.UpdateReview(tx, userId, id, reviewDto);
            return Ok(review);
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = UserClaimsProvider.GetUserId(User);

            if (!await ReviewRepository.ReviewExists(tx, id, userId))
                return NotFound("Review does not exist, or you don't have permission to delete it");

            await ReviewRepository.DeleteReview(tx, userId, id);
            return NoContent();
        });
    }
}
