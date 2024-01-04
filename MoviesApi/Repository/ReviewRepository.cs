using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ReviewRepository(IMovieRepository movieRepository, IDriver driver) : Repository(driver), IReviewRepository
{
    private IMovieRepository MovieRepository { get; } = movieRepository;
    
    public async Task<ReviewDto?> AddReview(Guid userId, AddReviewDto reviewDto)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, reviewDto.MovieId))
                return null;

            if (await ReviewExistsByMovieId(tx, reviewDto.MovieId, userId))
                return null;
            
            // language=Cypher
            const string query = """
                                 MATCH (u:User { Id: $userId}), (m:Movie { Id: $movieId })
                                 CREATE (u)-[r:REVIEWED { Id: randomUUID(), Score: $score}]->(m)
                                 RETURN {
                                   Id: r.Id,
                                   UserId: u.Id,
                                   MovieId: m.Id,
                                   Score: r.Score
                                 } AS Review
                                 """;

            var cursor =
                await tx.RunAsync(query,
                    new { userId = userId.ToString(), movieId = reviewDto.MovieId.ToString(), score = reviewDto.Score });

            return await cursor.SingleAsync(record =>
                record["Review"].As<IDictionary<string, object>>().ConvertToReviewDto());
        });
    }

    public async Task<ReviewDto?> UpdateReview(Guid userId, Guid reviewId, UpdateReviewDto reviewDto)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await ReviewExists(tx, reviewId, userId))
                return null;

            // language=Cypher
            const string query = """
                                 MATCH(u:User { Id: $userId })-[r:REVIEWED { Id: $id }]->(m:Movie)
                                 SET r.Score = $score
                                 RETURN {
                                   Id: r.Id,
                                   UserId: u.Id,
                                   MovieId: m.Id,
                                   Score: r.Score
                                 } AS Review
                                 """;

            var cursor = await tx.RunAsync(query,
                new
                {
                    id = reviewId.ToString(), userId = userId.ToString(),
                    score = reviewDto.Score
                });

            return await cursor.SingleAsync(record =>
                record["Review"].As<IDictionary<string, object>>().ConvertToReviewDto());
        });
    }

    public async Task<QueryResult> DeleteReview(Guid userId, Guid reviewId)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await ReviewExists(tx, reviewId, userId))
                return QueryResult.NotFound;

            // language=Cypher
            const string query = """
                                 MATCH (:User { Id: $userId })-[r:REVIEWED { Id: $reviewId }]->(:Movie)
                                 DELETE r
                                 """;

            await tx.RunAsync(query, new { userId = userId.ToString(), reviewId = reviewId.ToString() });
            return QueryResult.Completed;
        });
    }

    private static async Task<bool> ReviewExists(IAsyncQueryRunner tx, Guid id, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH(:User { Id: $userId })-[r:REVIEWED { Id: $id }]->(:Movie)
                             WITH COUNT(r) > 0 AS reviewExists
                             RETURN reviewExists
                             """;
        var cursor = await tx.RunAsync(query, new { id = id.ToString(), userId = userId.ToString() });
        return await cursor.SingleAsync(record => record["reviewExists"].As<bool>());
    }

    private static async Task<bool> ReviewExistsByMovieId(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH(:User { Id: $userId })-[r:REVIEWED]->(:Movie { Id: $movieId })
                             WITH COUNT(r) > 0 AS reviewExists
                             RETURN reviewExists
                             """;
        var cursor = await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["reviewExists"].As<bool>());
    }
}
