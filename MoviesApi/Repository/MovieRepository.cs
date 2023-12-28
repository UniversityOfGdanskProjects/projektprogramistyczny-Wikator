using MoviesApi.DTOs;
using MoviesApi.Models;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class MovieRepository(IDriver driver) : Repository(driver), IMovieRepository
{
	public async Task<Movie?> AddMovie(AddMovieDto movieDto)
	{
		var session = Driver.AsyncSession();

		try
		{
			return await session.ExecuteWriteAsync(async tx =>
			{
				var movie = await CreateAndReturnMovie(tx, movieDto);
				
				if (movie is null)
					return null;
				
				movie.Actors = await CreateRelationshipsAndReturnActors(tx, movieDto, movie);
				return movie;
			});
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return null;
		}
		finally
		{
			await session.CloseAsync();
		}
	}

	public async Task<List<Movie>> GetMovies()
	{
		List<Movie> movies;
		var session = Driver.AsyncSession();
		try
		{
			movies = await session.ExecuteReadAsync(MatchAndReturnMovies);
		}
		finally
		{
			await session.CloseAsync();
		}
		return movies;
	}

	private static async Task<List<Movie>> MatchAndReturnMovies(IAsyncQueryRunner tx)
	{
		const string query = """
		                     MATCH (a:Movie)
		                     RETURN a.Title as title, a.Description as description
		                     LIMIT 10
		                     """;
		var cursor = await tx.RunAsync(query);
		return await cursor.ToListAsync(record => new Movie
		{
			Title = record["title"].As<string>(),
			Description = record["description"].As<string>()
		});
	}

	private static async Task<Movie?> CreateAndReturnMovie(IAsyncQueryRunner tx, AddMovieDto movieDto)
	{
		var movieQuery = $"" +
		                 $"CREATE (a:Movie {{ Title: \"{movieDto.Title}\", Description: \"{movieDto.Description}\" }})" +
		                 $"RETURN Id(a) as id, a.Title as title, a.Description as description";
		var movieCursor = await tx.RunAsync(movieQuery);
		var movieNode = await movieCursor.SingleAsync();

		if (movieNode is null)
			return null;

		return new Movie
		{
			Id = movieNode["id"].As<int>(),
			Title = movieNode["title"].As<string>(),
			Description = movieNode["description"].As<string>()
		};
	}
	
	private static async Task<List<Actor>> CreateRelationshipsAndReturnActors(IAsyncQueryRunner tx,
		AddMovieDto movieDto, Movie movie)
	{
		var actorIds = '[' + string.Join(", ", movieDto.ActorIds) + ']';
		var relationshipQuery = $"MATCH (a:Movie), (b:Actor) WHERE Id(a) = {movie.Id} AND Id(b) IN {actorIds} " +
		                        $"CREATE (b)-[r:{RelationshipType.PLAYED_IN}]->(a) " +
		                        $"RETURN b.FirstName as firstName, b.LastName as lastName, b.DateOfBirth as dateOfBirth, b.Biography as biography";
		var relationshipCursor = await tx.RunAsync(relationshipQuery);
		return await relationshipCursor.ToListAsync(record => new Actor
		{
			FirstName = record["firstName"].As<string>(),
			LastName = record["lastName"].As<string>(),
			DateOfBirth = record["dateOfBirth"].As<string>(),
			Biography = record["biography"].As<string>()
		});
	}
}
