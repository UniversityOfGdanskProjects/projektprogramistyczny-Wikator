using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
public class UserController(IDriver driver, IUserRepository userRepository,
    IAccountRepository accountRepository) : BaseApiController(driver)
{
    private IUserRepository UserRepository { get; } = userRepository;
    private IAccountRepository AccountRepository { get; } = accountRepository;

    [HttpGet]
    public async Task<IActionResult> GetByMostActive()
    {
        return await ExecuteReadAsync(async tx =>
            Ok(await UserRepository.GetUsersByMostActiveAsync(tx)));
    }
    
    [HttpDelete("/{id:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await UserRepository.UserExistsAsync(tx, id))
                return NotFound("User not found");

            await AccountRepository.DeleteUserAsync(tx, id);
            return NoContent();
        });
    }
}