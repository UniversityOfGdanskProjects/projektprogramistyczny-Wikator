using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using Neo4j.Driver;

namespace MoviesService.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ReviewController(
    IAsyncQueryExecutor queryExecutor,
    IMovieRepository movieRepository,
    IReviewRepository reviewRepository,
    IMqttService mqttService,
    IUserClaimsProvider claimsProvider) : BaseApiController(queryExecutor)
{
    private IReviewRepository ReviewRepository { get; } = reviewRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IMqttService MqttService { get; } = mqttService;
    private IUserClaimsProvider ClaimsProvider { get; } = claimsProvider;


    [HttpPost]
    public async Task<IActionResult> CreateReview(AddReviewDto reviewDto)
    {
        Guid? id = null;
        var result = await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, reviewDto.MovieId))
                return BadRequest("Movie you are trying to review does not exist");

            var userId = ClaimsProvider.GetUserId(User);

            if (await ReviewRepository.ReviewExistsByMovieId(tx, reviewDto.MovieId, userId))
                return BadRequest("You already reviewed this movie");

            var review = await ReviewRepository.AddReview(tx, userId, reviewDto);
            id = review.Id;
            return Ok(review);
        });

        if (id is not null)
            _ = SendMqttNewReview(id.Value, ReviewRepository.GetAverageAndCountFromReviewId);

        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, UpdateReviewDto reviewDto)
    {
        var result = await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = ClaimsProvider.GetUserId(User);

            if (!await ReviewRepository.ReviewExists(tx, id, userId))
                return NotFound("Review does not exist, or you don't have permission to edit it");

            var review = await ReviewRepository.UpdateReview(tx, userId, id, reviewDto);
            return Ok(review);
        });

        if (result is OkObjectResult)
            _ = SendMqttNewReview(id, ReviewRepository.GetAverageAndCountFromReviewId);

        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        Guid? movieId = null;
        var result = await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = ClaimsProvider.GetUserId(User);
            movieId = await ReviewRepository.GetMovieIdFromReviewId(tx, id, userId);

            if (movieId is null)
                return NotFound("Review does not exist, or you don't have permission to delete it");

            await ReviewRepository.DeleteReview(tx, userId, id);
            return NoContent();
        });

        if (result is NoContentResult && movieId is not null)
            _ = SendMqttNewReview(movieId.Value, ReviewRepository.GetAverageAndCountFromMovieId);

        return result;
    }


    private async Task SendMqttNewReview(Guid id,
        Func<IAsyncQueryRunner, Guid, Task<ReviewAverageAndCount>> getAverageAndCount)
    {
        var reviewAverageAndCount = await QueryExecutor.ExecuteReadAsync(async tx => await getAverageAndCount(tx, id));

        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var payload = JsonSerializer.Serialize(reviewAverageAndCount, options);
        await MqttService.SendNotificationAsync($"movie/{reviewAverageAndCount.MovieId}/updated-reviews", payload);
    }
}