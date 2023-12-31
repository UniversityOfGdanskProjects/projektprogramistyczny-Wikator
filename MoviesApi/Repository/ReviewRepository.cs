using MoviesApi.DTOs;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ReviewRepository(IDriver driver) : Repository(driver), IReviewRepository
{
    public async Task<bool> AddReview(int userId, UpsertReviewDto reviewDto)
    {
        return await ExecuteAsync(async tx =>
        {
            const string movieQuery = """
                                      MATCH (m:Movie)
                                      WHERE Id(m) = $movieId
                                      RETURN m
                                      """;

            var movieResult = await tx.RunAsync(movieQuery, new { movieId = reviewDto.MovieId });

            try
            {
                await movieResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            const string relationQuery = """
                                            MATCH (u:User)-[r:REVIEWED]->(m:Movie)
                                            WHERE Id(u) = $userId AND Id(m) = $movieId
                                            RETURN r
                                         """;

            var relationResult = await tx.RunAsync(relationQuery, new { userId, movieId = reviewDto.MovieId });

            try
            {
                await relationResult.SingleAsync();

                // TODO: Review exists
                return false;
            }
            catch (InvalidOperationException)
            {
                // ignored
            }

            const string query = """
                                    MATCH (u:User), (m:Movie)
                                    WHERE Id(u) = $userId AND Id(m) = $movieId
                                    CREATE (u)-[r:REVIEWED {score: $score}]->(m)
                                    RETURN r
                                 """;

            var reviewResult =
                await tx.RunAsync(query, new { userId, movieId = reviewDto.MovieId, score = reviewDto.Score });

            try
            {
                await reviewResult.SingleAsync();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });
    }
}