using System.Security.Claims;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Exceptions;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Helpers;
using MoviesService.Models.Parameters;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class MovieControllerTests : ControllerTestsBase
{
    private Mock<IResponseHandler> ResponseHandlerMock { get; } = new();

    [Fact]
    public async Task GetMovies_WhenNotLoggedIn_ShouldReturnOkObjectResult()
    {
        // Arrange
        var queryParams = new MovieQueryParams();
        var pagedList = new PagedList<MovieDto>(new List<MovieDto>(), 1, 1, 10);

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.GetMoviesWhenNotLoggedIn(It.IsAny<IAsyncQueryRunner>(), queryParams))
            .ReturnsAsync(pagedList);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();
        claimsProviderMock.Setup(x => x.GetUserIdOrDefault(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.GetMovies(queryParams);

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MovieDto>>(okObjectResult.Value);
        Assert.Equal(pagedList.Items, model);
    }

    [Fact]
    public async Task GetMovies_WhenLoggedIn_ShouldReturnOkObjectResult()
    {
        // Arrange
        var queryParams = new MovieQueryParams();
        var pagedList = new PagedList<MovieDto>(new List<MovieDto>(), 1, 1, 10);

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x =>
                x.GetMoviesExcludingIgnored(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), queryParams))
            .ReturnsAsync(pagedList);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();
        claimsProviderMock.Setup(x => x.GetUserIdOrDefault(It.IsAny<ClaimsPrincipal>()))
            .Returns(Guid.NewGuid());

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.GetMovies(queryParams);

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MovieDto>>(okObjectResult.Value);
        Assert.Equal(pagedList.Items, model);
    }

    [Fact]
    public async Task GetMovie_WhenMovieExistsAndLoggedIn_ShouldReturnOkObjectResult()
    {
        // Arrange
        var movie = new MovieDetailsDto(Guid.NewGuid(), "The Matrix", "Description", false, 5.0,
            null, DateOnly.MaxValue, 13, null, false, false,
            null, 1, [], [], []);

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x =>
                x.GetMovieDetails(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(movie);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();
        claimsProviderMock.Setup(x => x.GetUserIdOrDefault(It.IsAny<ClaimsPrincipal>()))
            .Returns(Guid.NewGuid());

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.GetMovie(Guid.NewGuid());

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<MovieDetailsDto>(okObjectResult.Value);
        Assert.Equal(movie, model);
    }

    [Fact]
    public async Task GetMovie_WhenMovieExistsAndNotLoggedIn_ShouldReturnOkObjectResult()
    {
        // Arrange
        var movie = new MovieDetailsDto(Guid.NewGuid(), "The Matrix", "Description", false, 5.0,
            null, DateOnly.MaxValue, 13, null, false, false,
            null, 1, [], [], []);

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x =>
                x.GetMovieDetails(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(movie);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();
        claimsProviderMock.Setup(x => x.GetUserIdOrDefault(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.GetMovie(Guid.NewGuid());

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<MovieDetailsDto>(okObjectResult.Value);
        Assert.Equal(movie, model);
    }

    [Fact]
    public async Task GetMovie_WhenMovieDoesNotExist_ShouldReturnNotFoundResult()
    {
        // Arrange
        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x =>
                x.GetMovieDetails(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync((MovieDetailsDto?)null);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();
        claimsProviderMock.Setup(x => x.GetUserIdOrDefault(It.IsAny<ClaimsPrincipal>()))
            .Returns(Guid.NewGuid());

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.GetMovie(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateMovie_ShouldThrow_WhenUploadPhotoResultContainsErrors()
    {
        // Arrange
        var movieDto = new AddMovieDto
        {
            Title = "The Matrix",
            Description = "Description",
            ReleaseDate = DateOnly.MaxValue,
            FileContent = [0]
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();

        var photoServiceMock = new Mock<IPhotoService>();
        photoServiceMock.Setup(x => x.AddPhotoAsync(It.IsAny<IFormFile>(), "auto"))
            .ReturnsAsync(new ImageUploadResult
            {
                Error = new Error()
            });

        var claimsProviderMock = new Mock<IUserClaimsProvider>();

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Assert
        await Assert.ThrowsAsync<PhotoServiceException>((Func<Task<IActionResult>>)Act);
        return;

        // Act
        async Task<IActionResult> Act()
            => await controller.CreateMovie(movieDto);
        
    }

    [Fact]
    public async Task CreateMovie_ShouldReturnCreatedAtActionObjectResult_WhenUploadPhotoResultDoesNotContainErrors()
    {
        // Arrange
        var movieDto = new AddMovieDto
        {
            Title = "The Matrix",
            Description = "Description",
            ReleaseDate = DateOnly.MaxValue,
            FileContent = [0]
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.AddMovie(It.IsAny<IAsyncQueryRunner>(), movieDto,
                new Uri("https://url.com").AbsoluteUri, "public-id"))
            .ReturnsAsync(new MovieDetailsDto(Guid.NewGuid(), "The Matrix", "Description", false, 5.0,
                new Uri("https://url.com").AbsoluteUri, DateOnly.MaxValue, 13, "public-id", false, false,
                null, 1, [], [], []));

        var photoServiceMock = new Mock<IPhotoService>();
        photoServiceMock.Setup(x => x.AddPhotoAsync(It.IsAny<IFormFile>(), "auto"))
            .ReturnsAsync(new ImageUploadResult
            {
                SecureUrl = new Uri("https://url.com"),
                PublicId = "public-id"
            });

        var claimsProviderMock = new Mock<IUserClaimsProvider>();

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.CreateMovie(movieDto);

        // Assert
        var okObjectResult = Assert.IsType<CreatedAtActionResult>(result);
        var model = Assert.IsAssignableFrom<MovieDetailsDto>(okObjectResult.Value);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task EditMovie_ShouldReturnNotFoundResult_WhenMovieDoesNotExist()
    {
        // Arrange
        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.EditMovie(Guid.NewGuid(), It.IsAny<EditMovieDto>());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task EditMovie_ShouldReturnOkObjectResult_WhenMovieExists()
    {
        // Arrange
        var movieDto = new EditMovieDto
        {
            Title = "The Matrix",
            Description = "Description",
            ReleaseDate = DateOnly.MaxValue
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        movieRepositoryMock.Setup(x =>
                x.EditMovie(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>(), movieDto))
            .ReturnsAsync(new MovieDetailsDto(Guid.NewGuid(), "The Matrix", "Description", false, 5.0,
                null, DateOnly.MaxValue, 13, null, false, false,
                null, 1, [], [], []));

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.EditMovie(Guid.NewGuid(), movieDto);

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<MovieDetailsDto>(okObjectResult.Value);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task DeleteMovie_ShouldReturnNotFoundResult_WhenMovieDoesNotExist()
    {
        // Arrange
        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.DeleteMovie(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMovie_ShouldReturnNoContentResult_WhenMovieExists()
    {
        // Arrange
        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        movieRepositoryMock.Setup(x => x.DeleteMovie(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var photoServiceMock = new Mock<IPhotoService>();

        var claimsProviderMock = new Mock<IUserClaimsProvider>();

        var controller = new MovieController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            photoServiceMock.Object, claimsProviderMock.Object, ResponseHandlerMock.Object);

        // Act
        var result = await controller.DeleteMovie(Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}