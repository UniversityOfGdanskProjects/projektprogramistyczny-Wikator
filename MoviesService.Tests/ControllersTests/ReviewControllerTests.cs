using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Services.Contracts;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class ReviewControllerTests : ControllerTestsBase
{
    private Mock<IMqttService> MqttServiceMock { get; } = new();

    [Fact]
    public async Task CreateReview_WhenMovieDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new ReviewController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            It.IsAny<IReviewRepository>(), MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.CreateReview(new AddReviewDto());

        // Assert
        var badResultObject = result.Should().BeOfType<BadRequestObjectResult>();
        badResultObject.Subject.Value.Should().Be("Movie you are trying to review does not exist");
    }

    [Fact]
    public async Task CreateReview_WhenUserAlreadyReviewedMovie_ReturnsBadRequest()
    {
        // Arrange
        var reviewDto = new AddReviewDto
        {
            MovieId = Guid.NewGuid(),
            Score = 5
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), reviewDto.MovieId))
            .ReturnsAsync(true);

        var reviewRepositoryMock = new Mock<IReviewRepository>();
        reviewRepositoryMock.Setup(x => x.ReviewExistsByMovieId(It.IsAny<IAsyncQueryRunner>(),
                It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var controller = new ReviewController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            reviewRepositoryMock.Object,
            MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.CreateReview(reviewDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>();
        badRequestResult.Subject.Value.Should().Be("You already reviewed this movie");
    }

    [Fact]
    public async Task CreateReview_WhenReviewAdded_ReturnsOk()
    {
        // Arrange
        var reviewDto = new AddReviewDto
        {
            MovieId = Guid.NewGuid(),
            Score = 5
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(x => x.MovieExists(It.IsAny<IAsyncQueryRunner>(), reviewDto.MovieId))
            .ReturnsAsync(true);

        var reviewRepositoryMock = new Mock<IReviewRepository>();
        reviewRepositoryMock.Setup(x => x.ReviewExistsByMovieId(It.IsAny<IAsyncQueryRunner>(),
                reviewDto.MovieId, It.IsAny<Guid>()))
            .ReturnsAsync(false);

        reviewRepositoryMock.Setup(x => x.AddReview(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), reviewDto))
            .ReturnsAsync(new ReviewDto(Guid.NewGuid(), Guid.NewGuid(), reviewDto.MovieId, reviewDto.Score));

        var controller = new ReviewController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            reviewRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.CreateReview(reviewDto);

        // Assert
        var okObjectResult = result.Should().BeOfType<OkObjectResult>();
        okObjectResult.Subject.Value.Should().BeOfType<ReviewDto>();
    }

    [Fact]
    public async Task UpdateReview_WhenReviewDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var reviewRepositoryMock = new Mock<IReviewRepository>();
        reviewRepositoryMock
            .Setup(x => x.ReviewExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new ReviewController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            reviewRepositoryMock.Object,
            MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.UpdateReview(Guid.NewGuid(), new UpdateReviewDto());

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>();
        notFoundResult.Subject.Value.Should().Be("Review does not exist, or you don't have permission to edit it");
    }

    [Fact]
    public async Task UpdateReview_WhenReviewExists_ReturnsOk()
    {
        // Arrange
        var reviewRepositoryMock = new Mock<IReviewRepository>();
        reviewRepositoryMock
            .Setup(x => x.ReviewExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        reviewRepositoryMock.Setup(x => x.UpdateReview(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<UpdateReviewDto>()))
            .ReturnsAsync(new ReviewDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5));

        var controller = new ReviewController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            reviewRepositoryMock.Object,
            MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.UpdateReview(Guid.NewGuid(), new UpdateReviewDto());

        // Assert
        var okObjectResult = result.Should().BeOfType<OkObjectResult>();
        okObjectResult.Subject.Value.Should().BeOfType<ReviewDto>();
    }

    [Fact]
    public async Task DeleteReview_WhenReviewExists_ReturnsNoContent()
    {
        // Arrange
        var reviewRepositoryMock = new Mock<IReviewRepository>();

        reviewRepositoryMock.Setup(x =>
                x.GetMovieIdFromReviewId(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Guid.NewGuid());

        var controller = new ReviewController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            reviewRepositoryMock.Object,
            MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.DeleteReview(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteReview_WhenReviewDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var reviewRepositoryMock = new Mock<IReviewRepository>();
        reviewRepositoryMock.Setup(x =>
                x.GetMovieIdFromReviewId(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Guid?)null);

        var controller = new ReviewController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            reviewRepositoryMock.Object,
            MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.DeleteReview(Guid.NewGuid());

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>();
        notFoundResult.Subject.Value.Should().Be("Review does not exist, or you don't have permission to delete it");
    }
}