using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Models;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

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

        var cursor = await tx.RunAsync(query,parameters);
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

    public async Task<ReviewAverageAndCount> GetAverageAndCount(IAsyncQueryRunner tx, Guid reviewId)
    {
        // language=Cypher
        const string query = """
                             MATCH(m:Movie)<-[:REVIEWED { id: $reviewId }]-(:User)
                             OPTIONAL MATCH(m)<-[r:REVIEWED]-(:User)
                             WITH m, COUNT(r) AS count, COALESCE(r.score, 0) AS score
                             RETURN
                               m.id AS movieId,
                               AVG(score) AS average,
                               count
                             """;
        
        var cursor = await tx.RunAsync(query, new { reviewId = reviewId.ToString() });
        return await cursor.SingleAsync(record =>
            new ReviewAverageAndCount(
                Guid.Parse(record["movieId"].As<string>()),
                record["average"].As<double>(),
                record["count"].As<int>()));
    }
}
