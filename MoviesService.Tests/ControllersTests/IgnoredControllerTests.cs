using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class IgnoredControllerTests : ControllerTestsBase
{
    [Fact]
    public async Task GetAllIgnoredMovies_ReturnsOk()
    {
        // Arrange
        var ignoresRepository = new Mock<IIgnoresRepository>();
        ignoresRepository.Setup(x => x.GetAllIgnoreMovies(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<MovieDto>());

        var movieRepository = new Mock<IMovieRepository>();

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepository.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.GetAllIgnoredMovies();

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var movies = Assert.IsType<List<MovieDto>>(okObjectResult.Value);
        Assert.Empty(movies);
    }

    [Fact]
    public async Task IgnoreMovie_ReturnsNotFound_WhenMovieDoesNotExist()
    {
        // Arrange
        var ignoresRepositoryMock = new Mock<IIgnoresRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepositoryMock.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.IgnoreMovie(Guid.NewGuid());

        // Assert
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Movie does not exist", notFoundObjectResult.Value);
    }

    [Fact]
    public async Task IgnoreMovie_ReturnsBadRequest_WhenMovieIsAlreadyIgnored()
    {
        // Arrange
        var ignoresRepositoryMock = new Mock<IIgnoresRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        ignoresRepositoryMock
            .Setup(x => x.IgnoresExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepositoryMock.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.IgnoreMovie(Guid.NewGuid());

        // Assert
        var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Movie is already ignored", badRequestObjectResult.Value);
    }

    [Fact]
    public async Task IgnoreMovie_ReturnsNoContent_WhenMovieIsIgnored()
    {
        // Arrange
        var ignoresRepositoryMock = new Mock<IIgnoresRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        ignoresRepositoryMock
            .Setup(x => x.IgnoresExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepositoryMock.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.IgnoreMovie(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveMovieFromIgnored_ReturnsNotFound_WhenMovieDoesNotExist()
    {
        // Arrange
        var ignoresRepositoryMock = new Mock<IIgnoresRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepositoryMock.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.RemoveMovieFromIgnored(Guid.NewGuid());

        // Assert
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Movie does not exist", notFoundObjectResult.Value);
    }

    [Fact]
    public async Task RemoveMovieFromIgnored_ReturnsBadRequest_WhenMovieIsNotIgnored()
    {
        // Arrange
        var ignoresRepositoryMock = new Mock<IIgnoresRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        ignoresRepositoryMock
            .Setup(x => x.IgnoresExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepositoryMock.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.RemoveMovieFromIgnored(Guid.NewGuid());

        // Assert
        var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Movie is not on your ignored list", badRequestObjectResult.Value);
    }

    [Fact]
    public async Task RemoveMovieFromIgnored_ReturnsNoContent_WhenMovieIsRemovedFromIgnored()
    {
        // Arrange
        var ignoresRepositoryMock = new Mock<IIgnoresRepository>();

        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        ignoresRepositoryMock
            .Setup(x => x.IgnoresExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new IgnoredController(QueryExecutorMock.Object, ignoresRepositoryMock.Object,
            movieRepository.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.RemoveMovieFromIgnored(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}