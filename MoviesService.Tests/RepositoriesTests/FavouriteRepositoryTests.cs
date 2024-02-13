using FluentAssertions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class FavouriteRepositoryTests
{
    public FavouriteRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })-[r:FAVOURITE]->(m:Movie { id: $movieId })
                             DELETE r
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        using var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters)).Wait();
    }

    private TestDatabaseSetup Database { get; }
    private FavouriteRepository Repository { get; } = new();

    [Fact]
    public async Task GetAllFavouriteMovies_ShouldReturnEmptyList_WhenUserHasNoFavouriteMovies()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllFavouriteMovies(tx, Database.UserId));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllFavouriteMovies_ShouldReturnFavouriteMovies_WhenUserHasFavouriteMovies()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:FAVOURITE]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var result = (await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllFavouriteMovies(tx, Database.UserId))).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainEquivalentOf(new MovieDto(
            Database.MovieId,
            "The Matrix",
            PictureUri: null,
            MinimumAge: 13,
            OnWatchlist: false,
            IsFavourite: true,
            UserReview: null,
            ReviewsCount: 0,
            Genres: [],
            AverageScore: 0
        ));
    }

    [Fact]
    public async Task SetMovieAsFavourite_ShouldCreateRelationship()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.SetMovieAsFavourite(tx, Database.UserId, Database.MovieId);

            // language=Cypher
            const string query = """
                                 MATCH (:User { id: $userId })-[r:FAVOURITE]->(:Movie { id: $movieId })
                                 RETURN COUNT(r) AS count
                                 """;

            var parameters = new
            {
                userId = Database.UserId.ToString(),
                movieId = Database.MovieId.ToString()
            };

            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        });

        result.Should().Be(1);
    }

    [Fact]
    public async Task UnsetMovieAsFavourite_ShouldDeleteRelationship()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:FAVOURITE]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.UnsetMovieAsFavourite(tx, Database.UserId, Database.MovieId);

            // language=Cypher
            const string relationshipCountQuery = """
                                                  MATCH (:User { id: $userId })-[r:FAVOURITE]->(:Movie { id: $movieId })
                                                  RETURN COUNT(r) AS count
                                                  """;

            var relationshipCountParameters = new
            {
                userId = Database.UserId.ToString(),
                movieId = Database.MovieId.ToString()
            };

            var cursor = await tx.RunAsync(relationshipCountQuery, relationshipCountParameters);
            return await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        });

        result.Should().Be(0);
    }

    [Fact]
    public async Task MovieIsFavourite_ShouldReturnFalse_WhenMovieIsNotFavourite()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.MovieIsFavourite(tx, Database.MovieId, Database.UserId));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MovieIsFavourite_ShouldReturnTrue_WhenMovieIsFavourite()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:FAVOURITE]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.MovieIsFavourite(tx, Database.MovieId, Database.UserId));

        // Assert
        result.Should().BeTrue();
    }
}