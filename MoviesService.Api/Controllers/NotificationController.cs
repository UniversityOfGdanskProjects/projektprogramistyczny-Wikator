using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Extensions;
using MoviesService.Core.Helpers;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;
using Neo4j.Driver;

namespace MoviesService.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class NotificationController(IAsyncQueryExecutor queryExecutor,
    INotificationRepository notificationRepository) : BaseApiController(queryExecutor)
{
    private INotificationRepository NotificationRepository { get; } = notificationRepository;
    
    [HttpGet]
    public async Task<IActionResult> GetAllNotificationsAsync([FromQuery] NotificationQueryParams queryParams)
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var pagedList = await NotificationRepository.GetAllNotificationsAsync(tx, queryParams, User.GetUserId());
            
            PaginationHeader paginationHeader = new(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount,
                pagedList.TotalPages);

            Response.AddPaginationHeader(paginationHeader);
            return Ok(pagedList);
        });
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> MarkNotificationAsRead(Guid id)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            
            if (!await NotificationRepository.NotificationExistsAsync(tx, id, userId))
                return NotFound("Notification not found");
            
            await NotificationRepository.MarkNotificationAsReadAsync(tx, id, userId);
            return NoContent();
        });
    }
    
    [HttpPut]
    public async Task<IActionResult> MarkAllNotificationsAsRead()
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            await NotificationRepository.MarkAllNotificationsAsReadAsync(tx, userId);
            return NoContent();
        });
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            
            if (!await NotificationRepository.NotificationExistsAsync(tx, id, userId))
                return NotFound("Notification not found");
            
            await NotificationRepository.DeleteNotificationAsync(tx, id, userId);
            return NoContent();
        });
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteAllNotifications()
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            await NotificationRepository.DeleteAllNotificationsAsync(tx, userId);
            return NoContent();
        });
    }
}