#nullable disable

namespace MoviesApi.Models
{
	public class User
	{
        public int Id { get; set; }

        public string Name { get; set; }

        public Role Role { get; set; }
    }

    public enum Role
    {
		Admin,
		User
	}
}
