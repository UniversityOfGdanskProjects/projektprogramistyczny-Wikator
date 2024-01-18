using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class CommentRepository : ICommentRepository
{
    public async Task<CommentDto?> GetCommentAsync(IAsyncQueryRunner tx, Guid commentId)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)<-[r:COMMENTED]-(u:User)
                             WHERE r.id = $commentId
                             RETURN
                               r.id AS id,
                               m.id AS movieId,
                               u.id AS userId,
                               u.name AS username,
                               r.text AS text,
                               r.createdAt AS createdAt,
                               r.isEdited AS isEdited
                             """;
        
        var result = await tx.RunAsync(query, new { commentId = commentId.ToString() });

        try
        {
            return await result.SingleAsync(record => record.ConvertToCommentDto());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<CommentDto> AddCommentAsync(IAsyncQueryRunner tx, Guid userId, AddCommentDto addCommentDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                             OPTIONAL MATCH (m)<-[:FAVOURITE]-(u2:User) WHERE u2.id <> $userId
                             CREATE (u)-[r:COMMENTED {
                               id: apoc.create.uuid(),
                               text: $text,
                               createdAt: $dateTime,
                               isEdited: false
                             }]->(m)
                             WITH m, u, u2, r
                             CALL apoc.do.when(u2 IS NOT NULL,
                               'CREATE (u2)<-[:NOTIFICATION {
                                 id: apoc.create.uuid(),
                                 relatedEntityId: r.id,
                                 isRead: false
                               }]-(m)
                               RETURN m, r', 
                               'RETURN m, r', {u2:u2, m:m, r:r}) YIELD value
                             RETURN
                               value.r.id AS id,
                               value.m.id AS movieId,
                               u.id AS userId,
                               u.name AS username,
                               value.r.text AS text,
                               value.r.createdAt AS createdAt,
                               value.r.isEdited AS isEdited
                             """;
        
        var parameters = new
        {
            movieId = addCommentDto.MovieId.ToString(),
            userId = userId.ToString(),
            text = addCommentDto.Text,
            dateTime = DateTime.Now
        };
        
        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record.ConvertToCommentDto());
    }

    public async Task<CommentDto> EditCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId,
        EditCommentDto addCommentDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })-[r:COMMENTED { id: $commentId }]->(m:Movie)
                             SET r.text = $text, r.isEdited = true
                             RETURN
                               r.id AS id,
                               m.id AS movieId,
                               u.id AS userId,
                               u.name AS username,
                               r.text AS text,
                               r.createdAt AS createdAt,
                               r.isEdited AS isEdited
                             """;
        
        var parameters = new
        {
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            text = addCommentDto.Text
        };

        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record.ConvertToCommentDto());
    }

    public async Task DeleteCommentAsync(IAsyncQueryRunner tx,Guid commentId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:COMMENTED { id: $commentId }]->(:Movie)
                             DELETE r
                             """;

        await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
    }

    public async Task<bool> CommentExists(IAsyncQueryRunner tx, Guid commentId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:COMMENTED { id: $commentId }]->(:Movie)
                             RETURN COUNT(r) > 0 AS commentsExists
                             """;

        var cursor = await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
        return await cursor.SingleAsync(record => record["commentsExists"].As<bool>());
    }
}