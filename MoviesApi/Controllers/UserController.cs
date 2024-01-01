using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

public class UserController(IUserRepository userRepository) : BaseApiController
{
    private IUserRepository UserRepository { get; } = userRepository;

    [HttpGet]
    public async Task<IActionResult> GetByMostActive()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId switch
        {
            null => Ok(await UserRepository.GetUsersByMostActiveAsync(null)),
            _ => Ok(await UserRepository.GetUsersByMostActiveAsync(int.Parse(userId)))
        };
    }
}