using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

public class MessageRepository : IMessageRepository
{
    public async Task<MessageDto> CreateMessageAsync(IAsyncQueryRunner tx, Guid userId, string messageContent)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User {id: $userId})
                             CREATE (m:Message {content: $messageContent, createdAt: $messageDate})<-[:SENT]-(u)
                             RETURN m.content AS content, u.name AS userName, m.createdAt AS date
                             """;

        var cursor = await tx.RunAsync(query,
            new { userId = userId.ToString(), messageContent, messageDate = DateTime.Now });

        return await cursor.SingleAsync(r =>
            new MessageDto(r["content"].As<string>(), r["userName"].As<string>(), r["date"].As<DateTime>()));
    }

    public async Task<IEnumerable<MessageDto>> GetMostRecentMessagesAsync(IAsyncQueryRunner tx)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Message)<-[:SENT]-(u:User)
                             RETURN m.content AS content, u.name AS userName, m.createdAt AS date
                             ORDER BY m.createdAt DESC
                             LIMIT 10
                             """;

        var cursor = await tx.RunAsync(query);
        return await cursor.ToListAsync(r =>
            new MessageDto(r["content"].As<string>(), r["userName"].As<string>(), r["date"].As<DateTime>()));
    }
}