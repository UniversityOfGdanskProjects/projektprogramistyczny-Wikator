using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Models;
using Neo4j.Driver;
using System.Security.Cryptography;
using System.Text;

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
			IAsyncSession session = _driver.AsyncSession();
			try
			{
				string query = $$"""
				                    MATCH (a:User { Name: "{{loginDto.Name}}" })
				                    RETURN a.Name as name, a.Role as role, a.PasswordHash as passwordHash, a.PasswordSalt as passwordSalt, ID(a) as id
				                 """;
				IResultCursor cursor = await session.RunAsync(query);
				IRecord node = await cursor.SingleAsync();

				if (node is null)
					return Unauthorized("Invalid username or password");
				
				string storedPasswordHash = node["passwordHash"].As<string>();
				string storedPasswordSalt = node["passwordSalt"].As<string>();

				using HMACSHA512 hmac = new(Convert.FromBase64String(storedPasswordSalt));
				byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
				byte[] storedHash = Convert.FromBase64String(storedPasswordHash);

				if (computedHash.Where((t, i) => t != storedHash[i]).Any())
					return Unauthorized("Invalid password");
				
				User? user = new User
				{
					Id = node["id"].As<int>(),
					Name = node["name"].As<string>(),
					Role = (Role)Enum.Parse(typeof(Role), node["role"].As<string>())
				};
				
				return new UserDto
				{
					Name = user.Name,
					Role = user.Role,
					Token = _tokenService.CreateToken(user)
				};
				
			}
			finally
			{
				await session.CloseAsync();
			}
		}

		[HttpPost("register")]
		public async Task<ActionResult<UserDto>> Register(LoginDto loginDto)
		{
			using HMACSHA512 hmac = new();
			byte[] passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
			byte[] passwordSalt = hmac.Key;

			IAsyncSession session = _driver.AsyncSession();

			try
			{
				var query = $@"
					        CREATE (a:User {{
					            Name: ""{loginDto.Name}"",
					            PasswordHash: ""{Convert.ToBase64String(passwordHash)}"",
					            PasswordSalt: ""{Convert.ToBase64String(passwordSalt)}"",
					            Role: ""User""
					        }})";
        
				await session.RunAsync(query);

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
