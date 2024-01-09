using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

public class AccountController(ITokenService tokenService, IAccountRepository accountRepository) : BaseApiController
{
	private ITokenService TokenService { get; } = tokenService;
	private IAccountRepository AccountRepository { get; } = accountRepository;
		
	
	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginDto loginDto)
	{
		if (!await AccountRepository.EmailExistsAsync(loginDto.Email))
			return Unauthorized("Invalid username or password");
		
		var user = await AccountRepository.LoginAsync(loginDto);

		return user switch
		{
			null => Unauthorized("Invalid username or password"),
			_ => Ok(new UserDto(user.Id, user.Name, user.Role, TokenService.CreateToken(user)))
		};
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register(RegisterDto registerDto)
	{
		if (await AccountRepository.EmailExistsAsync(registerDto.Email))
			return BadRequest("Email is taken");
		
		var user = await AccountRepository.RegisterAsync(registerDto);

		return user switch
		{
			null => BadRequest("There was an error when creating new user"),
			_ => Ok(new UserDto(user.Id, user.Name, user.Role, TokenService.CreateToken(user)))
		};
	}
}
