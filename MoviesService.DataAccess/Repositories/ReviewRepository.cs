using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

public class ReviewRepository : IReviewRepository
{
    public async Task<ReviewDto> AddReview(IAsyncQueryRunner tx, Guid userId, AddReviewDto reviewDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId}), (m:Movie { id: $movieId })
                             CREATE (u)-[r:REVIEWED { id: apoc.create.uuid(), score: $score}]->(m)
                             RETURN
                               r.id AS id,
                               u.id AS userId,
                               m.id AS movieId,
                               r.score AS score
                             """;

        var parameters = new
        {
            userId = userId.ToString(),
            movieId = reviewDto.MovieId.ToString(),
            score = reviewDto.Score
        };

        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record.ConvertToReviewDto());
    }

    public async Task<ReviewDto> UpdateReview(IAsyncQueryRunner tx,
        Guid userId, Guid reviewId, UpdateReviewDto reviewDto)
    {
        // language=Cypher
        const string query = """
                             MATCH(u:User { id: $userId })-[r:REVIEWED { id: $id }]->(m:Movie)
                             SET r.score = $score
                             RETURN
                               r.id AS id,
                               u.id AS userId,
                               m.id AS movieId,
                               r.score AS score
                             """;

        var parameters = new
        {
            id = reviewId.ToString(),
            userId = userId.ToString(),
            score = reviewDto.Score
        };

        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record.ConvertToReviewDto());
    }

    public async Task DeleteReview(IAsyncQueryRunner tx, Guid userId, Guid reviewId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:REVIEWED { id: $reviewId }]->(:Movie)
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), reviewId = reviewId.ToString() });
    }

    public async Task<bool> ReviewExists(IAsyncQueryRunner tx, Guid id, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH(:User { id: $userId })-[r:REVIEWED { id: $id }]->(:Movie)
                             RETURN COUNT(r) > 0 AS reviewExists
                             """;

        var cursor = await tx.RunAsync(query, new { id = id.ToString(), userId = userId.ToString() });
        return await cursor.SingleAsync(record => record["reviewExists"].As<bool>());
    }

    public async Task<Guid?> GetMovieIdFromReviewId(IAsyncQueryRunner tx, Guid reviewId, Guid userId)
    {
        try
        {
            // language=Cypher
            const string query = """
                                 MATCH(:User { id: $userId })-[:REVIEWED { id: $reviewId }]->(m:Movie)
                                 RETURN m.id AS movieId
                                 """;

            var cursor = await tx.RunAsync(query, new { reviewId = reviewId.ToString(), userId = userId.ToString() });
            return await cursor.SingleAsync(record => Guid.Parse(record["movieId"].As<string>()));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<bool> ReviewExistsByMovieId(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH(:User { id: $userId })-[r:REVIEWED]->(:Movie { id: $movieId })
                             RETURN COUNT(r) > 0 AS reviewExists
                             """;

        var cursor = await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["reviewExists"].As<bool>());
    }

    public async Task<ReviewAverageAndCount> GetAverageAndCountFromReviewId(IAsyncQueryRunner tx, Guid reviewId)
    {
        // language=Cypher
        const string query = """
                             MATCH(m:Movie)<-[:REVIEWED { id: $reviewId }]-(:User)
                             MATCH(m)<-[r:REVIEWED]-(:User)
                             RETURN
                               m.id AS movieId,
                               AVG(r.score) AS average,
                               COUNT(r) AS count
                             """;

        var cursor = await tx.RunAsync(query, new { reviewId = reviewId.ToString() });
        return await cursor.SingleAsync(record =>
            new ReviewAverageAndCount(
                Guid.Parse(record["movieId"].As<string>()),
                record["average"].As<double>(),
                record["count"].As<int>()));
    }

    public async Task<ReviewAverageAndCount> GetAverageAndCountFromMovieId(IAsyncQueryRunner tx, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH(m:Movie {id: $movieId})
                             WITH m
                             OPTIONAL MATCH(m)<-[r:REVIEWED]-(:User)
                             WITH m, COUNT(r) AS rCount, AVG(r.score) AS rAvg
                             RETURN
                               m.id AS movieId,
                               COALESCE(rAvg, 0) AS average,
                               rCount AS count
                             """;

        var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString() });
        return await cursor.SingleAsync(record =>
            new ReviewAverageAndCount(
                Guid.Parse(record["movieId"].As<string>()),
                record["average"].As<double>(),
                record["count"].As<int>()));
    }
}