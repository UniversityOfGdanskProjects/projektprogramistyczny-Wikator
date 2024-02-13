using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

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

    public async Task<CommentWithNotification> AddCommentAsync(IAsyncQueryRunner tx, Guid userId,
        AddCommentDto addCommentDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                             CREATE (u)-[r:COMMENTED {
                               id: apoc.create.uuid(),
                               text: $text,
                               createdAt: $dateTime,
                               isEdited: false
                             }]->(m)
                             WITH m, u, r
                             OPTIONAL MATCH (m)<-[:FAVOURITE]-(u2:User) WHERE u2.id <> $userId
                             CALL apoc.do.when(
                               u2 IS NOT NULL,
                               'CREATE (u2)<-[:NOTIFICATION {
                                 id: apoc.create.uuid(),
                                 relatedEntityId: r.id,
                                 isRead: false
                               }]-(m)
                                RETURN m',
                               'RETURN m',
                               { u2: u2, m: m, r: r }
                             ) YIELD value
                             WITH m, u, r, COLLECT(value) AS ignore
                             RETURN
                               r.id AS id,
                               m.id AS movieId,
                               u.id AS userId,
                               u.name AS username,
                               r.text AS text,
                               r.createdAt AS createdAt,
                               r.isEdited AS isEdited,
                               {
                                commentUsername: u.name,
                                commentText: r.text,
                                movieTitle: m.title,
                                movieId: m.id
                               } AS notification
                             """;

        var parameters = new
        {
            movieId = addCommentDto.MovieId.ToString(),
            userId = userId.ToString(),
            text = addCommentDto.Text,
            dateTime = DateTime.Now
        };

        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record.ConvertToCommentWithNotification());
    }

    public async Task<CommentDto> EditCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId,
        EditCommentDto addCommentDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User)-[r:COMMENTED { id: $commentId }]->(m:Movie)
                             WHERE u.id = $userId OR EXISTS { MATCH (:User { id: $userId, role: 'Admin' }) }
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

    public async Task DeleteCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User)-[r:COMMENTED { id: $commentId }]->(m:Movie)
                             WHERE u.id = $userId OR EXISTS { MATCH (:User { id: $userId, role: 'Admin' }) }
                             OPTIONAL MATCH (m)-[r2:NOTIFICATION { relatedEntityId: $commentId }]->(:User)
                             DELETE r2, r
                             """;

        await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
    }

    public async Task<bool> CommentExistsAsOwnerOrAdmin(IAsyncQueryRunner tx, Guid commentId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User)-[r:COMMENTED { id: $commentId }]->(:Movie)
                             WHERE u.id = $userId OR EXISTS { MATCH (:User { id: $userId, role: 'Admin' }) }
                             RETURN COUNT(r) > 0 AS commentsExists
                             """;

        var cursor = await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
        return await cursor.SingleAsync(record => record["commentsExists"].As<bool>());
    }

    public async Task<Guid?> GetMovieIdFromCommentAsOwnerOrAdminAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId)
    {
        try
        {
            // language=Cypher
            const string query = """
                                 MATCH (u:User)-[r:COMMENTED { id: $commentId }]->(m:Movie)
                                 WHERE u.id = $userId OR EXISTS { MATCH (:User { id: $userId, role: 'Admin' }) }
                                 RETURN m.id AS movieId
                                 """;

            var cursor = await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
            return await cursor.SingleAsync(record => Guid.Parse(record["movieId"].As<string>()));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}