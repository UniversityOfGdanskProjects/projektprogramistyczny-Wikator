namespace MoviesApi.Models
{
	public class User
	{
        public int Id { get; init; }
		public required string Email { get; init; }
        public required string Name { get; init; }

        public Role Role { get; init; }
    }

    public enum Role
    {
		Admin,
		User
	}
}
