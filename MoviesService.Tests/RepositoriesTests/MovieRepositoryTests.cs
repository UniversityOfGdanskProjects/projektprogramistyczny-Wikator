using FluentAssertions;
using MoviesService.Models.Enums;
using MoviesService.Models.Parameters;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class MovieRepositoryTests
{
    public MovieRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query = """
                             MATCH (m:Movie { id: $movieId })
                             DETACH DELETE m
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             CREATE (:Actor { id: actor1Id, firstName: 'actor1', lastName: 'actor1', dateOfBirth: $date, biography: null, pictureAbsoluteUri: null, picturePublicId: null }),
                               (:Actor { id: actor2Id, firstName: 'actor2', lastName: 'actor2', dateOfBirth: $date, biography: null, pictureAbsoluteUri: null, picturePublicId: null })
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            actor1Id = Actor1Id.ToString(),
            actor2Id = Actor2Id.ToString(),
            date = DateOnly.FromDateTime(DateTime.Now)
        };
        var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters)).Wait();
    }

    private TestDatabaseSetup Database { get; }
    private MovieRepository Repository { get; } = new();
    private Guid Actor1Id { get; } = Guid.NewGuid();
    private Guid Actor2Id { get; } = Guid.NewGuid();

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenNoReviews()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie",
            0,
            13,
            null,
            false,
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

        var parameters = new
            { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasReviewed()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie",
            5,
            13,
            null,
            false,
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasWatchlist()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie",
            0,
            13,
            null,
            true,
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasFavourites()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie",
            0,
            13,
            null,
            false,
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ReturnsMovies_WhenUserHasFavouritesWatchlistAndReview()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie",
            5,
            13,
            null,
            true,
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams()));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndFilterOutTitles()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie",
            0,
            13,
            null,
            false,
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { Title = "Mo" }));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
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

        var parameters = new
            { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { Genre = "Comedy" }));

        // Assert
        movies.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndFilterOutActor()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie1",
            0,
            13,
            null,
            false,
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

        var parameters = new
            { movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId, new MovieQueryParams { Actor = Actor1Id }));

        // Assert
        movies.Items.Should().HaveCount(2);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetMoviesExcludingIgnored_ShouldReturnMovies_AndSortAndPaginate()
    {
        // Arrange
        var expectedResult = new MovieDto(
            Database.MovieId,
            "movie1",
            0,
            13,
            null,
            false,
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

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesExcludingIgnored(tx, Database.UserId,
                new MovieQueryParams
                    { SortBy = SortBy.Title, SortOrder = SortOrder.Descending, PageNumber = 2, PageSize = 2 }));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().ContainEquivalentOf(expectedResult);
    }

    [Fact]
    public async Task GetPublicId_ShouldReturnId_IfMovieExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var publicId = await session.ExecuteReadAsync(async tx =>
            await Repository.GetPublicId(tx, Database.MovieId));

        // Assert
        publicId.Should().Be("publicId");
    }

    [Fact]
    public async Task GetPublicId_ShouldReturnNull_IfMovieDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var publicId = await session.ExecuteReadAsync(async tx =>
            await Repository.GetPublicId(tx, Database.MovieId));

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

        // Act
        var title = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMostPopularMovieTitle(tx));

        // Assert
        title.Should().Be("movie2");
    }


    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task AddMovie_ShouldCreateNodeAndRelationships(int actorCount, int genreCount)
    {
        var actorIds = new List<Guid> { Actor1Id, Actor2Id };
        var genreNames = new List<string> { "Action", "Comedy" };

        // Arrange
        var addMovieDto = new AddMovieDto
        {
            Title = "testMovie",
            Description = "description",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Now),
            MinimumAge = 13,
            ActorIds = actorIds.Take(actorCount).ToList(),
            Genres = genreNames.Take(genreCount).ToList(),
            InTheaters = false
        };

        await using var session = Database.Driver.AsyncSession();

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.AddMovie(tx, addMovieDto, null, null));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (m:Movie { title: 'testMovie' })
                             WITH m, COUNT(m) AS moviesCount
                             OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
                             WITH m, moviesCount, COUNT(a) AS actorsCount
                             OPTIONAL MATCH (m)-[:IS]->(g:Genre)
                             RETURN moviesCount, actorsCount, COUNT(g) AS genresCount
                             """;

        var cursor = await session.RunAsync(query);
        var record = await cursor.SingleAsync();

        record["moviesCount"].Should().Be(1);
        record["actorsCount"].Should().Be(actorCount);
        record["genresCount"].Should().Be(genreCount);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task AddMovie_ShouldReturnMovie(int actorCount, int genreCount)
    {
        var actorIds = new List<Guid> { Actor1Id, Actor2Id };
        var genreNames = new List<string> { "Action", "Comedy" };

        // Arrange
        var addMovieDto = new AddMovieDto
        {
            Title = "testMovie",
            Description = "description",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Now),
            MinimumAge = 13,
            ActorIds = actorIds.Take(actorCount).ToList(),
            Genres = genreNames.Take(genreCount).ToList(),
            InTheaters = false
        };

        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteWriteAsync(async tx =>
            await Repository.AddMovie(tx, addMovieDto, null, null));

        // Assert
        result.Title.Should().Be(addMovieDto.Title);
        result.Actors.Should().HaveCount(actorCount);
        result.Genres.Should().HaveCount(genreCount);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task EditMovie_ShouldDeleteNode_WhenNoRelationsExisted(int actorCount, int genreCount)
    {
        // Arrange
        var actorIds = new List<Guid> { Actor1Id, Actor2Id };
        var genreNames = new List<string> { "Action", "Comedy" };

        var editMovieDto = new EditMovieDto
        {
            Title = "testMovie",
            Description = "description",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Now),
            MinimumAge = 13,
            ActorIds = actorIds.Take(actorCount).ToList(),
            Genres = genreNames.Take(genreCount).ToList(),
            InTheaters = false
        };

        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'testMovie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.EditMovie(tx, Database.MovieId, Database.UserId, editMovieDto));

        // Assert

        // language=Cypher
        const string newQuery = """
                                MATCH (m:Movie { title: 'testMovie' })
                                WITH m, COUNT(m) AS moviesCount
                                OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
                                WITH m, moviesCount, COUNT(a) AS actorsCount
                                OPTIONAL MATCH (m)-[:IS]->(g:Genre)
                                RETURN moviesCount, actorsCount, COUNT(g) AS genresCount
                                """;

        var cursor = await session.RunAsync(newQuery);
        var record = await cursor.SingleAsync();

        record["moviesCount"].Should().Be(1);
        record["actorsCount"].Should().Be(actorCount);
        record["genresCount"].Should().Be(genreCount);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task EditMovie_ShouldDeleteNode_WhenRelationsExisted(int actorCount, int genreCount)
    {
        // Arrange
        var actorIds = new List<Guid> { Actor1Id, Actor2Id };
        var genreNames = new List<string> { "Action", "Comedy" };

        var editMovieDto = new EditMovieDto
        {
            Title = "testMovie",
            Description = "description",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Now),
            MinimumAge = 13,
            ActorIds = actorIds.Take(actorCount).ToList(),
            Genres = genreNames.Take(genreCount).ToList(),
            InTheaters = false
        };

        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'testMovie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             WITH m, $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             """;
        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            actor1Id = Actor1Id.ToString(),
            actor2Id = Actor2Id.ToString()
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.EditMovie(tx, Database.MovieId, Database.UserId, editMovieDto));

        // Assert

        // language=Cypher
        const string newQuery = """
                                MATCH (m:Movie { title: 'testMovie' })
                                WITH m, COUNT(m) AS moviesCount
                                OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
                                WITH m, moviesCount, COUNT(a) AS actorsCount
                                OPTIONAL MATCH (m)-[:IS]->(g:Genre)
                                RETURN moviesCount, actorsCount, COUNT(g) AS genresCount
                                """;

        var cursor = await session.RunAsync(newQuery);
        var record = await cursor.SingleAsync();

        record["moviesCount"].Should().Be(1);
        record["actorsCount"].Should().Be(actorCount);
        record["genresCount"].Should().Be(genreCount);
    }

    [Fact]
    public async Task MoviePictureExists_ReturnsTrue_WhenPictureExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var exists = await session.ExecuteReadAsync(async tx =>
            await Repository.MoviePictureExists(tx, Database.MovieId));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task MoviePictureExists_ReturnsFalse_WhenPictureDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var exists = await session.ExecuteReadAsync(async tx =>
            await Repository.MoviePictureExists(tx, Database.MovieId));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMoviePicture_UpdatesProperty()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.DeleteMoviePicture(tx, Database.MovieId));

        // Assert

        // language=Cypher
        const string newQuery = "MATCH (m:Movie { id: $movieId }) RETURN m.pictureAbsoluteUri, m.picturePublicId";

        var cursor = await session.RunAsync(newQuery, parameters);
        var record = await cursor.SingleAsync();

        record["m.pictureAbsoluteUri"].Should().BeNull();
        record["m.picturePublicId"].Should().BeNull();
    }

    [Fact]
    public async Task AddMoviePicture_ShouldUpdateProperty()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.AddMoviePicture(tx, Database.MovieId, "https://www.example.com", "publicId"));

        // Assert

        // language=Cypher
        const string newQuery = "MATCH (m:Movie { id: $movieId }) RETURN m.pictureAbsoluteUri, m.picturePublicId";

        var cursor = await session.RunAsync(newQuery, parameters);
        var record = await cursor.SingleAsync();

        record["m.pictureAbsoluteUri"].Should().Be("https://www.example.com");
        record["m.picturePublicId"].Should().Be("publicId");
    }

    [Fact]
    public async Task DeleteMovie_ShouldDeleteNodeAndRelationships()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id
                             MATCH (a1:Actor { id: actor1Id }), (a2:Actor { id: actor2Id }), (m:Movie { id: $movieId }), (g:Genre { name: 'Action' })
                             CREATE (a1)-[:PLAYED_IN]->(m), (a2)-[:PLAYED_IN]->(m), (m)-[:IS]->(g)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString()
        };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.DeleteMovie(tx, Database.MovieId));

        // Assert

        // language=Cypher
        const string newQuery = "MATCH (m:Movie { id: $movieId }) RETURN COUNT(m) > 0 AS movieExists";

        var cursor = await session.RunAsync(newQuery, parameters);
        var result = await cursor.SingleAsync(record => ValExtensions.ToBool(record["movieExists"]));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MovieExists_ShouldDeleteFalse_WhenMovieDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var exists = await session.ExecuteReadAsync(async tx =>
            await Repository.MovieExists(tx, Database.MovieId));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task MovieExists_ShouldDeleteTrue_WhenMovieExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', picturePublicId: 'publicId', minimumAge: 13 })";

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var exists = await session.ExecuteReadAsync(async tx =>
            await Repository.MovieExists(tx, Database.MovieId));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetMovieDetails_ShouldReturnMovie_WhenExists()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        var expectedResult = new MovieDetailsDto(
            Database.MovieId,
            "movie",
            "description",
            OnWatchlist: false,
            IsFavourite: true,
            AverageScore: 5,
            ReviewsCount: 1,
            UserReview: new ReviewIdAndScoreDto(reviewId, 5),
            ReleaseDate: new DateOnly(2022, 1, 1),
            PictureUri: "https://www.example.com",
            MinimumAge: 13,
            Actors: [],
            Genres: new List<string> { "Action" },
            InTheaters: false,
            Comments: [],
            TrailerUrl: null);

        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (m:Movie { id: $movieId, title: 'movie',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: 'https://www.example.com', inTheaters: false, picturePublicId: 'publicId', minimumAge: 13, popularity: 0 })
                             WITH $actor1Id AS actor1Id, $actor2Id AS actor2Id, $userId AS userId, $reviewId AS reviewId
                             MATCH (m:Movie { id: $movieId }), (g:Genre { name: 'Action' }), (u:User { id: userId })
                             CREATE (m)-[:IS]->(g), (u)-[:FAVOURITE]->(m), (u)-[:REVIEWED { id: reviewId, score: 5 }]->(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(), actor1Id = Actor1Id.ToString(), actor2Id = Actor2Id.ToString(),
            userId = Database.UserId.ToString(), reviewId = reviewId.ToString()
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var movie = await session.ExecuteWriteAsync(async tx =>
            await Repository.GetMovieDetails(tx, Database.MovieId, Database.UserId));

        // Assert
        movie.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetMovieDetails_ShouldReturnNull_WhenDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var movie = await session.ExecuteWriteAsync(async tx =>
            await Repository.GetMovieDetails(tx, Database.MovieId, Database.UserId));

        // Assert
        movie.Should().BeNull();
    }

    [Fact]
    public async Task GetMoviesWhenNotLoggedIn_ShouldSortAndPaginate()
    {
        // Arrange
        var expectedResult = new List<MovieDto>
        {
            new(
                Database.MovieId,
                "movie1",
                0,
                13,
                null,
                false,
                UserReview: null,
                IsFavourite: false,
                ReviewsCount: 0,
                Genres: [])
        };

        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             CREATE (:Movie { id: $movieId, title: 'movie1',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (:Movie { id: apoc.create.uuid(), title: 'movie2',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 }),
                               (:Movie { id: apoc.create.uuid(), title: 'movie3',  description: 'description', releaseDate: date('2022-01-01'), pictureAbsoluteUri: null, picturePublicId: null, minimumAge: 13 })
                             """;

        var parameters = new { movieId = Database.MovieId.ToString() };
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var movies = await session.ExecuteReadAsync(async tx =>
            await Repository.GetMoviesWhenNotLoggedIn(tx,
                new MovieQueryParams
                    { SortBy = SortBy.Title, SortOrder = SortOrder.Descending, PageNumber = 2, PageSize = 2 }));

        // Assert
        movies.Items.Should().HaveCount(1);
        movies.Items.Should().BeEquivalentTo(expectedResult);
    }
}