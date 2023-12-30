using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(ITokenService tokenService, IAccountRepository accountRepository) : ControllerBase
{
	private ITokenService TokenService { get; } = tokenService;
	private IAccountRepository AccountRepository { get; } = accountRepository;
		
		
	[HttpPost("login")]
	public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
	{
		var user = await AccountRepository.LoginASync(loginDto);

		return user switch
		{
			null => Unauthorized("Invalid username or password"),
			_ => new UserDto
			{
				Name = user.Name,
				Token = TokenService.CreateToken(user)
			}
		};
	}

	[HttpPost("register")]
	public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
	{
		if (await AccountRepository.EmailExistsAsync(registerDto.Email))
			return BadRequest("Email is taken");
		
		var user = await AccountRepository.RegisterAsync(registerDto);

		return user switch
		{
			null => BadRequest("There was an error when creating new user"),
			_ => new UserDto
			{
				Name = user.Name,
				Token = TokenService.CreateToken(user)
			}
		};
	}

	[Authorize]
	[HttpGet("test")]
	public string Test()
	{
		return "Hello from test";
	}
}
