using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class CommentRepository(IDriver driver) : Repository(driver), ICommentRepository
{
    public async Task<IEnumerable<CommentDto>> GetMovieCommentsAsync(Guid movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (m:Movie { Id: $movieId })<-[r:COMMENTED]-(u:User)
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

            var result = await tx.RunAsync(query, new { movieId = movieId.ToString() });

            return await result.ToListAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });
        });
    }

    public async Task<IEnumerable<CommentDto>> GetUserCommentsAsync(Guid userId)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (m:Movie)<-[r:COMMENTED]-(u:User { Id: $userId })
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

            var result = await tx.RunAsync(query, new { userId = userId.ToString() });

            return await result.ToListAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });
        });
    }

    public async Task<CommentDto?> GetCommentAsync(Guid commentId)
    {
        return await ExecuteAsync(async tx =>
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
        });
    }

    public async Task<CommentDto?> AddCommentAsync(Guid userId, UpsertCommentDto upsertCommentDto)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string checkMovieExistsQuery = """
                                                 MATCH (m:Movie {Id: $movieId})
                                                 RETURN m
                                                 """;
            
            var movieExistsResult = await tx.RunAsync(checkMovieExistsQuery,
                new { movieId = upsertCommentDto.MovieId.ToString() });
            
            try
            {
                await movieExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            
            
            // language=Cypher
            const string query = """
                                 MATCH (m:Movie { Id: $movieId }), (u:User { Id: $userId })
                                    CREATE (u)-[r:COMMENTED {
                                        Id: randomUUID(),
                                        Text: $text,
                                        CreatedAt: $dateTime,
                                        IsEdited: false
                                    }]->(m)
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
                movieId = upsertCommentDto.MovieId.ToString(),
                userId = userId.ToString(),
                text = upsertCommentDto.Text,
                dateTime = DateTime.Now
            });
            
            return await cursor.SingleAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });
        });
    }

    public async Task<CommentDto?> EditCommentAsync(Guid commentId, Guid userId, UpsertCommentDto upsertCommentDto)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string checkIfCommentExistsQuery = """
                                                     MATCH (:User { Id: $userId })-[r:COMMENTED]->(:Movie)
                                                     WHERE r.Id = $commentId
                                                     RETURN r
                                                     """;

            var commentExistsResult = await tx.RunAsync(checkIfCommentExistsQuery,
                new { commentId = commentId.ToString(), userId = userId.ToString() });

            try
            {
                await commentExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            // language=Cypher
            const string query = """
                                 MATCH (u:User { Id: $userId })-[r:COMMENTED]->(m:Movie)
                                 WHERE r.Id = $commentId
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
                { userId = userId.ToString(), commentId = commentId.ToString(), text = upsertCommentDto.Text });

            return await cursor.SingleAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });
        });
    }

    public async Task<QueryResult> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        // language=Cypher
        return await ExecuteAsync(async tx =>
        {
            const string commentExistsQuery = """
                                              MATCH (:User { Id: $userId })-[r:COMMENTED]->(:Movie)
                                              WHERE r.Id = $commentId
                                              RETURN r
                                              """;

            var commentExistsResult = await tx.RunAsync(commentExistsQuery,
                new { commentId = commentId.ToString(), userId = userId.ToString() });

            try
            {
                await commentExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }

            // language=Cypher
            const string query = """
                                 MATCH (:User { Id: $userId })-[r:COMMENTED]->(:Movie)
                                 WHERE r.Id = $commentId
                                 DELETE r
                                 """;

            await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
            return QueryResult.Completed;
        });
    }
}