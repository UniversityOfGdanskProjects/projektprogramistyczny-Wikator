using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Services.Contracts;
using MoviesService.Models;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class CommentControllerTests : ControllerTestsBase
{
    private Mock<IMqttService> MqttServiceMock { get; } = new();

    [Fact]
    public async Task GetComment_ReturnsNotFound_IfCommentDoesNotExist()
    {
        // Arrange
        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo => repo.GetCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync((CommentDto?)null);

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.GetComment(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetComment_ReturnsOk_IfCommentExists()
    {
        // Arrange
        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo => repo.GetCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(new CommentDto(Guid.NewGuid(), Guid.NewGuid(), UserId, "admin",
                "content", DateTime.Now, false));

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.GetComment(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddCommentAsync_ReturnsCreatedAtResult_IfMovieExists()
    {
        // Arrange
        var addCommentDto = new AddCommentDto
        {
            Text = "Content",
            MovieId = It.IsAny<Guid>()
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(repo => repo.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.AddCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<AddCommentDto>()))
            .ReturnsAsync(SampleData(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.Now));

        var controller = new CommentController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.AddCommentAsync(addCommentDto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task AddCommentAsync_ReturnsNewComment_IfMovieExists()
    {
        // Arrange
        var addCommentDto = new AddCommentDto
        {
            Text = "Content",
            MovieId = It.IsAny<Guid>()
        };

        var commentId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var date = DateTime.Now;

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(repo => repo.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.AddCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<AddCommentDto>()))
            .ReturnsAsync(SampleData(commentId, movieId, UserId, date));

        var controller = new CommentController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.AddCommentAsync(addCommentDto);

        // Assert
        var createdAtActionResult = result as CreatedAtActionResult;
        createdAtActionResult?.Value.Should().BeOfType<CommentDto>();
        createdAtActionResult?.Value.Should().Be(SampleData(commentId, movieId, UserId, date).Comment);
    }

    [Fact]
    public async Task AddCommentAsync_ReturnsBadRequest_IfMovieDoesNotExist()
    {
        // Arrange
        var addCommentDto = new AddCommentDto
        {
            Text = "Content",
            MovieId = It.IsAny<Guid>()
        };

        var movieRepositoryMock = new Mock<IMovieRepository>();
        movieRepositoryMock.Setup(repo => repo.MovieExists(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.AddCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<AddCommentDto>()))
            .ReturnsAsync(SampleData(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.Now));

        var controller = new CommentController(QueryExecutorMock.Object, movieRepositoryMock.Object,
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.AddCommentAsync(addCommentDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EditCommentAsync_ReturnsNotFound_IfCommentDoesNotExist()
    {
        // Arrange
        var editCommentDto = new EditCommentDto
        {
            Text = "Content"
        };

        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.CommentExistsAsOwnerOrAdmin(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.EditCommentAsync(Guid.NewGuid(), editCommentDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EditCommentAsync_ReturnsOk_IfCommentExists()
    {
        // Arrange
        var editCommentDto = new EditCommentDto
        {
            Text = "Content"
        };

        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.CommentExistsAsOwnerOrAdmin(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        commentRepositoryMock.Setup(repo => repo.EditCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<EditCommentDto>()))
            .ReturnsAsync(SampleData(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.Now).Comment);

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.EditCommentAsync(Guid.NewGuid(), editCommentDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task EditCommentAsync_ReturnsNewComment_IfCommentExists()
    {
        // Arrange
        var editCommentDto = new EditCommentDto
        {
            Text = "Content"
        };

        var commentId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var date = DateTime.Now;

        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.CommentExistsAsOwnerOrAdmin(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        commentRepositoryMock.Setup(repo => repo.EditCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<EditCommentDto>()))
            .ReturnsAsync(SampleData(commentId, movieId, UserId, date).Comment);

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.EditCommentAsync(Guid.NewGuid(), editCommentDto);

        // Assert
        var okObjectResult = result as OkObjectResult;
        okObjectResult?.Value.Should().BeOfType<CommentDto>();
        okObjectResult?.Value.Should().Be(SampleData(commentId, movieId, UserId, date).Comment);
    }

    [Fact]
    public async Task DeleteCommentAsync_ReturnsNotFound_IfCommentDoesNotExist()
    {
        // Arrange
        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.GetMovieIdFromCommentAsOwnerOrAdminAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
            .ReturnsAsync((Guid?)null);

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.DeleteCommentAsync(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteCommentAsync_ReturnsNoContent_IfCommentExists()
    {
        // Arrange
        var commentRepositoryMock = new Mock<ICommentRepository>();
        commentRepositoryMock.Setup(repo =>
                repo.GetMovieIdFromCommentAsOwnerOrAdminAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
            .ReturnsAsync(Guid.NewGuid());
        commentRepositoryMock.Setup(repo =>
                repo.DeleteCommentAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var controller = new CommentController(QueryExecutorMock.Object, It.IsAny<IMovieRepository>(),
            commentRepositoryMock.Object, MqttServiceMock.Object, ClaimsProviderMock.Object);

        // Act
        var result = await controller.DeleteCommentAsync(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    private static CommentWithNotification SampleData(Guid commentId, Guid movieId, Guid userId, DateTime date)
    {
        return new CommentWithNotification(new CommentDto(commentId, movieId, userId, "admin",
            "content", date, false), new RealTimeNotification("Admin", "content",
            movieId, "The Matrix"));
    }
}