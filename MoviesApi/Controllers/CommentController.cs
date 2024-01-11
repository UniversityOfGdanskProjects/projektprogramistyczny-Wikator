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
public class CommentController(IDriver driver, IMovieRepository movieRepository, ICommentRepository commentRepository,
    IUserClaimsProvider userClaimsProvider) : BaseApiController(driver)
{
    private ICommentRepository CommentRepository { get; } = commentRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = userClaimsProvider;
    

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetComment(Guid id)
    {
        return await ExecuteReadAsync<IActionResult>(async tx =>
        {
            var comment = await CommentRepository.GetCommentAsync(tx, id);

            return comment switch
            {
                null => NotFound("Comment does not exist"),
                _ => Ok(comment)
            };
        });
    }
    
    
    [HttpPost]
    public async Task<IActionResult> AddCommentAsync(AddCommentDto addCommentDto)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, addCommentDto.MovieId))
                return BadRequest("Movie you are trying to comment on does not exist");

            var userId = UserClaimsProvider.GetUserId(User);

            if (await CommentRepository.CommentExists(tx, addCommentDto.MovieId, userId))
                return BadRequest("You already commented on this movie");

            var comment = await CommentRepository.AddCommentAsync(tx, userId, addCommentDto);
            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        });
    }
    
    [HttpPut("{commentId:guid}/")]
    public async Task<IActionResult> EditCommentAsync(Guid commentId, EditCommentDto editCommentDto)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = UserClaimsProvider.GetUserId(User);
            
            if (!await CommentRepository.CommentExists(tx, commentId, userId))
                return NotFound("Either the comment doesn't exist or you don't have permission to edit it");

            var comment =
                await CommentRepository.EditCommentAsync(tx, commentId, userId,
                    editCommentDto);
            return Ok(comment);
        });
    }
    
    [HttpDelete("{commentId:guid}/")]
    public async Task<IActionResult> DeleteCommentAsync(Guid commentId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = UserClaimsProvider.GetUserId(User);

            if (!await CommentRepository.CommentExists(tx, commentId, UserClaimsProvider.GetUserId(User)))
                return NotFound("Either the comment doesn't exist or you don't have permission to delete it");

            await CommentRepository.DeleteCommentAsync(tx, commentId, userId);
            return NoContent();
        });
    }
}
