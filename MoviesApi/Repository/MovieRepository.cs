using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
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
	
	public async Task<IEnumerable<MovieDto>> GetMoviesExcludingIgnored(Guid userId, MovieQueryParams queryParams)
	{
		return await ExecuteReadAsync(async tx =>
		{
			// language=Cypher
			const string query = """
			                     MATCH (ignoredMovie:Movie)<-[:IGNORES]-(u:User { Id: $userId })
			                     WITH COLLECT(ignoredMovie.Id) AS ignoredMovieIds

			                     MATCH (m:Movie)
			                     WHERE NOT m.Id IN ignoredMovieIds AND toLower(m.Title) CONTAINS toLower($Title)
			                     	AND ($Actor IS NULL OR $Actor = "" OR EXISTS {
			                     	MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     	WHERE a.Id = $Actor
			                     	})
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
			                     WITH m, AVG(r.score) AS AverageReviewScore
			                     RETURN {
			                       Id: m.Id,
			                       Title: m.Title,
			                       PictureAbsoluteUri: m.PictureAbsoluteUri,
			                       MinimumAge: m.MinimumAge,
			                       AverageReviewScore: COALESCE(AverageReviewScore, 0)
			                     } AS Movies
			                     ORDER BY
			                     CASE WHEN $SortOrder = "Ascending" THEN CASE WHEN $SortBy IS NULL OR $SortBy = "Popularity" THEN m.Popularity ELSE Movies[$SortBy] END ELSE null END ASC,
			                     CASE WHEN $SortOrder = "Ascending" THEN null ELSE CASE WHEN $SortBy IS NULL OR $SortBy = "Popularity" THEN m.Popularity ELSE Movies[$SortBy] END END DESC
			                     """;
			
			var cursor = await tx.RunAsync(query,
				new
				{
					userId = userId.ToString(), queryParams.Title, Actor = queryParams.Actor.ToString(),
					queryParams.SortBy, SortOrder = queryParams.SortOrder.ToString()
				});
			
			return await cursor.ToListAsync(record =>
			{
				var movieWithActorsDto = record["Movies"].As<IDictionary<string, object>>();
				return movieWithActorsDto.ConvertToMovieDto();
			});
		});
	}

	public async Task<QueryResult<MovieDetailsDto>> AddMovie(AddMovieDto movieDto)
	{
		return await ExecuteWriteAsync(async tx =>
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
					return new QueryResult<MovieDetailsDto>(QueryResultStatus.PhotoFailedToSave, null);

				pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
				picturePublicId = uploadResult.PublicId;
			}
			
			if (!movieDto.ActorIds.Any())
			{
				// language=Cypher
				const string createMovieQuery = """
				                                CREATE (m:Movie {
				                                  Id: randomUUID(),
				                                  Title: $Title,
				                                  Description: $Description,
				                                  PictureAbsoluteUri: $PictureAbsoluteUri,
				                                  PicturePublicId: $PicturePublicId,
				                                  InTheaters: $InTheaters,
				                                  ReleaseDate: $ReleaseDate,
				                                  MinimumAge: $MinimumAge,
				                                  TrailerAbsoluteUri: $TrailerAbsoluteUri,
				                                  Popularity: 0
				                                })
				                                RETURN {
				                                  Id: m.Id,
				                                  Title: m.Title,
				                                  Description: m.Description,
				                                  InTheaters: m.InTheaters,
				                                  TrailerAbsoluteUri: m.TrailerAbsoluteUri,
				                                  PictureAbsoluteUri: m.PictureAbsoluteUri,
				                                  ReleaseDate: m.ReleaseDate,
				                                  MinimumAge: m.MinimumAge,
				                                  Actors: [],
				                                  Comments: [],
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

				return new QueryResult<MovieDetailsDto>(QueryResultStatus.Completed, movieNodeWithoutActors.ConvertToMovieDetailsDto());
			}
			
			// language=Cypher
			const string createQuery = """
			                           CREATE (m:Movie {
			                             Id: randomUUID(),
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
			                           MATCH (a:Actor { Id: actorId })
			                           CREATE (a)-[:PLAYED_IN]->(m)
			                           WITH m, COLLECT(
			                             CASE
			                               WHEN a IS NULL THEN null
			                               ELSE {
			                                 Id: a.Id,
			                                 FirstName: a.FirstName,
			                                 LastName: a.LastName,
			                                 DateOfBirth: a.DateOfBirth,
			                                 Biography: a.Biography,
			                                 PictureAbsoluteUri: a.PictureAbsoluteUri
			                               }
			                             END
			                           ) AS Actors
			                           RETURN {
			                             Id: m.Id,
			                             Title: m.Title,
			                             Description: m.Description,
			                             InTheaters: m.InTheaters,
			                             TrailerAbsoluteUri: m.TrailerAbsoluteUri,
			                             PictureAbsoluteUri: m.PictureAbsoluteUri,
			                             ReleaseDate: m.ReleaseDate,
			                             MinimumAge: m.MinimumAge,
			                             Actors: Actors,
			                             Comments: [],
			                             AverageReviewScore: 0
			                           } AS MovieWithActors
			                           """;

			var movieCursor = await tx.RunAsync(
				createQuery,
				new { movieDto.Title, movieDto.Description, ActorIds = movieDto.ActorIds.Select(a => a.ToString()), PictureAbsoluteUri = pictureAbsoluteUri,
					PicturePublicId = picturePublicId, movieDto.InTheaters, movieDto.ReleaseDate, movieDto.MinimumAge,
					TrailerAbsoluteUri = movieDto.TrailerUrl }
			);

			var record = await movieCursor.SingleAsync();

			var movieNode = record["MovieWithActors"].As<IDictionary<string, object>>();
			return new QueryResult<MovieDetailsDto>(QueryResultStatus.Completed, movieNode.ConvertToMovieDetailsDto());
		});
	}

	public async Task<QueryResult> DeleteMovie(Guid movieId)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			// language=cypher
			const string movieExistsAsync = """
			                                  MATCH (m:Movie { Id: $movieId })
			                                  RETURN m.PicturePublicId AS PicturePublicId
			                                """;

			var cursor = await tx.RunAsync(movieExistsAsync, new { movieId = movieId.ToString() });

			try
			{
				var publicId = await cursor.SingleAsync(record => record["PicturePublicId"].As<string?>());
				
				if (publicId is not null && (await PhotoService.DeleteASync(publicId)).Error is not null)
					return new QueryResult(QueryResultStatus.PhotoFailedToDelete);

				// language=cypher
				await tx.RunAsync("MATCH (m:Movie { Id: $movieId }) DETACH DELETE m",
					new { movieId = movieId.ToString() });
				
				return new QueryResult(QueryResultStatus.Completed);
			}
			catch
			{
				return new QueryResult(QueryResultStatus.NotFound);
			}
		});
	}

	public async Task<bool> MovieExists(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=Cypher
		const string query = """
		                                     MATCH (m:Movie {Id: $movieId})
		                                     WITH COUNT(m) > 0 as movieExists
		                                     RETURN movieExists
		                                     """;

		var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString() });
		return await cursor.SingleAsync(record => record["movieExists"].As<bool>());
	}

	public async Task<MovieDetailsDto?> GetMovieDetails(Guid movieId)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			// language=Cypher
			const string query = """
			                     MATCH (m:Movie { Id: $movieId })
			                     OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
			                     OPTIONAL MATCH (m)<-[c:COMMENTED]-(u2:User)
			                     SET m.Popularity = m.Popularity + 1
			                     WITH m, COLLECT(
			                       CASE
			                         WHEN a IS NULL THEN null
			                         ELSE {
			                           Id: a.Id,
			                           FirstName: a.FirstName,
			                           LastName: a.LastName,
			                           DateOfBirth: a.DateOfBirth,
			                           Biography: a.Biography,
			                           PictureAbsoluteUri: a.PictureAbsoluteUri
			                         }
			                       END
			                     ) AS Actors, 
			                     COLLECT(
			                       CASE
			                         WHEN u2 is NULL OR c is NULL THEN null
			                         ELSE {
			                           Id: c.Id,
			                           MovieId: m.Id,
			                           UserId: u2.Id,
			                           Username: u2.Name,
			                           Text: c.Text,
			                           CreatedAt: c.CreatedAt,
			                           IsEdited: c.IsEdited
			                         }
			                       END
			                     ) AS Comments, AVG(r.score) AS AverageReviewScore
			                     RETURN {
			                       Id: m.Id,
			                       Title: m.Title,
			                       Description: m.Description,
			                       InTheaters: m.InTheaters,
			                       TrailerAbsoluteUri: m.TrailerAbsoluteUri,
			                       PictureAbsoluteUri: m.PictureAbsoluteUri,
			                       ReleaseDate: m.ReleaseDate,
			                       MinimumAge: m.MinimumAge,
			                       Actors: Actors,
			                       Comments: Comments,
			                       AverageReviewScore: COALESCE(AverageReviewScore, 0)
			                     } AS MovieWithActors
			                     """;
			
			var cursor = await tx.RunAsync(query, new
			{  movieId = movieId.ToString() });
			return await cursor.SingleAsync(record =>
				record["MovieWithActors"].As<IDictionary<string, object>>().ConvertToMovieDetailsDto());
		});
	}

	public async Task<IEnumerable<MovieDto>> GetMovies(MovieQueryParams queryParams)
	{
		return await ExecuteReadAsync(async tx =>
		{
			// language=Cypher
			const string query = """
			                     MATCH (m:Movie)
			                     WHERE toLower(m.Title) CONTAINS toLower($Title)
			                     	AND $Actor IS NULL OR $Actor = "" OR EXISTS {
			                     		MATCH (m)<-[:PLAYED_IN]-(a:Actor { Id: $Actor })
			                     	}
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
			                     WITH m, AVG(r.score) AS AverageReviewScore
			                     RETURN {
			                       Id: m.Id,
			                       Title: m.Title,
			                       PictureAbsoluteUri: m.PictureAbsoluteUri,
			                       MinimumAge: m.MinimumAge,
			                       AverageReviewScore: COALESCE(AverageReviewScore, 0)
			                     } AS Movies
			                     ORDER BY
			                     CASE WHEN $SortOrder = "Ascending" THEN CASE WHEN $SortBy IS NULL OR $SortBy = "Popularity" THEN m.Popularity ELSE Movies[$SortBy] END ELSE null END ASC,
			                     CASE WHEN $SortOrder = "Ascending" THEN null ELSE CASE WHEN $SortBy IS NULL OR $SortBy = "Popularity" THEN m.Popularity ELSE Movies[$SortBy] END END DESC
			                     """;
			
			var cursor = await tx.RunAsync(query, new
			{
				queryParams.Title, Actor = queryParams.Actor.ToString(),
				queryParams.SortBy, SortOrder = queryParams.SortOrder.ToString()
			});
			return await cursor.ToListAsync(record =>
			{
				var movieWithActorsDto = record["Movies"].As<IDictionary<string, object>>();
				return movieWithActorsDto.ConvertToMovieDto();
			});
		});
	}
}
