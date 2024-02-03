using FluentAssertions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class WatchlistRepositoryTests
{
    public WatchlistRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })-[r:WATCHLIST]->(m:Movie { id: $movieId })
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

    [Fact]
    public async Task GetAllMoviesOnWatchlist_ShouldReturnMovieDtos()
    {
        // Arrange
        var repository = new WatchlistRepository();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[:WATCHLIST]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var movieDtos = await session.ExecuteReadAsync(async tx =>
        {
            var result = await repository.GetAllMoviesOnWatchlist(tx, Database.UserId);
            return result.ToList();
        });

        // Assert
        movieDtos.Should().HaveCount(1);
        movieDtos.Should().ContainEquivalentOf(new MovieDto(
            Database.MovieId,
            "The Matrix",
            PictureUri: null,
            MinimumAge: 13,
            OnWatchlist: true,
            IsFavourite: false,
            UserReview: null,
            ReviewsCount: 0,
            Genres: [],
            AverageScore: 0
        ));
    }

    [Fact]
    public async Task GetAllMoviesOnWatchlist_ShouldReturnEmptyList_WhenUserHasNoFavouriteMovies()
    {
        // Arrange
        var repository = new WatchlistRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var movieDtos = await session.ExecuteReadAsync(async tx =>
        {
            var result = await repository.GetAllMoviesOnWatchlist(tx, Database.UserId);
            return result.ToList();
        });

        // Assert
        movieDtos.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToWatchList_ShouldAddRelationship()
    {
        // Arrange
        var repository = new WatchlistRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        await session.ExecuteWriteAsync(async tx =>
        {
            await repository.AddToWatchList(tx, Database.UserId, Database.MovieId);
        });

        // Assert
        var relationshipExists = await session.ExecuteReadAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (u:User { id: $userId })-[r:WATCHLIST]->(m:Movie { id: $movieId })
                                 RETURN COUNT(r) > 0 AS relationshipCount
                                 """;

            var parameters = new
            {
                userId = Database.UserId.ToString(),
                movieId = Database.MovieId.ToString()
            };

            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync(record => ValExtensions.ToInt(record["relationshipCount"]));
        });

        relationshipExists.Should().Be(1);
    }

    [Fact]
    public async Task RemoveFromWatchlist_ShouldDeleteRelationship()
    {
        // Arrange
        var repository = new WatchlistRepository();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[:WATCHLIST]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
        {
            await repository.RemoveFromWatchList(tx, Database.UserId, Database.MovieId);
        });

        // Assert
        var relationshipExists = await session.ExecuteReadAsync(async tx =>
        {
            // language=Cypher
            const string relationshipExistsQuery = """
                                                   MATCH (u:User { id: $userId })-[r:WATCHLIST]->(m:Movie { id: $movieId })
                                                   RETURN COUNT(r) > 0 AS relationshipCount
                                                   """;

            var relationshipExistsParameters = new
            {
                userId = Database.UserId.ToString(),
                movieId = Database.MovieId.ToString()
            };

            var cursor = await tx.RunAsync(relationshipExistsQuery, relationshipExistsParameters);
            return await cursor.SingleAsync(record => ValExtensions.ToBool(record["relationshipCount"]));
        });

        relationshipExists.Should().BeFalse();
    }

    [Fact]
    public async Task WatchlistExists_ShouldReturnTrueIfExists()
    {
        // Arrange
        var repository = new WatchlistRepository();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[:WATCHLIST]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var watchlistExists = await session.ExecuteReadAsync(async tx =>
            await repository.WatchlistExists(tx, Database.MovieId, Database.UserId));

        // Assert
        watchlistExists.Should().BeTrue();
    }

    [Fact]
    public async Task WatchlistExists_ShouldReturnFalseIfNotExists()
    {
        // Arrange
        var repository = new WatchlistRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var watchlistExists = await session.ExecuteReadAsync(async tx =>
            await repository.WatchlistExists(tx, Database.MovieId, Database.UserId));

        // Assert
        watchlistExists.Should().BeFalse();
    }
}