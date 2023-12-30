using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class MovieRepository(IDriver driver) : IMovieRepository
{
	private IDriver Driver { get; } = driver;
		
	public async Task<MovieDto?> AddMovie(AddMovieDto movieDto)
	{
		var session = Driver.AsyncSession();

		try
		{
			return await session.ExecuteWriteAsync(async tx =>
				await CreateAndReturnMovie(tx, movieDto));
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

	public async Task<IEnumerable<MovieDto>> GetMovies()
	{
		IEnumerable<MovieDto> movies;
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

	private static async Task<IEnumerable<MovieDto>> MatchAndReturnMovies(IAsyncQueryRunner tx)
	{
		var query = $$"""
		                     MATCH (m:Movie)
		                     OPTIONAL MATCH (m)<-[:{{RelationshipType.PLAYED_IN}}]-(a:Actor)
		                     WITH m, COLLECT(
		                         CASE
		                             WHEN a IS NULL THEN null
		                             ELSE {
		                                 Id: ID(a),
		                                 FirstName: a.FirstName,
		                                 LastName: a.LastName,
		                                 DateOfBirth: a.DateOfBirth,
		                                 Biography: a.Biography
		                             }
		                         END
		                     ) AS Actors
		                     RETURN {
		                         Id: ID(m),
		                         Title: m.Title,
		                         Description: m.Description,
		                         Actors: Actors
		                     } AS MovieWithActors
		                     """;
		var cursor = await tx.RunAsync(query);
		await cursor.FetchAsync();
		return await cursor.ToListAsync(record =>
		{
			var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
			var actors = movieWithActorsDto["Actors"].As<List<IDictionary<string, object>>>();

			return new MovieDto(
				movieWithActorsDto["Id"].As<int>(),
				movieWithActorsDto["Title"].As<string>(),
				movieWithActorsDto["Description"].As<string>(),
				actors.Select(actor => MapActorDto(actor))
			);
		});
	}	

	private static async Task<MovieDto?> CreateAndReturnMovie(IAsyncQueryRunner tx, AddMovieDto movieDto)
	{
		if (!movieDto.ActorIds.Any())
		{
			const string createMovieQuery = """
			                                CREATE (m:Movie {Title: $Title, Description: $Description})
			                                RETURN m, Id(m) as id
			                                """;

			var movieCursorWithoutActors = await tx.RunAsync(createMovieQuery, new { movieDto.Title, movieDto.Description });
			var movieRecordWithoutActors = await movieCursorWithoutActors.SingleAsync();
        
			var movieNodeWithoutActors = movieRecordWithoutActors["m"].As<INode>();

			return new MovieDto(
				Id: movieRecordWithoutActors["id"].As<int>(),
				Title: movieNodeWithoutActors["Title"].As<string>(),
				Description: movieNodeWithoutActors["Description"].As<string>(),
				Actors: Enumerable.Empty<ActorDto>()
			);
		}
		
		
		var createQuery = $$"""
		                           CREATE (m:Movie {Title: $Title, Description: $Description})
		                           WITH m
		                           UNWIND $ActorIds AS actorId
		                           MATCH (a:Actor) WHERE ID(a) = actorId
		                           CREATE (a)-[:{{RelationshipType.PLAYED_IN}}]->(m)
		                           RETURN m, Id(m) as id, COLLECT({ Id: ID(a), FirstName: a.FirstName, LastName: a.LastName, DateOfBirth: a.DateOfBirth, Biography: a.Biography }) AS actors
		                           """;
		
		var movieCursor = await tx.RunAsync(
			createQuery,
			new { movieDto.Title, movieDto.Description, movieDto.ActorIds }
		);
		
		var record = await movieCursor.SingleAsync();
		
		var movieNode = record["m"].As<INode>();
		var actors = record["actors"].As<List<IDictionary<string, object>>>();

		return new MovieDto(
			Id: record["id"].As<int>(),
			Title: movieNode["Title"].As<string>(),
			Description: movieNode["Description"].As<string>(),
			Actors: actors.Select(actor => MapActorDto(actor))
		);
	}
	
	private static ActorDto MapActorDto(IDictionary<string, object> actorData)
	{
		return new ActorDto(
			actorData["Id"].As<int>(),
			actorData["FirstName"].As<string>(),
			actorData["LastName"].As<string>(),
			actorData["DateOfBirth"].As<string>(),
			actorData["Biography"]?.As<string>()
		);
	}
}
