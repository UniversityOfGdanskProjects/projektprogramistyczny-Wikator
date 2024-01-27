using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
public class UserController(IDriver driver, IUserRepository userRepository,
    IAccountRepository accountRepository, ITokenService tokenService) : BaseApiController(driver)
{
    private IUserRepository UserRepository { get; } = userRepository;
    private IAccountRepository AccountRepository { get; } = accountRepository;
    private ITokenService TokenService { get; } = tokenService;
    

    [HttpGet]
    public async Task<IActionResult> GetByMostActive()
    {
        return await ExecuteReadAsync(async tx =>
            Ok(await UserRepository.GetUsersByMostActiveAsync(tx)));
    }
    
    [HttpPut("username")]
    [Authorize]
    public async Task<IActionResult> UpdateUsername(UpdateUsernameDto updateUsernameDto)
    {
        return await ExecuteWriteAsync(async tx =>
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
        return await ExecuteWriteAsync(async tx =>
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
        return await ExecuteWriteAsync(async tx =>
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
        return await ExecuteWriteAsync(async tx =>
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
        return await ExecuteWriteAsync(async tx =>
        {
            var userId = User.GetUserId();
            await AccountRepository.DeleteUserAsync(tx, userId);
            return NoContent();
        });
    }
}
