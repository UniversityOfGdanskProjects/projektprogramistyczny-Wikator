using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class CommentController(ICommentRepository commentRepository) : BaseApiController
{
    private ICommentRepository CommentRepository { get; } = commentRepository;
    
    [HttpPost]
    public async Task<IActionResult> AddCommentAsync(AddCommentDto addCommentDto)
    {
        var comment = await CommentRepository.AddCommentAsync(User.GetUserId(), addCommentDto);

        return comment switch
        {
            null => NotFound("Movie not found"),
            _ => Ok(comment)
        };
    }
    
    [HttpPut("{commentId:guid}/")]
    public async Task<IActionResult> EditCommentAsync(Guid commentId, EditCommentDto editCommentDto)
    {
        var comment = await CommentRepository.EditCommentAsync(commentId, User.GetUserId(), editCommentDto);

        return comment switch
        {
            null => BadRequest("Either the comment doesn't exist or you don't have permission to edit it"),
            _ => Ok(comment)
        };
    }
    
    [HttpDelete("{commentId:guid}/")]
    public async Task<IActionResult> DeleteCommentAsync(Guid commentId)
    {
        var result = await CommentRepository.DeleteCommentAsync(commentId, User.GetUserId());

        return result switch
        {
            QueryResult.NotFound => NotFound("Either the comment doesn't exist or you don't have permission to delete it"),
            _ => NoContent()
        };
    }
}