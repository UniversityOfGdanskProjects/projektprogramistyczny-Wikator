using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class NotificationRepository : INotificationRepository
{
    public async Task<PagedList<NotificationDto>> GetAllNotificationsAsync(IAsyncQueryRunner tx, NotificationQueryParams queryParams, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId })<-[r:NOTIFICATION]-(m:Movie)
                             MATCH (m)<-[c:COMMENTED { Id: r.RelatedEntityId }]-(u2:User)

                             RETURN {
                               Id: r.Id,
                               IsRead: r.IsRead,
                               CreatedAt: r.CreatedAt,
                               Comment: {
                                 Id: c.Id,
                                 MovieId: m.Id,
                                 UserId: u2.Id,
                                 Username: u2.Name,
                                 Text: c.Text,
                                 CreatedAt: c.CreatedAt,
                                 IsEdited: c.IsEdited
                               }
                             } AS Notification
                             SKIP $Skip
                             LIMIT $Limit
                             """;
        
        var cursor = await tx.RunAsync(query,
            new
            {
                userId = userId.ToString(), Skip = (queryParams.PageNumber - 1) * queryParams.PageSize,
                Limit = queryParams.PageSize
            });
        
        var items = await cursor.ToListAsync(record =>
        {
            var notification = record["Notification"].As<IDictionary<string, object>>();
            return notification.ConvertToNotificationDto();
        });
        
        // language=Cypher
        const string totalCountQuery = """
                                       MATCH (:User { Id: $userId })<-[r:NOTIFICATION]-(:Movie)
                                       RETURN COUNT(r) AS TotalCount
                                       """;
        
        var totalCountCursor = await tx.RunAsync(totalCountQuery,
            new { userId = userId.ToString() });
        var totalCount = await totalCountCursor.SingleAsync(record => record["TotalCount"].As<int>());
        
        return new PagedList<NotificationDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
    }

    public async Task MarkNotificationAsReadAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })<-[r:NOTIFICATION { Id: $notificationId }]-(:Movie)
                             SET r.IsRead = true
                             """;

        await tx.RunAsync(query,
            new
            {
                userId = userId.ToString(),
                notificationId = notificationId.ToString()
            });
    }

    public Task MarkAllNotificationsAsReadAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })<-[r:NOTIFICATION]-(:Movie)
                             SET r.IsRead = true
                             """;
        
        return tx.RunAsync(query,
            new { userId = userId.ToString() });
    }

    public Task DeleteNotificationAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })<-[r:NOTIFICATION { Id: $notificationId }]-(:Movie)
                             DELETE r
                             """;
        
        return tx.RunAsync(query,
            new
            {
                userId = userId.ToString(),
                notificationId = notificationId.ToString()
            });
    }

    public Task DeleteAllNotificationsAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })<-[r:NOTIFICATION]-(:Movie)
                             DELETE r
                             """;
        
        return tx.RunAsync(query,
            new { userId = userId.ToString() });
    }

    public async Task<bool> NotificationExistsAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })<-[r:NOTIFICATION { Id: $notificationId }]-(:Movie)
                             RETURN COUNT(r) > 0 AS Exists
                             """;
        
        var cursor = await tx.RunAsync(query,
            new
            {
                userId = userId.ToString(),
                notificationId = notificationId.ToString()
            });
        return await cursor.SingleAsync(record => record["Exists"].As<bool>());
    }
}