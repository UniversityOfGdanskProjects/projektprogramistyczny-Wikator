using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Extensions;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Responses;

namespace MoviesService.Api.Controllers;

[Route("api/[controller]")]
public class UserController(
    IAsyncQueryExecutor queryExecutor,
    IUserRepository userRepository,
    IAccountRepository accountRepository,
    ITokenService tokenService) : BaseApiController(queryExecutor)
{
    private IUserRepository UserRepository { get; } = userRepository;
    private IAccountRepository AccountRepository { get; } = accountRepository;
    private ITokenService TokenService { get; } = tokenService;


    [HttpGet]
    public async Task<IActionResult> GetByMostActive()
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
            Ok(await UserRepository.GetUsersByMostActiveAsync(tx)));
    }

    [HttpGet("active-today-count")]
    public async Task<IActionResult> GetActiveTodayCount()
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
            Ok(await UserRepository.GetUserActiveTodayCount(tx)));
    }

    [HttpPut("username")]
    [Authorize]
    public async Task<IActionResult> UpdateUsername(UpdateUsernameDto updateUsernameDto)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            var user = await UserRepository.UpdateUserNameAsync(tx, userId, updateUsernameDto.NewUsername);

            var userDto = new UserDto(user.Id, user.Name, user.Role, TokenService.CreateToken(user));
            return Ok(userDto);
        });
    }

    [HttpPut("{id:guid}/username")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> UpdateUsername(Guid id, UpdateUsernameDto updateUsernameDto)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await UserRepository.UserExistsAsync(tx, id))
                return NotFound("User not found");

            await UserRepository.UpdateUserNameAsync(tx, id, updateUsernameDto.NewUsername);
            return NoContent();
        });
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> ChangeUserRoleToAdmin(Guid id)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await UserRepository.UserExistsAsync(tx, id))
                return NotFound("User not found");

            await UserRepository.ChangeUserRoleToAdminAsync(tx, id);
            return NoContent();
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteUserAsAdmin(Guid id)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await UserRepository.UserExistsAsync(tx, id))
                return NotFound("User not found");

            await AccountRepository.DeleteUserAsync(tx, id);
            return NoContent();
        });
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> DeleteUser()
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            await AccountRepository.DeleteUserAsync(tx, userId);
            return NoContent();
        });
    }
}