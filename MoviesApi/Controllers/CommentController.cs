﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CommentController(IDriver driver, IMovieRepository movieRepository,
    ICommentRepository commentRepository) : BaseApiController(driver)
{
    private ICommentRepository CommentRepository { get; } = commentRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetComment(Guid id)
    {
        return await ExecuteReadAsync(async tx =>
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
        return await ExecuteWriteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, addCommentDto.MovieId))
                return BadRequest("Movie you are trying to comment on does not exist");

            var userId = User.GetUserId();
            var comment = await CommentRepository.AddCommentAsync(tx, userId, addCommentDto);
            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        });
    }
    
    [HttpPut("{commentId:guid}/")]
    public async Task<IActionResult> EditCommentAsync(Guid commentId, EditCommentDto editCommentDto)
    {
        return await ExecuteWriteAsync(async tx =>
        {
            var userId = User.GetUserId();
            
            if (!await CommentRepository.CommentExistsAsOwnerOrAdmin(tx, commentId, userId))
                return NotFound("Either the comment doesn't exist or you don't have permission to edit it");

            var comment = await CommentRepository.EditCommentAsync(tx, commentId, userId, editCommentDto);
            return Ok(comment);
        });
    }
    
    [HttpDelete("{commentId:guid}/")]
    public async Task<IActionResult> DeleteCommentAsync(Guid commentId)
    {
        return await ExecuteWriteAsync(async tx =>
        {
            var userId = User.GetUserId();

            if (!await CommentRepository.CommentExistsAsOwnerOrAdmin(tx, commentId, userId))
                return NotFound("Either the comment doesn't exist or you don't have permission to delete it");

            await CommentRepository.DeleteCommentAsync(tx, commentId, userId);
            return NoContent();
        });
    }
}
