using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers;
using MoviesService.Api.Services.Contracts;
using MoviesService.Models;
using MoviesService.Tests.ControllersTests.Base;

namespace MoviesService.Tests.ControllersTests;

public class AccountControllerTests : ControllerTestsBase
{
    private Mock<IMqttService> MqttService { get; } = new();
    
    
    [Fact]
    public async Task Login_ShouldReturnOk_WhenUsernameAndPasswordAreCorrect()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@gmail.com",
            Password = "password"
        };

        var user = new User(new Guid(), "Test", "test@gmail.com", "User");
        
        var tokenService = new Mock<ITokenService>();
        
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.LoginAsync(It.IsAny<IAsyncQueryRunner>(), loginDto))
            .ReturnsAsync(user);
        
        var claimsProvider = new Mock<IUserClaimsProvider>();
        
        var controller = new AccountController(QueryExecutorMock.Object, tokenService.Object, accountRepository.Object,
            MqttService.Object, claimsProvider.Object);

        // Act
        var result = await controller.Login(loginDto);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        userDto.Id.Should().Be(user.Id);
    }
    
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUsernameOrPasswordAreIncorrect()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@gmail.com",
            Password = "password"
        };
        
        var tokenService = new Mock<ITokenService>();
        
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.LoginAsync(It.IsAny<IAsyncQueryRunner>(), loginDto))
            .ReturnsAsync((User?)null);
        
        var claimsProvider = new Mock<IUserClaimsProvider>();
        
        var controller = new AccountController(QueryExecutorMock.Object, tokenService.Object, accountRepository.Object,
            MqttService.Object, claimsProvider.Object);
        
        // Act
        var result = await controller.Login(loginDto);
        
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        unauthorizedResult.Value.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenEmailIsNotTaken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "Test",
            Email = "test@gmail.com",
            Password = "password"
        };

        var user = new User(Guid.NewGuid(), "Test", "test@gmail.com", "User");

        var tokenService = new Mock<ITokenService>();

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<IAsyncQueryRunner>(), registerDto.Email))
            .ReturnsAsync(false);

        accountRepository.Setup(x => x.RegisterAsync(It.IsAny<IAsyncQueryRunner>(), registerDto))
            .ReturnsAsync(user);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        
        var controller = new AccountController(QueryExecutorMock.Object, tokenService.Object, accountRepository.Object,
            MqttService.Object, claimsProvider.Object);

        // Act
        var result = await controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        userDto.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailIsTaken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "Test",
            Email = "test@gmail.com",
            Password = "password"
        };

        var tokenService = new Mock<ITokenService>();

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<IAsyncQueryRunner>(), registerDto.Email))
            .ReturnsAsync(true);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        
        var controller = new AccountController(QueryExecutorMock.Object, tokenService.Object, accountRepository.Object,
            MqttService.Object, claimsProvider.Object);

        // Act
        var result = await controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        badRequestResult.Value.Should().Be("Email is taken");
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturnNoContent_WhenUserIsDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var tokenService = new Mock<ITokenService>();

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.DeleteUserAsync(It.IsAny<IAsyncQueryRunner>(), userId))
            .Returns(Task.CompletedTask);

        var claimsProvider = new Mock<IUserClaimsProvider>();
        claimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);
        
        var controller = new AccountController(QueryExecutorMock.Object, tokenService.Object, accountRepository.Object,
            MqttService.Object, claimsProvider.Object);

        // Act
        var result = await controller.DeleteAccount();

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
