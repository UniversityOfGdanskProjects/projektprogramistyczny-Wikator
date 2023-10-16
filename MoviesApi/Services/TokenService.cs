using Microsoft.IdentityModel.Tokens;
using MoviesApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MoviesApi.Services
{
	public class TokenService : ITokenService
	{
		private readonly SymmetricSecurityKey _key;

		public TokenService(IConfiguration config)
		{
			string tokenKey = config["TokenKey"]
				?? throw new Exception("Token key not found");

			_key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
		}

		public string CreateToken(User user)
		{
			List<Claim> claims = new()
			{
				new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.UniqueName, user.Name),
				new Claim(ClaimTypes.Role, user.Role.ToString())
			};

			SigningCredentials creds = new(_key, SecurityAlgorithms.HmacSha512Signature);

			SecurityTokenDescriptor tokenDescriptor = new()
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddDays(7),
				SigningCredentials = creds,
				Issuer = "localhost",
			};

			JwtSecurityTokenHandler tokenHandler = new();
			SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}
	}
}
