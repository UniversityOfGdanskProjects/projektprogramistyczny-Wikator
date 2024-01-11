using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class CommentRepository : ICommentRepository
{
    public async Task<CommentDto?> GetCommentAsync(IAsyncQueryRunner tx,Guid commentId)
    {
        // language=Cypher
        const string query = """
                                MATCH (m:Movie)<-[r:COMMENTED]-(u:User)
                                WHERE r.Id = $commentId
                                RETURN {
                                    Id: r.Id,
                                    MovieId: m.Id,
                                    UserId: u.Id,
                                    Username: u.Name,
                                    Text: r.Text,
                                    CreatedAt: r.CreatedAt,
                                    IsEdited: r.IsEdited
                                } AS Comment
                             """;
        
        var result = await tx.RunAsync(query, new { commentId = commentId.ToString() });

        try
        {
            return await result.SingleAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<CommentDto> AddCommentAsync(IAsyncQueryRunner tx,Guid userId, AddCommentDto addCommentDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie { Id: $movieId }), (u:User { Id: $userId })
                             MATCH (m)<-[:FAVOURITE]-(u2:User)
                             CREATE (u)-[r:COMMENTED {
                               Id: randomUUID(),
                               Text: $text,
                               CreatedAt: $dateTime,
                               IsEdited: false
                             }]->(m)
                             CREATE (u2)<-[:NOTIFICATION {
                               Id: randomUUID(),
                               RelatedEntityId: r.Id,
                               CreatedAt: $dateTime,
                               IsRead: false
                             }]-(m)
                             RETURN {
                                 Id: r.Id,
                                 MovieId: m.Id,
                                 UserId: u.Id,
                                 Username: u.Name,
                                 Text: r.Text,
                                 CreatedAt: r.CreatedAt,
                                 IsEdited: r.IsEdited
                             } AS Comment
                             """;
        var cursor = await tx.RunAsync(query, new
        {
            movieId = addCommentDto.MovieId.ToString(),
            userId = userId.ToString(),
            text = addCommentDto.Text,
            dateTime = DateTime.Now
        });
        
        return await cursor.SingleAsync(record =>
        {
            var comment = record["Comment"].As<IDictionary<string, object>>();
            return comment.ConvertToCommentDto();
        });
    }

    public async Task<CommentDto> EditCommentAsync(IAsyncQueryRunner tx,Guid commentId, Guid userId, EditCommentDto addCommentDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId })-[r:COMMENTED { Id: $commentId }]->(m:Movie)
                             SET r.Text = $text, r.IsEdited = true
                             RETURN {
                                 Id: r.Id,
                                 MovieId: m.Id,
                                 UserId: u.Id,
                                 Username: u.Name,
                                 Text: r.Text,
                                 CreatedAt: r.CreatedAt,
                                 IsEdited: r.IsEdited
                             } AS Comment
                             """;

        var cursor = await tx.RunAsync(query, new
            { userId = userId.ToString(), commentId = commentId.ToString(), text = addCommentDto.Text });

        return await cursor.SingleAsync(record =>
        {
            var comment = record["Comment"].As<IDictionary<string, object>>();
            return comment.ConvertToCommentDto();
        });
    }

    public async Task DeleteCommentAsync(IAsyncQueryRunner tx,Guid commentId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:COMMENTED { Id: $commentId }]->(:Movie)
                             DELETE r
                             """;

        await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
    }

    public async Task<bool> CommentExists(IAsyncQueryRunner tx, Guid commentId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:COMMENTED { Id: $commentId }]->(:Movie)
                             WITH COUNT(r) > 0 AS commentsExists
                             RETURN commentsExists
                             """;

        var cursor = await tx.RunAsync(query,
            new { commentId = commentId.ToString(), userId = userId.ToString() });
        return await cursor.SingleAsync(record => record["commentsExists"].As<bool>());
    }
}