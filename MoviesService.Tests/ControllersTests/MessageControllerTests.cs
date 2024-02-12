using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Services.Contracts;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class MessageControllerTests : ControllerTestsBase
{
    [Fact]
    public async Task GetMostRecentMessages_ShouldReturnOkObjectResult()
    {
        // Arrange
        var messages = new List<MessageDto>
        {
            new("Content", "Admin", DateTime.Now),
            new("Content", "Admin", DateTime.Now)
        };

        var messageRepository = new Mock<IMessageRepository>();
        messageRepository.Setup(x => x.GetMostRecentMessagesAsync(It.IsAny<IAsyncQueryRunner>()))
            .ReturnsAsync(messages);

        var mqttService = new Mock<IMqttService>();
        var controller = new MessageController(QueryExecutorMock.Object, messageRepository.Object, mqttService.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.GetMostRecentMessagesAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MessageDto>>(okResult.Value);
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateMessage_ShouldReturnOkObjectResult()
    {
        // Arrange
        var message = new MessageDto("Content", "Admin", DateTime.Now);
        var messageRepository = new Mock<IMessageRepository>();
        messageRepository.Setup(x =>
                x.CreateMessageAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(message);

        var mqttService = new Mock<IMqttService>();
        var controller = new MessageController(QueryExecutorMock.Object, messageRepository.Object, mqttService.Object,
            ClaimsProviderMock.Object);

        // Act
        var result = await controller.CreateMessageAsync(new CreateMessageDto
        {
            Content = "Content"
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsAssignableFrom<MessageDto>(okResult.Value);
        model.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task CreateMessage_MqttShouldFireOnce()
    {
        // Arrange
        var message = new MessageDto("Content", "Admin", DateTime.Now);
        var messageRepository = new Mock<IMessageRepository>();
        messageRepository.Setup(x =>
                x.CreateMessageAsync(It.IsAny<IAsyncQueryRunner>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(message);

        var mqttService = new Mock<IMqttService>();
        var controller = new MessageController(QueryExecutorMock.Object, messageRepository.Object, mqttService.Object,
            ClaimsProviderMock.Object);

        // Act
        await controller.CreateMessageAsync(new CreateMessageDto
        {
            Content = "Content"
        });

        // Assert
        mqttService.Verify(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}