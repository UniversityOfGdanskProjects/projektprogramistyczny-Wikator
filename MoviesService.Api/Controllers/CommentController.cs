using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Extensions;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CommentController(IDriver driver, IMovieRepository movieRepository,
    ICommentRepository commentRepository, IMqttService mqttService) : BaseApiController(driver)
{
    private ICommentRepository CommentRepository { get; } = commentRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IMqttService MqttService { get; } = mqttService;
    

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
            var commentWithNotification = await CommentRepository.AddCommentAsync(tx, userId, addCommentDto);
            _ = SendNewMqttCommentNotificationAsync(commentWithNotification);
            return CreatedAtAction(nameof(GetComment), new { id = commentWithNotification.Comment.Id }, commentWithNotification.Comment);
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
            _ = SendUpdatedMqttCommentNotificationAsync(comment);
            return Ok(comment);
        });
    }
    
    [HttpDelete("{commentId:guid}/")]
    public async Task<IActionResult> DeleteCommentAsync(Guid commentId)
    {
        return await ExecuteWriteAsync(async tx =>
        {
            var userId = User.GetUserId();
            var movieId = await CommentRepository.GetMovieIdFromCommentAsOwnerOrAdminAsync(tx, commentId, userId);
            
            if (movieId is null)
                return NotFound("Either the comment doesn't exist or you don't have permission to delete it");

            await CommentRepository.DeleteCommentAsync(tx, commentId, userId);
            _ = SendDeletedMqttCommentNotificationAsync(commentId, movieId.Value);
            return NoContent();
        });
    }
    
    private async Task SendNewMqttCommentNotificationAsync(CommentWithNotification commentWithNotification)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var commentPayload = JsonSerializer.Serialize(commentWithNotification.Comment, options);
        await MqttService.SendNotificationAsync($"movie/{commentWithNotification.Comment.MovieId}/new-comment", commentPayload);
        var notificationPayload = JsonSerializer.Serialize(commentWithNotification.Notification, options);
        await MqttService.SendNotificationAsync($"notification/movie/{commentWithNotification.Notification.MovieId}", notificationPayload);
    }
    
    private async Task SendUpdatedMqttCommentNotificationAsync(CommentDto commentDto)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var payload = JsonSerializer.Serialize(commentDto, options);
        await MqttService.SendNotificationAsync($"movie/{commentDto.MovieId}/updated-comment", payload);
    }
    
    private async Task SendDeletedMqttCommentNotificationAsync(Guid commentId, Guid movieId)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var payload = JsonSerializer.Serialize(new { commentId }, options);
        await MqttService.SendNotificationAsync($"movie/{movieId}/deleted-comment", payload);
    }
}
