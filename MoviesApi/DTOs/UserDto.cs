#nullable disable

using MoviesApi.Models;

namespace MoviesApi.DTOs
{
	public class UserDto
	{
		public string Name { get; set; }
        public Role Role { get; set; }
        public string Token { get; set; }
	}
}
