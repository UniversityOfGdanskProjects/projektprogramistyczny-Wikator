using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class WatchlistControllerTests : ControllerTestsBase
{
    [Fact]
    public async Task GetAllMoviesOnWatchlist_ReturnsOkObjectResult()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository
            .Setup(x => x.GetAllMoviesOnWatchlist(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<MovieDto>());

        var movieRepository = new Mock<IMovieRepository>();

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepository.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.GetAllMoviesOnWatchlist();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MovieDto>>(okResult.Value);
        model.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToWatchList_ReturnsNotFoundObjectResult_WhenMovieDoesNotExist()
    {
        // Arrange
        var watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository
            .Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepositoryMock.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.AddToWatchList(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        notFoundResult.Value.Should().Be("Movie does not exist found");
    }

    [Fact]
    public async Task AddToWatchList_ReturnsBadRequestObjectResult_WhenMovieAlreadyInWatchlist()
    {
        // Arrange
        var watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository
            .Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        watchlistRepositoryMock
            .Setup(x => x.WatchlistExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepositoryMock.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.AddToWatchList(Guid.NewGuid());

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        badRequestResult.Value.Should().Be("Movie already in watchlist");
    }

    [Fact]
    public async Task AddToWatchList_ReturnsNoContentResult_WhenMovieIsAddedToWatchlist()
    {
        // Arrange
        var watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository
            .Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        watchlistRepositoryMock
            .Setup(x => x.WatchlistExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepositoryMock.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.AddToWatchList(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveFromWatchList_ReturnsNotFoundObjectResult_WhenMovieDoesNotExist()
    {
        // Arrange
        var watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository
            .Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepositoryMock.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.RemoveFromWatchList(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        notFoundResult.Value.Should().Be("Movie does not exist");
    }

    [Fact]
    public async Task RemoveFromWatchList_ReturnsBadRequestObjectResult_WhenMovieIsNotOnWatchlist()
    {
        // Arrange
        var watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository
            .Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        watchlistRepositoryMock
            .Setup(x => x.WatchlistExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepositoryMock.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.RemoveFromWatchList(Guid.NewGuid());

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        badRequestResult.Value.Should().Be("This movie is not on your watchlist");
    }

    [Fact]
    public async Task RemoveFromWatchList_ReturnsNoContentResult_WhenMovieIsRemovedFromWatchlist()
    {
        // Arrange
        var watchlistRepositoryMock = new Mock<IWatchlistRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository
            .Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        watchlistRepositoryMock
            .Setup(x => x.WatchlistExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new WatchlistController(
            QueryExecutorMock.Object,
            watchlistRepositoryMock.Object,
            movieRepository.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.RemoveFromWatchList(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}