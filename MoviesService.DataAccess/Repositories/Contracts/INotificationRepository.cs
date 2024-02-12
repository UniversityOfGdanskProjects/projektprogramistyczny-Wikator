using MoviesService.DataAccess.Helpers;
using MoviesService.Models.DTOs.Responses;
using MoviesService.Models.Parameters;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface INotificationRepository
{
    Task<PagedList<NotificationDto>> GetAllNotificationsAsync(IAsyncQueryRunner tx,
        NotificationQueryParams queryParams, Guid userId);

    Task MarkNotificationAsReadAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId);
    Task MarkAllNotificationsAsReadAsync(IAsyncQueryRunner tx, Guid userId);
    Task DeleteNotificationAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId);
    Task DeleteAllNotificationsAsync(IAsyncQueryRunner tx, Guid userId);
    Task<bool> NotificationExistsAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId);
}