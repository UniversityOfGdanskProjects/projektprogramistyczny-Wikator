namespace MoviesApi.Models;

public class Movie
{
	public int Id { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
        
	public ICollection<Actor>? Actors { get; set; }
}