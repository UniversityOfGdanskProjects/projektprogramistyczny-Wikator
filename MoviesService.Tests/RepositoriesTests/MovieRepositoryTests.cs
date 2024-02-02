using FluentAssertions;
using MoviesService.Core.Enums;
using MoviesService.Core.Helpers;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class MovieRepositoryTests
{
    private TestDatabaseSetup Database { get; }
    private Guid Actor1Id { get; } = Guid.NewGuid();
    private Guid Actor2Id { get; } = Guid.NewGuid();
    
    
    public MovieRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();
        
        // language=Cypher
        const string query = """
                             MATCH (m:Movie { id: $movieId })
                             DETACH DELETE m
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             CREATE (:Actor { id: actor1Id, firstName: 'actor1', lastName: 'actor1', dateOfBirth: datetime(), biography: null, pictureAbsoluteUri: null, picturePublicId: null }),
                               (:Actor { id: actor2Id, firstName: 'actor2', lastName: 'actor2', dateOfBirth: datetime(), biography: null, pictureAbsoluteUri: null, picturePublicId: null })
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters)).Wait();
    }
    
    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenNoReviews()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie",
            AverageScore: 0,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: false,
            UserReview: null,
            IsFavourite: false,
            ReviewsCount: 0,
            Genres: ["Action"]);
        
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));
        
        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasReviewed()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie",
            AverageScore: 5,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: false,
            UserReview: new ReviewIdAndScoreDto(
                Score: 5,
                Id: reviewId),
            IsFavourite: false,
            ReviewsCount: 1,
            Genres: ["Action"]);

        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             WITH $userId AS userId, $movieId AS movieId, m
                             MATCH (u:User { id: userId })
                             CREATE (u)-[:REVIEWED { id: $reviewId, score: 5 }]->(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString(),
            userId = Database.UserId.ToString(), reviewId = reviewId.ToString()
        };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        var repository = new MovieRepository();

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }
    
    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasIgnored()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             WITH $userId AS userId, $movieId AS movieId
                             MATCH (u:User { id: userId }), (m:Movie { id: movieId })
                             CREATE (u)-[:IGNORES]->(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString(),
            userId = Database.UserId.ToString()
        };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        var repository = new MovieRepository();

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasWatchlist()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie",
            AverageScore: 0,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: true,
            UserReview: null,
            IsFavourite: false,
            ReviewsCount: 0,
            Genres: ["Action"]);
        
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             WITH $userId AS userId, $movieId AS movieId
                             MATCH (u:User { id: userId }), (m:Movie { id: movieId })
                             CREATE (u)-[:WATCHLIST]->(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString(),
            userId = Database.UserId.ToString()
        };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        var repository = new MovieRepository();

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }
    
    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasFavourites()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie",
            AverageScore: 0,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: false,
            UserReview: null,
            IsFavourite: true,
            ReviewsCount: 0,
            Genres: ["Action"]);
        
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             WITH $userId AS userId, $movieId AS movieId
                             MATCH (u:User { id: userId }), (m:Movie { id: movieId })
                             CREATE (u)-[:FAVOURITE]->(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString(),
            userId = Database.UserId.ToString()
        };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        var repository = new MovieRepository();

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasFavouritesWatchlistAndReview()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie",
            AverageScore: 5,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: true,
            UserReview: new ReviewIdAndScoreDto(
                Score: 5,
                Id: reviewId),
            IsFavourite: true,
            ReviewsCount: 1,
            Genres: ["Action"]);
        
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id, m
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             WITH $userId AS userId, $movieId AS movieId, m
                             MATCH (u:User { id: userId })
                             CREATE (u)-[:REVIEWED { id: $reviewId, score: 5 }]->(m), (u)-[:WATCHLIST]->(m), (u)-[:FAVOURITE]->(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString(),
            userId = Database.UserId.ToString(), reviewId = reviewId.ToString()
        };
        
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));
        
        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndFilterOutTitles()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie",
            AverageScore: 0,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: false,
            UserReview: null,
            IsFavourite: false,
            ReviewsCount: 0,
            Genres: []);
        
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (:Movie { id: apoc.create.uuid(), title: 'aaaa',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { Title = "Mo" }));
        
        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }
    
    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndFilterOutGenres()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { Genre = "Comedy"  }));
        
        // Assert
        movies.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndFilterOutActor()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie1",
            AverageScore: 0,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: false,
            UserReview: null,
            IsFavourite: false,
            ReviewsCount: 0,
            Genres: []);
        
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (m1:Movie { id: $movieId, title: 'movie1',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (m2:Movie { id: apoc.create.uuid(), title: 'movie2',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (m3:Movie { id: apoc.create.uuid(), title: 'movie3',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id, m1, m2, m3
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id })
                             CREATE (a1)-[:PLAYED_IN]->(m1), (a2)-[:PLAYED_IN]->(m1), (a1)-[:PLAYED_IN]->(m2)
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { Actor = Actor1Id }));
        
        // Assert
        movies.Should().HaveCount(2);
        movies.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndSortAndPaginate()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Id: Database.MovieId,
            Title: "movie1",
            AverageScore: 0,
            MinimumAge: 13,
            PictureUri: null,
            OnWatchlist: false,
            UserReview: null,
            IsFavourite: false,
            ReviewsCount: 0,
            Genres: []);
        
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (m1:Movie { id: $movieId, title: 'movie1',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (m2:Movie { id: apoc.create.uuid(), title: 'movie2',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (m3:Movie { id: apoc.create.uuid(), title: 'movie3',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { SortBy = SortBy.Title, SortOrder = SortOrder.Descending, PageNumber = 2, PageSize = 2 }));
        
        // Assert
        movies.Should().HaveCount(1);
        movies.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetPublicId_ShouldReturnId_IfMovieExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13 })
                             """;
        
        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
        
        var repository = new MovieRepository();
        
        // Act
        var publicId = await session.ExecuteReadAsync(async tx =>
            await repository.GetPublicId(tx, Database.MovieId));
        
        // Assert
        publicId.Should().Be("publicId");
    }
    
    [Fact]
    public async Task GetPublicId_ShouldReturnNull_IfMovieDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        var repository = new MovieRepository();
        
        // Act
        var publicId = await session.ExecuteReadAsync(async tx =>
            await repository.GetPublicId(tx, Database.MovieId));
        
        // Assert
        publicId.Should().BeNull();
    }

    [Fact]
    public async Task GetMostPopularMovieTitle_ShouldReturnCorrectTitle()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             CREATE (:Movie { id: apoc.create.uuid(), title: 'movie1',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13, popularity: 5 }),
                               (:Movie { id: apoc.create.uuid(), title: 'movie2',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13, popularity: 10 })
                             """;
        
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query));
        
        var repository = new MovieRepository();
        
        // Act
        var title = await session.ExecuteReadAsync(async tx =>
            await repository.GetMostPopularMovieTitle(tx));
        
        // Assert
        title.Should().Be("movie2");
        
    }
}