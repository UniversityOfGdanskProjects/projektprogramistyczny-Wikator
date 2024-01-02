using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ReviewRepository(IDriver driver) : Repository(driver), IReviewRepository
{
    public async Task<QueryResult> AddReview(Guid userId, UpsertReviewDto reviewDto)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string movieQuery = """
                                      MATCH (m:Movie { Id: $movieId })
                                      RETURN m
                                      """;

            var movieResult = await tx.RunAsync(movieQuery, new { movieId = reviewDto.MovieId.ToString() });

            try
            {
                await movieResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }
            
            // language=Cypher
            const string relationQuery = """
                                         MATCH (:User { Id: $userId})-[r:REVIEWED]->(:Movie { Id: $movieId })
                                         RETURN r
                                         """;

            var relationResult = await tx.RunAsync(relationQuery,
                new { userId = userId.ToString(), movieId = reviewDto.MovieId.ToString() });

            try
            {
                await relationResult.SingleAsync();
                return QueryResult.EntityAlreadyExists;
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
            
            // language=Cypher
            const string query = """
                                 MATCH (u:User { Id: $userId}), (m:Movie { Id: $movieId })
                                 CREATE (u)-[r:REVIEWED {score: $score}]->(m)
                                 RETURN r
                                 """;

            var reviewResult =
                await tx.RunAsync(query,
                    new { userId = userId.ToString(), movieId = reviewDto.MovieId.ToString(), score = reviewDto.Score });

            try
            {
                await reviewResult.SingleAsync();
                return QueryResult.Completed;
            }
            catch (InvalidOperationException)
            {
                return QueryResult.UnexpectedError;
            }
        });
    }
}
