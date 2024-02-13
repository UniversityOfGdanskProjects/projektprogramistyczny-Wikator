using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;

namespace MoviesService.Api.Controllers;

[Route("api/[controller]")]
public class AccountController(
    IAsyncQueryExecutor queryExecutor,
    ITokenService tokenService,
    IAccountRepository accountRepository,
    IMqttService mqttService,
    IUserClaimsProvider claimsProvider) : BaseApiController(queryExecutor)
{
    private ITokenService TokenService { get; } = tokenService;
    private IAccountRepository AccountRepository { get; } = accountRepository;
    private IMqttService MqttService { get; } = mqttService;
    private IUserClaimsProvider ClaimsProvider { get; } = claimsProvider;


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var user = await AccountRepository.LoginAsync(tx, loginDto);

            return user switch
            {
                null => Unauthorized("Invalid username or password"),
                _ => Ok(new UserDto(user.Id, user.Name, user.Role, TokenService.CreateToken(user)))
            };
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (await AccountRepository.EmailExistsAsync(tx, registerDto.Email))
                return BadRequest("Email is taken");

            var user = await AccountRepository.RegisterAsync(tx, registerDto);
            _ = MqttService.SendNotificationAsync("users/new-today", "New user today!");
            return Ok(new UserDto(user.Id, user.Name, user.Role, TokenService.CreateToken(user)));
        });
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = ClaimsProvider.GetUserId(User);
            await AccountRepository.DeleteUserAsync(tx, userId);
            return NoContent();
        });
    }
}