using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Helpers;
using MoviesService.Models.Parameters;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class NotificationControllerTests : ControllerTestsBase
{
    [Fact]
    public async Task GetAllNotificationsAsync_ShouldReturnOkObjectResult()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        var notificationQueryParams = new NotificationQueryParams();

        var sampleNotificationDto = new NotificationDto(Guid.NewGuid(), false, DateTime.Now, "admin", "Content", Guid.NewGuid(), "The Matrix");
        
        notificationRepository.Setup(x => x.GetAllNotificationsAsync(It.IsAny<IAsyncQueryRunner>(), notificationQueryParams, It.IsAny<Guid>()))
            .ReturnsAsync(new PagedList<NotificationDto>(new List<NotificationDto> { sampleNotificationDto }, 1, 1, 2));

        // Act
        var result = await notificationController.GetAllNotificationsAsync(notificationQueryParams);

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<NotificationDto>>(okObjectResult.Value);
        items.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task MarkNotificationAsRead_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        var notificationId = Guid.NewGuid();
        
        notificationRepository.Setup(x => x.NotificationExistsAsync(It.IsAny<IAsyncQueryRunner>(), notificationId, It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act
        var result = await notificationController.MarkNotificationAsRead(notificationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        notFoundResult.Value.Should().Be("Notification not found");
    }
    
    [Fact]
    public async Task MarkNotificationAsRead_ShouldReturnNoContent_WhenNotificationExists()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        var notificationId = Guid.NewGuid();
        
        notificationRepository.Setup(x => x.NotificationExistsAsync(It.IsAny<IAsyncQueryRunner>(), notificationId, It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        var result = await notificationController.MarkNotificationAsRead(notificationId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task MarkAllNotificationsAsRead_ShouldReturnNoContent()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        // Act
        var result = await notificationController.MarkAllNotificationsAsRead();

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task DeleteNotification_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        var notificationId = Guid.NewGuid();
        
        notificationRepository.Setup(x => x.NotificationExistsAsync(It.IsAny<IAsyncQueryRunner>(), notificationId, It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act
        var result = await notificationController.DeleteNotification(notificationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        notFoundResult.Value.Should().Be("Notification not found");
    }
    
    [Fact]
    public async Task DeleteNotification_ShouldReturnNoContent_WhenNotificationExists()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        var notificationId = Guid.NewGuid();
        
        notificationRepository.Setup(x => x.NotificationExistsAsync(It.IsAny<IAsyncQueryRunner>(), notificationId, It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        var result = await notificationController.DeleteNotification(notificationId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task DeleteAllNotifications_ShouldReturnNotContent()
    {
        // Arrange
        var notificationRepository = new Mock<INotificationRepository>();
        var responseHandler = new Mock<IResponseHandler>();
        var notificationController = new NotificationController(QueryExecutorMock.Object, notificationRepository.Object,
            ClaimsProviderMock.Object, responseHandler.Object);
        
        // Act
        var result = await notificationController.DeleteAllNotifications();

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}