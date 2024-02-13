using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Helpers;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Responses;
using MoviesService.Models.Parameters;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

public class NotificationRepository : INotificationRepository
{
    public async Task<PagedList<NotificationDto>> GetAllNotificationsAsync(IAsyncQueryRunner tx,
        NotificationQueryParams queryParams, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })<-[r:NOTIFICATION]-(m:Movie)
                             MATCH (m)<-[c:COMMENTED { id: r.relatedEntityId }]-(u:User)

                             RETURN
                               r.id AS id,
                               r.isRead AS isRead,
                               c.createdAt AS createdAt,
                               u.name AS commentUsername,
                               c.text AS commentText,
                               m.id AS movieId,
                               m.title AS movieTitle
                             ORDER BY isRead ASC, createdAt DESC
                             SKIP $skip
                             LIMIT $limit
                             """;

        var parameters = new
        {
            userId = userId.ToString(),
            skip = (queryParams.PageNumber - 1) * queryParams.PageSize,
            limit = queryParams.PageSize
        };

        var cursor = await tx.RunAsync(query, parameters);
        var items = await cursor.ToListAsync(record => record.ConvertToNotificationDto());

        // language=Cypher
        const string totalCountQuery = """
                                       MATCH (:User { id: $userId })<-[r:NOTIFICATION]-(:Movie)
                                       RETURN COUNT(r) AS totalCount
                                       """;

        var totalCountCursor = await tx.RunAsync(totalCountQuery, new { userId = userId.ToString() });
        var totalCount = await totalCountCursor.SingleAsync(record => record["totalCount"].As<int>());
        return new PagedList<NotificationDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
    }

    public async Task MarkNotificationAsReadAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })<-[r:NOTIFICATION { id: $notificationId }]-(:Movie)
                             SET r.isRead = true
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), notificationId = notificationId.ToString() });
    }

    public Task MarkAllNotificationsAsReadAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })<-[r:NOTIFICATION]-(:Movie)
                             SET r.isRead = true
                             """;

        return tx.RunAsync(query, new { userId = userId.ToString() });
    }

    public async Task DeleteNotificationAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })<-[r:NOTIFICATION { id: $notificationId }]-(:Movie)
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), notificationId = notificationId.ToString() });
    }

    public async Task DeleteAllNotificationsAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })<-[r:NOTIFICATION]-(:Movie)
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString() });
    }

    public async Task<bool> NotificationExistsAsync(IAsyncQueryRunner tx, Guid notificationId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })<-[r:NOTIFICATION { id: $notificationId }]-(:Movie)
                             RETURN COUNT(r) > 0 AS exists
                             """;

        var parameters = new
        {
            userId = userId.ToString(),
            notificationId = notificationId.ToString()
        };

        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record["exists"].As<bool>());
    }
}