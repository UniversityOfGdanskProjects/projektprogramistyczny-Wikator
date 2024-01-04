using System.Security.Claims;

namespace MoviesApi.Tests.ControllerTests;

public class WatchlistControllerTests
{
    [Fact]
    public async Task GetAllMoviesOnWatchlist_Returns_OkResult()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.GetAllMoviesOnWatchlist(It.IsAny<Guid>()))
            .ReturnsAsync(new List<MovieDto>());
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.GetAllMoviesOnWatchlist();
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task GetAllMoviesOnWatchlist_Returns_List_Of_Movies()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.GetAllMoviesOnWatchlist(It.IsAny<Guid>()))
            .ReturnsAsync(new List<MovieDto>());
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.GetAllMoviesOnWatchlist();
        
        // Assert
        Assert.IsType<List<MovieDto>>(((OkObjectResult)result).Value);
    }
    
    [Fact]
    public async Task AddToWatchList_Returns_OkResult_If_Successful()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.AddToWatchList(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.Completed));
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.AddToWatchList(new Guid());
        
        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task AddToWatchList_Returns_NotFound_If_Movie_Not_Found()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.AddToWatchList(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.NotFound));
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.AddToWatchList(new Guid());
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task AddToWatchList_Returns_BadRequest_If_Movie_Already_In_Watchlist()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.AddToWatchList(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.EntityAlreadyExists));
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.AddToWatchList(new Guid());
        
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    // Test another method now
    [Fact]
    public async Task RemoveFromWatchList_Returns_NoContent_If_Successful()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.RemoveFromWatchList(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.Completed));
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.RemoveFromWatchList(new Guid());
        
        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task RemoveFromWatchList_Returns_NotFound_If_Movie_Or_Watchlist_Not_Found()
    {
        // Arrange
        var watchlistRepository = new Mock<IWatchlistRepository>();
        watchlistRepository.Setup(x => x.RemoveFromWatchList(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.NotFound));
        var userClaimsProvider = new Mock<IUserClaimsProvider>();
        userClaimsProvider.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(new Guid());
        var controller = new WatchlistController(watchlistRepository.Object, userClaimsProvider.Object);
        
        // Act
        var result = await controller.RemoveFromWatchList(new Guid());
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}