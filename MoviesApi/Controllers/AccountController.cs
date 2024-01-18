using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
public class AccountController(IDriver driver, ITokenService tokenService, IAccountRepository accountRepository)
	: BaseApiController(driver)
{
	private ITokenService TokenService { get; } = tokenService;
	private IAccountRepository AccountRepository { get; } = accountRepository;
		
	
	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginDto loginDto)
	{
		return await ExecuteReadAsync(async tx =>
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
		return await ExecuteWriteAsync(async tx =>
		{
			if (await AccountRepository.EmailExistsAsync(tx, registerDto.Email))
				return BadRequest("Email is taken");
		
			var user = await AccountRepository.RegisterAsync(tx, registerDto);

			return user switch
			{
				null => BadRequest("There was an error when creating new user"),
				_ => Ok(new UserDto(user.Id, user.Name, user.Role, TokenService.CreateToken(user)))
			};
		});
	}
	
	[Authorize]
	[HttpDelete]
	public async Task<IActionResult> DeleteAccount()
	{
		return await ExecuteWriteAsync(async tx =>
		{
			var userId = User.GetUserId();
			await AccountRepository.DeleteUserAsync(tx, userId);
			return NoContent();
		});
	}
}
