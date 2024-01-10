using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ReviewRepository : IReviewRepository
{
    public async Task<ReviewDto> AddReview(IAsyncQueryRunner tx, Guid userId, AddReviewDto reviewDto)
    {
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
    }

    public async Task<ReviewDto> UpdateReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId, UpdateReviewDto reviewDto)
    {
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
    }

    public async Task DeleteReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:REVIEWED { Id: $reviewId }]->(:Movie)
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), reviewId = reviewId.ToString() });
    }

    public async Task<bool> ReviewExists(IAsyncQueryRunner tx, Guid id, Guid userId)
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

    public async Task<bool> ReviewExistsByMovieId(IAsyncQueryRunner tx, Guid movieId, Guid userId)
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
