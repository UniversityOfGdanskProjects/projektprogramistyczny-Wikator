using Microsoft.AspNetCore.Mvc;
using MoviesService.DataAccess.Contracts;

namespace MoviesService.Api.Controllers.Base;

[ApiController]
public abstract class BaseApiController(IAsyncQueryExecutor queryExecutor) : ControllerBase
{
    protected IAsyncQueryExecutor QueryExecutor { get; } = queryExecutor;
}