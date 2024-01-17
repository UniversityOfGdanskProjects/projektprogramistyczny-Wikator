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
		                     MATCH (ignoredMovie:Movie)<-[:IGNORES]-(u:User { id: $userId })
		                     WITH COLLECT(ignoredMovie.id) AS ignoredMovieIds

		                     MATCH (m:Movie)
		                     WHERE NOT m.id IN ignoredMovieIds AND toLower(m.title) CONTAINS toLower($title)
		                       AND ($actor IS NULL OR $actor = "" OR EXISTS {
		                       MATCH (m)<-[:PLAYED_IN]-(a:Actor)
		                       WHERE a.id = $actor
		                       })
		                       AND ($inTheaters IS NULL OR m.inTheaters = $inTheaters)
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { id: $userId })
		                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { id: $userId })
		                     OPTIONAL MATCH (m)<-[ur:REVIEWED]-(:User { id: $userId })
		                     WITH m, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(w) > 0 AS onWatchlist, COUNT(f) > 0 AS isFavourite, COUNT(r) AS reviewsCount, ur.score AS userReviewScore
		                     ORDER BY
		                     CASE WHEN $sortOrder = "ascending" THEN CASE WHEN $sortBy = "averageReviewScore" THEN averageReviewScore ELSE m[$sortBy] END ELSE null END ASC,
		                     CASE WHEN $sortOrder = "descending" THEN CASE WHEN $sortBy = "averageReviewScore" THEN averageReviewScore ELSE m[$sortBy] END ELSE null END DESC
		                     RETURN
		                       m.id AS id,
		                       m.title AS title,
		                       m.pictureAbsoluteUri AS pictureAbsoluteUri,
		                       m.minimumAge AS minimumAge,
		                       onWatchlist,
		                       isFavourite,
		                       userReviewScore,
		                       reviewsCount,
		                       averageReviewScore
		                     SKIP $skip
		                     LIMIT $limit
		                     """;

		var parameters = new
		{
			userId = userId.ToString(),
			title = queryParams.Title,
			actor = queryParams.Actor.ToString(),
			inTheaters = queryParams.InTheaters,
			sortBy = queryParams.SortBy.ToCamelCaseString(),
			sortOrder = queryParams.SortOrder.ToCamelCaseString(),
			skip = (queryParams.PageNumber - 1) * queryParams.PageSize,
			limit = queryParams.PageSize
		};
		
		var cursor = await tx.RunAsync(query, parameters);
		var items = await cursor.ToListAsync(record => record.ConvertToMovieDto());
		
		// language=Cypher
		const string totalCountQuery = """
		                               MATCH (ignoredMovie:Movie)<-[:IGNORES]-(u:User { id: $userId })
		                               WITH COLLECT(ignoredMovie.id) AS ignoredMovieIds
		                               
		                               MATCH (m:Movie)
		                               WHERE NOT m.id IN ignoredMovieIds AND toLower(m.title) CONTAINS toLower($title)
		                                 AND ($actor IS NULL OR $actor = "" OR EXISTS {
		                                 MATCH (m)<-[:PLAYED_IN]-(a:Actor)
		                                 WHERE a.id = $actor
		                               })
		                               RETURN COUNT(m) AS totalCount
		                               """;

		var totalCountParameters = new
		{
			userId = userId.ToString(),
			title = queryParams.Title,
			actor = queryParams.Actor.ToString()
		};

		var totalCountCursor = await tx.RunAsync(totalCountQuery, totalCountParameters);
		var totalCount = await totalCountCursor.SingleAsync(record => record["totalCount"].As<int>());
		return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
	}

	public async Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=Cypher
		const string query = """
		                     MATCH (m:Movie { id: $movieId })
		                     RETURN m.picturePublicId AS picturePublicId
		                     """;

		var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString() });
		return await cursor.SingleAsync(record => record["picturePublicId"].As<string?>());
	}

	public async Task<MovieDetailsDto> AddMovie(IAsyncQueryRunner tx,
		AddMovieDto movieDto, string? pictureAbsoluteUri, string? picturePublicId)
	{
		if (!movieDto.ActorIds.Any())
		{
			// language=Cypher
			const string createMovieQuery = """
			                                CREATE (m:Movie {
			                                  id: apoc.create.uuid(),
			                                  title: $title,
			                                  description: $description,
			                                  pictureAbsoluteUri: $pictureAbsoluteUri,
			                                  picturePublicId: $picturePublicId,
			                                  inTheaters: $inTheaters,
			                                  releaseDate: $releaseDate,
			                                  minimumAge: $minimumAge,
			                                  trailerAbsoluteUri: $trailerAbsoluteUri,
			                                  popularity: 0
			                                })
			                                RETURN
			                                  m.id AS id,
			                                  m.title AS title,
			                                  m.description AS description,
			                                  m.inTheaters AS inTheaters,
			                                  m.trailerAbsoluteUri AS trailerAbsoluteUri,
			                                  m.pictureAbsoluteUri AS pictureAbsoluteUri,
			                                  m.releaseDate AS releaseDate,
			                                  m.minimumAge AS minimumAge,
			                                  false AS onWatchlist,
			                                  false AS isFavourite,
			                                  null AS userReviewScore,
			                                  0 AS reviewsCount,
			                                  [] AS actors,
			                                  [] AS comments,
			                                  0 AS averageReviewScore
			                                """;

			var movieWithoutActorsParameters = new
			{
				title = movieDto.Title,
				description = movieDto.Description,
				inTheaters = movieDto.InTheaters,
				releaseDate = movieDto.ReleaseDate,
				minimumAge = movieDto.MinimumAge,
				trailerAbsoluteUri = movieDto.TrailerUrl,
				pictureAbsoluteUri,
				picturePublicId
			};

			var movieCursorWithoutActors = await tx.RunAsync(createMovieQuery, movieWithoutActorsParameters);
			return await movieCursorWithoutActors.SingleAsync(record => record.ConvertToMovieDetailsDto());
		}
		
		// language=Cypher
		const string createQuery = """
		                           CREATE (m:Movie {
		                             id: apoc.create.uuid(),
		                             title: $title,
		                             description: $description,
		                             pictureAbsoluteUri: $pictureAbsoluteUri,
		                             picturePublicId: $picturePublicId,
		                             inTheaters: $inTheaters,
		                             releaseDate: $releaseDate,
		                             minimumAge: $minimumAge,
		                             popularity: 0,
		                             trailerAbsoluteUri: $trailerAbsoluteUri
		                           })
		                           WITH m
		                           UNWIND $actorIds AS actorId
		                           MATCH (a:Actor { id: actorId })
		                           CREATE (a)-[:PLAYED_IN]->(m)
		                           WITH m, COLLECT(
		                             CASE
		                               WHEN a IS NULL THEN null
		                               ELSE {
		                                 id: a.id,
		                                 firstName: a.firstName,
		                                 lastName: a.lastName,
		                                 dateOfBirth: a.dateOfBirth,
		                                 biography: a.biography,
		                                 pictureAbsoluteUri: a.pictureAbsoluteUri
		                               }
		                             END
		                           ) AS actors
		                           RETURN
		                             m.id AS id,
		                             m.title AS title,
		                             m.description AS description,
		                             m.inTheaters AS inTheaters,
		                             m.trailerAbsoluteUri AS trailerAbsoluteUri,
		                             m.pictureAbsoluteUri AS pictureAbsoluteUri,
		                             m.releaseDate AS releaseDate,
		                             m.minimumAge AS minimumAge,
		                             false AS onWatchlist,
		                             false AS isFavourite,
		                             null AS userReviewScore,
		                             0 AS reviewsCount,
		                             actors,
		                             [] AS comments,
		                             0 AS averageReviewScore
		                           """;
		
		var movieWithActorsParameters = new
		{
			title = movieDto.Title,
			actorIds = movieDto.ActorIds.Select(a => a.ToString()),
			description = movieDto.Description,
			inTheaters = movieDto.InTheaters,
			releaseDate = movieDto.ReleaseDate,
			minimumAge = movieDto.MinimumAge,
			trailerAbsoluteUri = movieDto.TrailerUrl,
			pictureAbsoluteUri,
			picturePublicId
		};

		var movieCursor = await tx.RunAsync(createQuery, movieWithActorsParameters);
		return await movieCursor.SingleAsync(record => record.ConvertToMovieDetailsDto());
	}

	public async Task DeleteMovie(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=cypher
		const string query = """
                             MATCH (m:Movie { id: $movieId })
                             DETACH DELETE m
                             """;

		await tx.RunAsync(query, new { movieId = movieId.ToString() });
	}

	public async Task<bool> MovieExists(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=Cypher
		const string query = """
                             MATCH (m:Movie { id: $movieId })
                             RETURN COUNT(m) > 0 AS movieExists
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
			                     MATCH (m:Movie { id: $movieId })
			                     SET m.popularity = m.popularity + 1
			                     WITH m
			                     OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
			                     OPTIONAL MATCH (m)<-[c:COMMENTED]-(u:User)
			                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { id: $userId })
			                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { id: $userId })
			                     OPTIONAL MATCH (m)<-[ur:REVIEWED]-(:User { id: $userId })
			                     WITH m, COLLECT(
			                       CASE
			                         WHEN a IS NULL THEN null
			                         ELSE {
			                           id: a.id,
			                           firstName: a.firstName,
			                           lastName: a.lastName,
			                           dateOfBirth: a.dateOfBirth,
			                           biography: a.biography,
			                           pictureAbsoluteUri: a.pictureAbsoluteUri
			                         }
			                       END
			                     ) AS actors, 
			                     COLLECT(
			                       CASE
			                         WHEN u is NULL OR c is NULL THEN null
			                         ELSE {
			                           id: c.id,
			                           movieId: m.id,
			                           userId: u.id,
			                           username: u.name,
			                           text: c.text,
			                           createdAt: c.createdAt,
			                           isEdited: c.isEdited
			                         }
			                       END
			                     ) AS comments, AVG(r.score) AS averageReviewScore, COUNT(w) > 0 AS onWatchlist, COUNT(f) > 0 AS isFavourite, COUNT(r) AS reviewsCount, ur.score AS userReviewScore 
			                     RETURN
			                       m.id AS id,
			                       m.title AS title,
			                       m.description AS description,
			                       m.inTheaters AS inTheaters,
			                       m.trailerAbsoluteUri AS trailerAbsoluteUri,
			                       m.pictureAbsoluteUri AS pictureAbsoluteUri,
			                       m.releaseDate AS releaseDate,
			                       m.minimumAge AS minimumAge,
			                       false AS onWatchlist,
			                       false AS isFavourite,
			                       null AS userReviewScore,
			                       0 AS reviewsCount,
			                       actors,
			                       comments,
			                       0 AS averageReviewScore
			                     """;
			
			var cursor = await tx.RunAsync(query, new {  movieId = movieId.ToString(), userId = userId?.ToString() });
			return await cursor.SingleAsync(record => record.ConvertToMovieDetailsDto());
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
		                     WHERE toLower(m.title) CONTAINS toLower($title)
		                       AND ($actor IS NULL OR $actor = "" OR EXISTS {
		                         MATCH (m)<-[:PLAYED_IN]-(a:Actor { id: $actor })
		                       })
		                       AND ($inTheaters IS NULL OR m.inTheaters = $inTheaters)
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     WITH m, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(r) AS reviewsCount
		                     ORDER BY
		                     CASE WHEN $sortOrder = "ascending" THEN CASE WHEN $sortBy = "averageReviewScore" THEN averageReviewScore ELSE m[$sortBy] END ELSE null END ASC,
		                     CASE WHEN $sortOrder = "descending" THEN CASE WHEN $sortBy = "averageReviewScore" THEN averageReviewScore ELSE m[$sortBy] END ELSE null END DESC
		                     RETURN
		                       m.id AS id,
		                       m.title AS title,
		                       m.pictureAbsoluteUri AS pictureAbsoluteUri,
		                       m.minimumAge AS minimumAge,
		                       false AS onWatchlist,
		                       false AS isFavourite,
		                       null AS userReviewScore,
		                       reviewsCount,
		                       averageReviewScore
		                     SKIP $skip
		                     LIMIT $limit
		                     """;
		
		var parameters = new
		{
			title = queryParams.Title,
			actor = queryParams.Actor.ToString(),
			inTheaters = queryParams.InTheaters,
			sortBy = queryParams.SortBy.ToCamelCaseString(),
			sortOrder = queryParams.SortOrder.ToCamelCaseString(),
			skip = (queryParams.PageNumber - 1) * queryParams.PageSize,
			limit = queryParams.PageSize
		};
		
		var cursor = await tx.RunAsync(query, parameters);
		
		var items = await cursor.ToListAsync(record => record.ConvertToMovieDto());
		
		// language=Cypher
		const string totalCountQuery = """
		                               MATCH (m:Movie)
		                               WHERE toLower(m.title) CONTAINS toLower($title)
		                               AND ($actor IS NULL OR $actor = "" OR EXISTS {
		                                 MATCH (m)<-[:PLAYED_IN]-(a:Actor { id: $actor })
		                               })
		                               RETURN COUNT(m) AS totalCount
		                               """;
		
		var totalCountParameters = new
		{
			title = queryParams.Title,
			actor = queryParams.Actor.ToString()
		};

		var totalCountCursor = await tx.RunAsync(totalCountQuery, totalCountParameters);
		var totalCount = await totalCountCursor.SingleAsync(record => record["totalCount"].As<int>());
		return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
	}
}
