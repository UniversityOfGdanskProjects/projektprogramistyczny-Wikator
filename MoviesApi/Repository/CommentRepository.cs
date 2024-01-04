using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class CommentRepository(IMovieRepository movieRepository, IDriver driver) : Repository(driver), ICommentRepository
{
    private IMovieRepository MovieRepository { get; } = movieRepository;
    
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

    public async Task<QueryResult<CommentDto>> AddCommentAsync(Guid userId, AddCommentDto addCommentDto)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, addCommentDto.MovieId))
                return new QueryResult<CommentDto>(QueryResultStatus.RelatedEntityDoesNotExists, null);
            
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
                movieId = addCommentDto.MovieId.ToString(),
                userId = userId.ToString(),
                text = addCommentDto.Text,
                dateTime = DateTime.Now
            });
            
            var comment = await cursor.SingleAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });

            return new QueryResult<CommentDto>(QueryResultStatus.Completed, comment);
        });
    }

    public async Task<QueryResult<CommentDto>> EditCommentAsync(Guid commentId, Guid userId, EditCommentDto addCommentDto)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await CommentExists(tx, commentId, userId))
                return new QueryResult<CommentDto>(QueryResultStatus.NotFound, null);

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

            var comment = await cursor.SingleAsync(record =>
            {
                var comment = record["Comment"].As<IDictionary<string, object>>();
                return comment.ConvertToCommentDto();
            });

            return new QueryResult<CommentDto>(QueryResultStatus.Completed, comment);
        });
    }

    public async Task<QueryResult> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        // language=Cypher
        return await ExecuteAsync(async tx =>
        {
            if (!await CommentExists(tx, commentId, userId))
                return new QueryResult(QueryResultStatus.NotFound);

            // language=Cypher
            const string query = """
                                 MATCH (:User { Id: $userId })-[r:COMMENTED { Id: $commentId }]->(:Movie)
                                 DELETE r
                                 """;

            await tx.RunAsync(query, new { commentId = commentId.ToString(), userId = userId.ToString() });
            return new QueryResult(QueryResultStatus.Completed);
        });
    }

    private static async Task<bool> CommentExists(IAsyncQueryRunner tx, Guid commentId, Guid userId)
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