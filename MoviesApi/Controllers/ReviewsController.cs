using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
[ApiController]
[Route("/api/[controller]")]
public class ReviewsController(IReviewRepository reviewRepository) : ControllerBase
{
    private IReviewRepository ReviewRepository { get; } = reviewRepository;
    
    [HttpPost]
    public async Task<IActionResult> CreateReview(UpsertReviewDto reviewDto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        var review = await ReviewRepository.AddReview(userId, reviewDto);
        
        if (!review)
            return NotFound("Movie does not exist.");

        return NoContent();
    }
}