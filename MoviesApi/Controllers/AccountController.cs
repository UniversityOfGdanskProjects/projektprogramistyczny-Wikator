using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
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
		return await ExecuteReadAsync<IActionResult>(async tx =>
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
		return await ExecuteWriteAsync<IActionResult>(async tx =>
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
}
