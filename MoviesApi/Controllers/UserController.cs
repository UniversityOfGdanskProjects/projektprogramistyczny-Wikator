using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
public class UserController(IDriver driver, IUserRepository userRepository) : BaseApiController(driver)
{
    private IUserRepository UserRepository { get; } = userRepository;

    [HttpGet]
    public async Task<IActionResult> GetByMostActive()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId switch
            {
                null => Ok(await UserRepository.GetUsersByMostActiveAsync(tx, null)),
                _ => Ok(await UserRepository.GetUsersByMostActiveAsync(tx, Guid.Parse(userId)))
            };
        });

    }
}