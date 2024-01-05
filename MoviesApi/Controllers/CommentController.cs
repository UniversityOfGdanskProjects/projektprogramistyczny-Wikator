using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class CommentController(ICommentRepository commentRepository, IUserClaimsProvider userClaimsProvider)
    : BaseApiController
{
    private ICommentRepository CommentRepository { get; } = commentRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = userClaimsProvider;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetComment(Guid id)
    {
        var comment = await CommentRepository.GetCommentAsync(id);

        return comment switch
        {
            null => NotFound("Comment does not exist"),
            _ => Ok(comment)
        };
    }
    
    
    [HttpPost]
    public async Task<IActionResult> AddCommentAsync(AddCommentDto addCommentDto)
    {
        var comment = await CommentRepository
            .AddCommentAsync(UserClaimsProvider.GetUserId(User), addCommentDto);

        return comment.Status switch
        {
            QueryResultStatus.RelatedEntityDoesNotExists => BadRequest("Movie does not exist"),
            QueryResultStatus.Completed => CreatedAtAction(nameof(GetComment), new { id = comment.Data!.Id }, comment.Data),
            _ => throw new Exception("This shouldn't have happened")
        };
    }
    
    [HttpPut("{commentId:guid}/")]
    public async Task<IActionResult> EditCommentAsync(Guid commentId, EditCommentDto editCommentDto)
    {
        var comment = await CommentRepository
            .EditCommentAsync(commentId, UserClaimsProvider.GetUserId(User), editCommentDto);

        return comment.Status switch
        {
            QueryResultStatus.NotFound => NotFound("Either the comment doesn't exist or you don't have permission to edit it"),
            QueryResultStatus.Completed => Ok(comment.Data),
            _ => throw new Exception("This shouldn't have happened")
        };
    }
    
    [HttpDelete("{commentId:guid}/")]
    public async Task<IActionResult> DeleteCommentAsync(Guid commentId)
    {
        var result = await CommentRepository
            .DeleteCommentAsync(commentId, UserClaimsProvider.GetUserId(User));

        return result.Status switch
        {
            QueryResultStatus.NotFound => NotFound("Either the comment doesn't exist or you don't have permission to edit it"),
            QueryResultStatus.Completed => NoContent(),
            _ => throw new Exception("This shouldn't have happened")
        };
    }
}
