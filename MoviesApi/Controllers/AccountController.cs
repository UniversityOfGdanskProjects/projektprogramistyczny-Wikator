using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Models;
using Neo4j.Driver;
using System.Security.Cryptography;
using System.Text;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(ITokenService tokenService, IDriver driver) : ControllerBase
{
	private ITokenService TokenService { get; } = tokenService;
	private IDriver Driver { get; } = driver;
		
		
	[HttpPost("login")]
	public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
	{
		var session = Driver.AsyncSession();
		try
		{
			var query = $$"""
			                 MATCH (a:User { Name: "{{loginDto.Name}}" })
			                 RETURN a.Name as name, a.Role as role, a.PasswordHash as passwordHash, a.PasswordSalt as passwordSalt, ID(a) as id
			              """;
			var cursor = await session.RunAsync(query);
			var node = await cursor.SingleAsync();

			if (node is null)
				return Unauthorized("Invalid username or password");
				
			var storedPasswordHash = node["passwordHash"].As<string>();
			var storedPasswordSalt = node["passwordSalt"].As<string>();

			Console.WriteLine();
			using HMACSHA512 hmac = new(Convert.FromBase64String(storedPasswordSalt));
			var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
			var storedHash = Convert.FromBase64String(storedPasswordHash);

			if (computedHash.Where((t, i) => t != storedHash[i]).Any())
				return Unauthorized("Invalid username password");
				
			var user = new User
			{
				Id = node["id"].As<int>(),
				Name = node["name"].As<string>(),
				Role = (Role)Enum.Parse(typeof(Role), node["role"].As<string>())
			};
				
			return new UserDto
			{
				Name = user.Name,
				Role = user.Role,
				Token = TokenService.CreateToken(user)
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
		var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
		var passwordSalt = hmac.Key;

		var session = Driver.AsyncSession();

		try
		{
			var query = $$"""
			                    CREATE (a:User {
			                        Name: "{{loginDto.Name}}",
			                        PasswordHash: "{{Convert.ToBase64String(passwordHash)}}",
			                        PasswordSalt: "{{Convert.ToBase64String(passwordSalt)}}",
			                        Role: "User"
			                    })
			                    RETURN a.Name as name, a.Role as role, ID(a) as id
			              """;
        
			var cursor = await session.RunAsync(query);
			var node = await cursor.SingleAsync();
				
			var user = new User 
			{
				Id = node["id"].As<int>(),
				Name = node["name"].As<string>(),
				Role = (Role)Enum.Parse(typeof(Role), node["role"].As<string>())
			};

			return new UserDto
			{
				Name = user.Name,
				Role = user.Role,
				Token = TokenService.CreateToken(user)
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