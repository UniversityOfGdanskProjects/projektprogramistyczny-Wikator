using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class MovieRepository(IPhotoService photoService, IDriver driver) : Repository(driver), IMovieRepository
{
	private IPhotoService PhotoService { get; } = photoService;
	
	public async Task<IEnumerable<MovieDto>> GetMoviesExcludingIgnored(int userId, MovieQueryParams queryParams)
	{
		return await ExecuteAsync(async tx =>
		{
			// language=Cypher
			const string query = """
			                     MATCH (ignoredMovie:Movie)<-[:IGNORES]-(u:User)
			                     WHERE ID(u) = $userId
			                     WITH COLLECT(ID(ignoredMovie)) AS ignoredMovieIds

			                     MATCH (m:Movie)
			                     WHERE NOT ID(m) IN ignoredMovieIds AND toLower(m.Title) CONTAINS toLower($Title)
			                     	AND ($Actor IS NULL OR EXISTS {
			                     	MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     	WHERE ID(a) = $Actor
			                     	})
			                     OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
			                     WITH m, COLLECT(
			                       CASE
			                         WHEN a IS NULL THEN null
			                         ELSE {
			                           Id: ID(a),
			                           FirstName: a.FirstName,
			                           LastName: a.LastName,
			                           DateOfBirth: a.DateOfBirth,
			                           Biography: a.Biography,
			                           PictureAbsoluteUri: m.PictureAbsoluteUri
			                         }
			                       END
			                     ) AS Actors, AVG(r.score) AS AverageReviewScore
			                     RETURN {
			                       Id: ID(m),
			                       Title: m.Title,
			                       Description: m.Description,
			                       InTheaters: m.InTheaters,
			                       TrailerAbsoluteUri: m.TrailerAbsoluteUri,
			                       PictureAbsoluteUri: m.PictureAbsoluteUri,
			                       ReleaseDate: m.ReleaseDate,
			                       MinimumAge: m.MinimumAge,
			                       Actors: Actors,
			                       AverageReviewScore: COALESCE(AverageReviewScore, 0)
			                     } AS MovieWithActors
			                     """;
			
			var cursor = await tx.RunAsync(query, new {userId, queryParams.Title, queryParams.Actor});
			return await cursor.ToListAsync(record =>
			{
				var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
				return movieWithActorsDto.ConvertToMovieDto();
			});
		});
	}

	public async Task<MovieDto?> AddMovie(AddMovieDto movieDto)
	{
		return await ExecuteAsync(async tx =>
		{
			string? pictureAbsoluteUri = null;
			string? picturePublicId = null;
			
			if (movieDto.FileContent is not null)
			{
				var file = new FormFile(
					new MemoryStream(movieDto.FileContent),
					0,
					movieDto.FileContent.Length,
					"file", movieDto.FileName ?? $"movie-{new Guid()}");

				var uploadResult = await PhotoService.AddPhotoAsync(file);
				if (uploadResult.Error is not null)
					throw new Exception("Image failed to add");

				pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
				picturePublicId = uploadResult.PublicId;
			}
			
			if (!movieDto.ActorIds.Any())
			{
				// language=Cypher
				const string createMovieQuery = """
				                                CREATE (m:Movie {
				                                  Title: $Title,
				                                  Description: $Description,
				                                  PictureAbsoluteUri: $PictureAbsoluteUri,
				                                  PicturePublicId: $PicturePublicId,
				                                  InTheaters: $InTheaters,
				                                  ReleaseDate: $ReleaseDate,
				                                  MinimumAge: $MinimumAge,
				                                  TrailerAbsoluteUri: $TrailerAbsoluteUri
				                                })
				                                RETURN {
				                                  Id: ID(m),
				                                  Title: m.Title,
				                                  Description: m.Description,
				                                  InTheaters: m.InTheaters,
				                                  TrailerAbsoluteUri: m.TrailerAbsoluteUri,
				                                  PictureAbsoluteUri: m.PictureAbsoluteUri,
				                                  ReleaseDate: m.ReleaseDate,
				                                  MinimumAge: m.MinimumAge,
				                                  Actors: [],
				                                  AverageReviewScore: 0
				                                } AS MovieWithActors
				                                """;

				var movieCursorWithoutActors =
					await tx.RunAsync(createMovieQuery, 
						new { movieDto.Title, movieDto.Description, movieDto.ActorIds, PictureAbsoluteUri = pictureAbsoluteUri,
							PicturePublicId = picturePublicId, movieDto.InTheaters, movieDto.ReleaseDate, movieDto.MinimumAge,
							TrailerAbsoluteUri = movieDto.TrailerUrl });
				
				var movieRecordWithoutActors = await movieCursorWithoutActors.SingleAsync();
				var movieNodeWithoutActors = movieRecordWithoutActors["MovieWithActors"].As<IDictionary<string, object>>();

				return movieNodeWithoutActors.ConvertToMovieDto();
			}
			
			// language=Cypher
			const string createQuery = """
			                           CREATE (m:Movie {
			                             Title: $Title,
			                             Description: $Description,
			                             PictureAbsoluteUri: $PictureAbsoluteUri,
			                             PicturePublicId: $PicturePublicId,
			                             InTheaters: $InTheaters,
			                             ReleaseDate: $ReleaseDate,
			                             MinimumAge: $MinimumAge,
			                             TrailerAbsoluteUri: $TrailerAbsoluteUri
			                           })
			                           WITH m
			                           UNWIND $ActorIds AS actorId
			                           MATCH (a:Actor) WHERE ID(a) = actorId
			                           CREATE (a)-[:PLAYED_IN]->(m)
			                           WITH m, COLLECT(
			                             CASE
			                               WHEN a IS NULL THEN null
			                               ELSE {
			                                 Id: ID(a),
			                                 FirstName: a.FirstName,
			                                 LastName: a.LastName,
			                                 DateOfBirth: a.DateOfBirth,
			                                 Biography: a.Biography,
			                                 PictureAbsoluteUri: a.PictureAbsoluteUri
			                               }
			                             END
			                           ) AS Actors
			                           RETURN {
			                             Id: ID(m),
			                             Title: m.Title,
			                             Description: m.Description,
			                             InTheaters: m.InTheaters,
			                             TrailerAbsoluteUri: m.TrailerAbsoluteUri,
			                             PictureAbsoluteUri: m.PictureAbsoluteUri,
			                             ReleaseDate: m.ReleaseDate,
			                             MinimumAge: m.MinimumAge,
			                             Actors: Actors,
			                             AverageReviewScore: 0
			                           } AS MovieWithActors
			                           """;

			var movieCursor = await tx.RunAsync(
				createQuery,
				new { movieDto.Title, movieDto.Description, movieDto.ActorIds, PictureAbsoluteUri = pictureAbsoluteUri,
					PicturePublicId = picturePublicId, movieDto.InTheaters, movieDto.ReleaseDate, movieDto.MinimumAge,
					TrailerAbsoluteUri = movieDto.TrailerUrl }
			);

			var record = await movieCursor.SingleAsync();

			var movieNode = record["MovieWithActors"].As<IDictionary<string, object>>();
			return movieNode.ConvertToMovieDto();
		});
	}

	public async Task<QueryResult> DeleteMovie(int movieId)
	{
		return await ExecuteAsync(async tx =>
		{
			// language=cypher
			const string movieExistsAsync = """
			                                  MATCH (m:Movie)
			                                  WHERE ID(m) = $movieId
			                                  RETURN m.PicturePublicId AS PicturePublicId
			                                """;

			var cursor = await tx.RunAsync(movieExistsAsync, new { movieId });

			try
			{
				var movie = await cursor.SingleAsync();
				var publicId = movie["PicturePublicId"].As<string?>();

				if (publicId is not null && (await PhotoService.DeleteASync(publicId)).Error is not null)
					return QueryResult.PhotoFailedToDelete;

				// language=cypher
				await tx.RunAsync("MATCH (m:Movie) WHERE Id(m) = $movieId DETACH DELETE m", new { movieId });
				return QueryResult.Completed;
			}
			catch
			{
				return QueryResult.NotFound;
			}
		});
	}

	public async Task<IEnumerable<MovieDto>> GetMovies(MovieQueryParams queryParams)
	{
		return await ExecuteAsync(async tx =>
		{
			// language=Cypher
			const string query = """

			                     MATCH (m:Movie)
			                     WHERE toLower(m.Title) CONTAINS toLower($Title)
			                     	AND $Actor IS NULL OR EXISTS {
			                     		MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     		WHERE ID(a) = $Actor
			                     	}
			                     OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
			                     WITH m, COLLECT(
			                       CASE
			                         WHEN a IS NULL THEN null
			                         ELSE {
			                           Id: ID(a),
			                           FirstName: a.FirstName,
			                           LastName: a.LastName,
			                           DateOfBirth: a.DateOfBirth,
			                           Biography: a.Biography,
			                           PictureAbsoluteUri: a.PictureAbsoluteUri
			                         }
			                       END
			                     ) AS Actors, AVG(r.score) AS AverageReviewScore
			                     RETURN {
			                       Id: ID(m),
			                       Title: m.Title,
			                       Description: m.Description,
			                       InTheaters: m.InTheaters,
			                       TrailerAbsoluteUri: m.TrailerAbsoluteUri,
			                       PictureAbsoluteUri: m.PictureAbsoluteUri,
			                       ReleaseDate: m.ReleaseDate,
			                       MinimumAge: m.MinimumAge,
			                       Actors: Actors,
			                       AverageReviewScore: COALESCE(AverageReviewScore, 0)
			                     } AS MovieWithActors
			                     """;
			
			var cursor = await tx.RunAsync(query, queryParams);
			return await cursor.ToListAsync(record =>
			{
				var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
				return movieWithActorsDto.ConvertToMovieDto();
			});
		});
	}
}
