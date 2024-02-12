using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Services.Contracts;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class FavouriteControllerTests : ControllerTestsBase
{
    [Fact]
    public async Task GetAllMoviesOnFavouriteList_ReturnsOkObjectResult()
    {
        // Arrange
        var favouriteRepository = new Mock<IFavouriteRepository>();
        favouriteRepository.Setup(x => x.GetAllFavouriteMovies(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<MovieDto>());

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var movieRepository = new Mock<IMovieRepository>();
        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.GetAllMoviesOnFavouriteList();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MovieDto>>(okResult.Value);
        model.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToFavouriteList_ReturnsNotFoundObjectResult_WhenMovieNotFound()
    {
        // Arrange
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var favouriteRepository = new Mock<IFavouriteRepository>();
        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.AddToFavouriteList(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        notFoundResult.Value.Should().Be("Movie does not exist found");
    }

    [Fact]
    public async Task AddToFavouriteList_ReturnsBadRequestObjectResult_WhenMovieIsAlreadyFavourite()
    {
        // Arrange
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var favouriteRepository = new Mock<IFavouriteRepository>();
        favouriteRepository.Setup(x =>
                x.MovieIsFavourite(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.AddToFavouriteList(Guid.NewGuid());

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        badRequestResult.Value.Should().Be("Movie is already favourite");
    }

    [Fact]
    public async Task AddToFavouriteList_ReturnsNoContentResult_WhenMovieIsAddedToFavourites()
    {
        // Arrange
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var favouriteRepository = new Mock<IFavouriteRepository>();
        favouriteRepository.Setup(x =>
                x.MovieIsFavourite(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.AddToFavouriteList(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveFromFavourites_ReturnsNotFoundObjectResult_WhenMovieNotFound()
    {
        // Arrange
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var favouriteRepository = new Mock<IFavouriteRepository>();
        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.RemoveFromFavourites(Guid.NewGuid());

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        notFoundResult.Value.Should().Be("Movie does not exist");
    }

    [Fact]
    public async Task RemoveFromFavourites_ReturnsBadRequestObjectResult_WhenMovieIsNotOnFavouriteList()
    {
        // Arrange
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var favouriteRepository = new Mock<IFavouriteRepository>();
        favouriteRepository.Setup(x =>
                x.MovieIsFavourite(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.RemoveFromFavourites(Guid.NewGuid());

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        badRequestResult.Value.Should().Be("This movie is not on your favourite list");
    }

    [Fact]
    public async Task RemoveFromFavourites_ReturnsNoContentResult_WhenMovieIsRemovedFromFavourites()
    {
        // Arrange
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(Guid.NewGuid());

        var favouriteRepository = new Mock<IFavouriteRepository>();
        favouriteRepository.Setup(x =>
                x.MovieIsFavourite(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new FavouriteController(QueryExecutorMock.Object, favouriteRepository.Object,
            movieRepository.Object, claimsProvider.Object);

        // Act
        var result = await controller.RemoveFromFavourites(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}