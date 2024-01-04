namespace MoviesApi.Tests.ControllerTests;

public class ActorsControllerTests
{
    [Fact]
    public async Task Get_Returns_OkResult_With_Actors()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.GetAllActors()).ReturnsAsync(new List<ActorDto>());
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.GetAll();
        
        //
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Get_Returns_OkResult_If_Exists()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.GetActor(It.IsAny<Guid>()))
            .ReturnsAsync(SampleActorDto());
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.GetActor(It.IsAny<Guid>());
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task Get_Returns_NotFoundResult_If_Does_Not_Exist()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.GetActor(It.IsAny<Guid>()))
            .ReturnsAsync((ActorDto?)null);
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.GetActor(It.IsAny<Guid>());
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Post_Returns_CreatedAtResult_If_Successful()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.CreateActor(It.IsAny<UpsertActorDto>()))
            .ReturnsAsync(new QueryResult<ActorDto>(QueryResultStatus.Completed, SampleActorDto()));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.CreateActor(It.IsAny<UpsertActorDto>());
        
        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
    }
    
    [Fact]
    public async Task Post_Returns_New_Actor_If_Successful()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.CreateActor(It.IsAny<UpsertActorDto>()))
            .ReturnsAsync(new QueryResult<ActorDto>(QueryResultStatus.Completed, SampleActorDto()));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.CreateActor(It.IsAny<UpsertActorDto>());
        
        // Assert
        Assert.IsType<ActorDto>(((ObjectResult)result).Value);
    }
    
    [Fact]
    public async Task Post_Throws_Exception_If_Photo_Failed_To_Save()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.CreateActor(It.IsAny<UpsertActorDto>()))
            .ReturnsAsync(new QueryResult<ActorDto>(QueryResultStatus.PhotoFailedToSave, null));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act and Assert
        await Assert.ThrowsAsync<PhotoServiceException>(async () =>
        {
            await controller.CreateActor(It.IsAny<UpsertActorDto>());
        });
    }
    
    [Fact]
    public async Task Put_Returns_OkResult_If_Successful()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.UpdateActor(It.IsAny<Guid>(), It.IsAny<UpsertActorDto>()))
            .ReturnsAsync(new QueryResult<ActorDto>(QueryResultStatus.Completed, SampleActorDto()));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.UpdateActor(It.IsAny<Guid>(), It.IsAny<UpsertActorDto>());
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task Put_Returns_New_Actor_If_Successful()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.UpdateActor(It.IsAny<Guid>(), It.IsAny<UpsertActorDto>()))
            .ReturnsAsync(new QueryResult<ActorDto>(QueryResultStatus.Completed, SampleActorDto()));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.UpdateActor(It.IsAny<Guid>(), It.IsAny<UpsertActorDto>());
        
        // Assert
        Assert.IsType<ActorDto>(((ObjectResult)result).Value);
    }
    
    [Fact]
    public async Task Put_Returns_NotFoundResult_If_Actor_Does_Not_Exist()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.UpdateActor(It.IsAny<Guid>(), It.IsAny<UpsertActorDto>()))
            .ReturnsAsync(new QueryResult<ActorDto>(QueryResultStatus.NotFound, null));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.UpdateActor(It.IsAny<Guid>(), It.IsAny<UpsertActorDto>());
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Delete_Returns_NoContentResult_If_Successful()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.DeleteActor(It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.Completed));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.DeleteActor(It.IsAny<Guid>());
        
        // Assert
        Assert.IsType<NoContentResult>(result);
    }
    
    [Fact]
    public async Task Delete_Returns_NotFoundResult_If_Actor_Does_Not_Exist()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.DeleteActor(It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.NotFound));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act
        var result = await controller.DeleteActor(It.IsAny<Guid>());
        
        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Delete_Throws_Exception_If_Photo_Fails_To_Delete()
    {
        // Arrange
        var actorsRepository = new Mock<IActorRepository>();
        actorsRepository.Setup(x => x.DeleteActor(It.IsAny<Guid>()))
            .ReturnsAsync(new QueryResult(QueryResultStatus.PhotoFailedToDelete));
        var controller = new ActorsController(actorsRepository.Object);
        
        // Act and Assert
        await Assert.ThrowsAsync<PhotoServiceException>(async () =>
        {
            await controller.DeleteActor(It.IsAny<Guid>());
        });
    }
    
    private static ActorDto SampleActorDto() =>
        new (new Guid(), "Test", "Test", new DateOnly(), "Test", null);
}
