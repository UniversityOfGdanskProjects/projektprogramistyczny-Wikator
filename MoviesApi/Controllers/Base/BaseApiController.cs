using Microsoft.AspNetCore.Mvc;
using MoviesApi.Helpers;

namespace MoviesApi.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(LogUserActivity))]
public abstract class BaseApiController : ControllerBase;