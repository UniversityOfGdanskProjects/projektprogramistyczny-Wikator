using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Models;
using Neo4j.Driver;

namespace MoviesApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AccountController : ControllerBase
	{
		private readonly ITokenService _tokenService;
		private readonly IDriver _driver;

		public AccountController(ITokenService tokenService, IDriver driver)
		{
			_tokenService = tokenService;
			_driver = driver;
		}

		[HttpPost("login")]
		public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
		{
			User? user = null;
			IAsyncSession session = _driver.AsyncSession();
			try
			{
				IResultCursor cursor = await session.RunAsync($"MATCH (a:User {{ Name: \"{loginDto.Name}\" }}) RETURN a.Name as name, a.Role as role, a.Password as password, ID(a) as id");
				IRecord node = await cursor.SingleAsync();
				if (loginDto.Password == node["password"].As<string>())
				{
					user = new User
					{
						Id = node["id"].As<int>(),
						Name = node["name"].As<string>(),
						Role = (Role)Enum.Parse(typeof(Role), node["role"].As<string>())
					};
				}
			}
			finally
			{
				await session.CloseAsync();
			}
			
			if (user is null)
				return Unauthorized("Invalid username or password");

			return new UserDto
			{
				Name = user.Name,
				Role = user.Role,
				Token = _tokenService.CreateToken(user)
			};
		}

		[HttpPost("register")]
		public async Task<ActionResult<UserDto>> Register(LoginDto loginDto)
		{
			IAsyncSession session = _driver.AsyncSession();
			try
			{
				IResultCursor cursor = await session.RunAsync($"CREATE (a:User {{ Name: \"{loginDto.Name}\", Password: \"{loginDto.Password}\", Role: \"User\" }})");
				return new UserDto
				{
					Name = loginDto.Name,
					Role = Role.User
				};
			}
			finally
			{
				await session.CloseAsync();
			}
		}

		[Authorize]
		[HttpGet("test")]
		public string Test()
		{
			return "Hello from test";
		}
	}
}
