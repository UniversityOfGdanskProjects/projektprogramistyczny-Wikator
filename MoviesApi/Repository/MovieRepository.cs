using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class MovieRepository(IDriver driver) : Repository(driver), IMovieRepository
{
	public async Task<MovieDto?> AddMovie(AddMovieDto movieDto)
	{
		return await ExecuteAsync(async tx =>
		{
			if (!movieDto.ActorIds.Any())
			{
				const string createMovieQuery = """
				                                CREATE (m:Movie {Title: $Title, Description: $Description})
				                                RETURN m, Id(m) as id
				                                """;

				var movieCursorWithoutActors =
					await tx.RunAsync(createMovieQuery, new { movieDto.Title, movieDto.Description });
				var movieRecordWithoutActors = await movieCursorWithoutActors.SingleAsync();

				var movieNodeWithoutActors = movieRecordWithoutActors["m"].As<INode>();

				return new MovieDto(
					Id: movieRecordWithoutActors["id"].As<int>(),
					Title: movieNodeWithoutActors["Title"].As<string>(),
					Description: movieNodeWithoutActors["Description"].As<string>(),
					0,
					Actors: Enumerable.Empty<ActorDto>()
				);
			}
			
			var createQuery = $$"""
			                    CREATE (m:Movie {Title: $Title, Description: $Description})
			                    WITH m
			                    UNWIND $ActorIds AS actorId
			                    MATCH (a:Actor) WHERE ID(a) = actorId
			                    CREATE (a)-[:{{RelationshipType.PLAYED_IN}}]->(m)
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
			                      Actors: Actors,
			                      AverageReviewScore: 0
			                    } AS MovieWithActors
			                    """;

			var movieCursor = await tx.RunAsync(
				createQuery,
				new { movieDto.Title, movieDto.Description, movieDto.ActorIds }
			);

			var record = await movieCursor.SingleAsync();

			var movieNode = record["MovieWithActors"].As<IDictionary<string, object>>();
			return movieNode.ConvertToMovieDto();
		});
	}

	public async Task<IEnumerable<MovieDto>> GetMovies()
	{
		return await ExecuteAsync(async tx =>
		{
			var query = $$"""
			              MATCH (m:Movie)
			              OPTIONAL MATCH (m)<-[:{{RelationshipType.PLAYED_IN}}]-(a:Actor)
			              OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
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
			              ) AS Actors, AVG(r.score) AS AverageReviewScore
			              RETURN {
			                Id: ID(m),
			                Title: m.Title,
			                Description: m.Description,
			                Actors: Actors,
			                AverageReviewScore: COALESCE(AverageReviewScore, 0)
			              } AS MovieWithActors
			              """;
			var cursor = await tx.RunAsync(query);
			return await cursor.ToListAsync(record =>
			{
				var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
				return movieWithActorsDto.ConvertToMovieDto();
			});
		});
	}
}
