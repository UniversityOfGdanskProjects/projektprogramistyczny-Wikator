using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

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