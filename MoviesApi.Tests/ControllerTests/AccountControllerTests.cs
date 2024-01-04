using MoviesApi.Models;

namespace MoviesApi.Tests.ControllerTests;

public class AccountControllerTests
{
    [Fact]
    public async Task Login_Returns_Unauthorized_If_Email_Does_Not_Exist()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Login(new LoginDto
        {
            Email = "Test",
            Password = "Test"
        });
        
        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public async Task Login_Returns_Unauthorized_If_Password_Is_Incorrect()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        accountRepository.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync((User?)null);
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Login(new LoginDto
        {
            Email = "Test",
            Password = "Test"
        });
        
        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public async Task Login_Returns_OkResult_If_Successful()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        accountRepository.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(new User(
                Name: "Test",
                Email: "Test",
                Id: new Guid(),
                Role: Role.User
            ));
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Login(new LoginDto
        {
            Email = "Test",
            Password = "Test"
        });
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task Login_Returns_User_If_Successful()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        accountRepository.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(new User(
                Name: "Test",
                Email: "Test",
                Id: new Guid(),
                Role: Role.User
            ));
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Login(new LoginDto
        {
            Email = "Test",
            Password = "Test"
        });
        
        // Assert
        Assert.IsType<UserDto>(((ObjectResult)result).Value);
    }
    
    [Fact]
    public async Task Register_Returns_BadRequest_If_Email_Exists()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Register(new RegisterDto
        {
            Email = "Test",
            Password = "Test",
            Name = "Test"
        });
        
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task Register_Returns_OkResult_If_Successful()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        accountRepository.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(new User(
                Name: "Test",
                Email: "Test",
                Id: new Guid(),
                Role: Role.User
            ));
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Register(new RegisterDto
        {
            Email = "Test",
            Password = "Test",
            Name = "Test"
        });
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task Register_Returns_New_User_If_Successful()
    {
        // Arrange
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        accountRepository.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(new User(
                Name: "Test",
                Email: "Test",
                Id: new Guid(),
                Role: Role.User
            ));
        var controller = new AccountController(new Mock<ITokenService>().Object, accountRepository.Object);
        
        // Act
        var result = await controller.Register(new RegisterDto
        {
            Email = "Test",
            Password = "Test",
            Name = "Test"
        });
        
        // Assert
        Assert.IsType<UserDto>(((ObjectResult)result).Value);
    }
}