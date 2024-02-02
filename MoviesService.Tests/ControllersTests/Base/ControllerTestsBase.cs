﻿using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MoviesService.DataAccess.Contracts;
using MoviesService.Services.Contracts;

namespace MoviesService.Tests.ControllersTests.Base;

public abstract class ControllerTestsBase
{
    protected Mock<IAsyncQueryExecutor> QueryExecutorMock { get; }
    protected Mock<IUserClaimsProvider> ClaimsProviderMock { get; }
    protected Guid UserId { get; } = Guid.NewGuid();

    protected ControllerTestsBase()
    {
        QueryExecutorMock = new Mock<IAsyncQueryExecutor>();
        var sessionMock = new Mock<IAsyncQueryRunner>();
        
        QueryExecutorMock.Setup(executor => executor.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task<IActionResult>>>()))
            .Returns<Func<IAsyncQueryRunner, Task<IActionResult>>>(func => func.Invoke(sessionMock.Object));
        
        QueryExecutorMock.Setup(executor => executor.ExecuteWriteAsync(It.IsAny<Func<IAsyncQueryRunner, Task<IActionResult>>>()))
            .Returns<Func<IAsyncQueryRunner, Task<IActionResult>>>(func => func.Invoke(sessionMock.Object));
        
        ClaimsProviderMock = new Mock<IUserClaimsProvider>();
        ClaimsProviderMock.Setup(provider => provider.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(UserId);
    }
}