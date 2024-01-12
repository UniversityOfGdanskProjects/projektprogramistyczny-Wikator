using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class MovieRepository : IMovieRepository
{
	public async Task<PagedList<MovieDto>> GetMoviesExcludingIgnored(IAsyncQueryRunner tx,Guid userId, MovieQueryParams queryParams)
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
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { Id: $userId })
		                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { Id: $userId })
		                     WITH m, COALESCE(AVG(r.Score), 0) AS AverageReviewScore, COUNT(w) > 0 AS OnWatchlist, COUNT(f) > 0 AS IsFavorite
		                     ORDER BY
		                     CASE WHEN $SortOrder = "Ascending" THEN CASE WHEN $SortBy = "AverageReviewScore" THEN AverageReviewScore ELSE m[$SortBy] END ELSE null END ASC,
		                     CASE WHEN $SortOrder = "Descending" THEN CASE WHEN $SortBy = "AverageReviewScore" THEN AverageReviewScore ELSE m[$SortBy] END ELSE null END DESC
		                     RETURN {
		                       Id: m.Id,
		                       Title: m.Title,
		                       PictureAbsoluteUri: m.PictureAbsoluteUri,
		                       MinimumAge: m.MinimumAge,
		                       OnWatchlist: OnWatchlist,
		                       IsFavorite: IsFavorite,
		                       AverageReviewScore: AverageReviewScore
		                     } AS Movies
		                     SKIP $Skip
		                     LIMIT $Limit
		                     """;
		
		var cursor = await tx.RunAsync(query,
			new
			{
				userId = userId.ToString(), queryParams.Title, Actor = queryParams.Actor.ToString(),
				SortBy = queryParams.SortBy.ToString(), SortOrder = queryParams.SortOrder.ToString(),
				Skip = (queryParams.PageNumber - 1) * queryParams.PageSize, Limit = queryParams.PageSize
			});
		
		var items = await cursor.ToListAsync(record =>
		{
			var movieWithActorsDto = record["Movies"].As<IDictionary<string, object>>();
			return movieWithActorsDto.ConvertToMovieDto();
		});
		
		// language=Cypher
		const string totalCountQuery = """
		                               MATCH (ignoredMovie:Movie)<-[:IGNORES]-(u:User { Id: $userId })
		                               WITH COLLECT(ignoredMovie.Id) AS ignoredMovieIds
		                               
		                               MATCH (m:Movie)
		                               WHERE NOT m.Id IN ignoredMovieIds AND toLower(m.Title) CONTAINS toLower($Title)
		                                 AND ($Actor IS NULL OR $Actor = "" OR EXISTS {
		                                 MATCH (m)<-[:PLAYED_IN]-(a:Actor)
		                                 WHERE a.Id = $Actor
		                               })
		                               WITH COUNT(m) as TotalCount
		                               RETURN TotalCount
		                               """;

		var totalCountCursor = await tx.RunAsync(totalCountQuery,
			new { userId = userId.ToString(), queryParams.Title, Actor = queryParams.Actor.ToString()});
		var totalCount = await totalCountCursor.SingleAsync(record => record["TotalCount"].As<int>());
		return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
	}

	public async Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=Cypher
		const string query = """
		                     MATCH (m:Movie { Id: $movieId })
		                     RETURN m.PicturePublicId AS PicturePublicId
		                     """;

		var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString() });
		return await cursor.SingleAsync(record => record["PicturePublicId"].As<string?>());
	}

	public async Task<MovieDetailsDto> AddMovie(IAsyncQueryRunner tx, AddMovieDto movieDto, string? pictureAbsoluteUri, string? picturePublicId)
	{
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
			                                  OnWatchlist: false,
			                                  IsFavorite: false,
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

			return  movieNodeWithoutActors.ConvertToMovieDetailsDto();
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
		                             Popularity: 0,
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
		                             OnWatchlist: false,
		                             IsFavorite: false,
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
		return movieNode.ConvertToMovieDetailsDto();
	}

	public async Task DeleteMovie(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=cypher
		const string movieExistsAsync = """
		                                  MATCH (m:Movie { Id: $movieId })
		                                  DETACH DELETE m
		                                """;

		await tx.RunAsync(movieExistsAsync, new { movieId = movieId.ToString() });
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

	public async Task<MovieDetailsDto?> GetMovieDetails(IAsyncQueryRunner tx, Guid movieId, Guid? userId = null)
	{
		try
		{
			// language=Cypher
			const string query = """
			                     MATCH (m:Movie { Id: $movieId })
			                     SET m.Popularity = m.Popularity + 1
			                     WITH m
			                     OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
			                     OPTIONAL MATCH (m)<-[c:COMMENTED]-(u:User)
			                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { Id: $userId })
			                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { Id: $userId })
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
			                         WHEN u is NULL OR c is NULL THEN null
			                         ELSE {
			                           Id: c.Id,
			                           MovieId: m.Id,
			                           UserId: u.Id,
			                           Username: u.Name,
			                           Text: c.Text,
			                           CreatedAt: c.CreatedAt,
			                           IsEdited: c.IsEdited
			                         }
			                       END
			                     ) AS Comments, AVG(r.Score) AS AverageReviewScore, COUNT(w) > 0 AS OnWatchlist, COUNT(f) > 0 AS IsFavorite
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
			                       OnWatchlist: OnWatchlist,
			                       IsFavorite: IsFavorite,
			                       Comments: Comments,
			                       AverageReviewScore: COALESCE(AverageReviewScore, 0)
			                     } AS MovieWithActors
			                     """;
			
			var cursor = await tx.RunAsync(query,
				new {  movieId = movieId.ToString(), userId = userId?.ToString() });
			return await cursor.SingleAsync(record =>
				record["MovieWithActors"].As<IDictionary<string, object>>().ConvertToMovieDetailsDto());
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public async Task<PagedList<MovieDto>> GetMoviesWhenNotLoggedIn(IAsyncQueryRunner tx, MovieQueryParams queryParams)
	{
		// language=Cypher
		const string query = """
		                     MATCH (m:Movie)
		                     WHERE toLower(m.Title) CONTAINS toLower($Title)
		                       AND ($Actor IS NULL OR $Actor = "" OR EXISTS {
		                         MATCH (m)<-[:PLAYED_IN]-(a:Actor { Id: $Actor })
		                       })
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     WITH m, COALESCE(AVG(r.Score), 0) AS AverageReviewScore
		                     ORDER BY
		                     CASE WHEN $SortOrder = "Ascending" THEN CASE WHEN $SortBy = "AverageReviewScore" THEN AverageReviewScore ELSE m[$SortBy] END ELSE null END ASC,
		                     CASE WHEN $SortOrder = "Descending" THEN CASE WHEN $SortBy = "AverageReviewScore" THEN AverageReviewScore ELSE m[$SortBy] END ELSE null END DESC
		                     RETURN {
		                       Id: m.Id,
		                       Title: m.Title,
		                       PictureAbsoluteUri: m.PictureAbsoluteUri,
		                       MinimumAge: m.MinimumAge,
		                       OnWatchlist: false,
		                       IsFavorite: false,
		                       AverageReviewScore: AverageReviewScore
		                     } AS Movies
		                     SKIP $Skip
		                     LIMIT $Limit
		                     """;
		
		var cursor = await tx.RunAsync(query, new
		{
			queryParams.Title, Actor = queryParams.Actor.ToString(),
			SortBy = queryParams.SortBy.ToString(), SortOrder = queryParams.SortOrder.ToString(),
			Skip = (queryParams.PageNumber - 1) * queryParams.PageSize,
			Limit = queryParams.PageSize
		});
		
		var items = await cursor.ToListAsync(record =>
		{
			var movieWithActorsDto = record["Movies"].As<IDictionary<string, object>>();
			return movieWithActorsDto.ConvertToMovieDto();
		});
		
		// language=Cypher
		const string totalCountQuery = """
		                               MATCH (m:Movie)
		                               WHERE toLower(m.Title) CONTAINS toLower($Title)
		                               AND ($Actor IS NULL OR $Actor = "" OR EXISTS {
		                                 MATCH (m)<-[:PLAYED_IN]-(a:Actor { Id: $Actor })
		                               })
		                               WITH COUNT(m) as TotalCount
		                               RETURN TotalCount
		                               """;

		var totalCountCursor = await tx.RunAsync(totalCountQuery,
			new {queryParams.Title, Actor = queryParams.Actor.ToString()});
		var totalCount = await totalCountCursor.SingleAsync(record => record["TotalCount"].As<int>());
		return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
	}
}
