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
		                         MATCH (m)<-[:PLAYED_IN]-(a:Actor { id: $actor })
		                       })
		                       AND ($genre IS NULL OR $genre = "" OR EXISTS {
		                         MATCH (m)-[:IS]->(g:Genre)
		                         WHERE toLower(g.name) = toLower($genre)
		                       })
		                       AND ($inTheaters IS NULL OR m.inTheaters = $inTheaters)
		                     OPTIONAL MATCH (g:Genre)<-[:IS]-(m)
		                     WHERE ($genre IS NULL OR $genre = "" OR g.name = $genre)
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { id: $userId })
		                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { id: $userId })
		                     OPTIONAL MATCH (m)<-[ur:REVIEWED]-(:User { id: $userId })
		                     WITH m, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(w) > 0 AS onWatchlist, COUNT(f) > 0 AS isFavourite, COUNT(r) AS reviewsCount, CASE WHEN ur IS NULL THEN NULL ELSE { id: ur.id, score: ur.score } END AS userReviewScore,
		                       COLLECT(
		                         CASE
		                           WHEN g IS NOT NULL THEN g.name
		                         END
		                       ) AS genres
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
		                       averageReviewScore,
		                       COALESCE(genres, []) AS genres
		                     SKIP $skip
		                     LIMIT $limit
		                     """;

		var parameters = new
		{
			userId = userId.ToString(),
			title = queryParams.Title,
			actor = queryParams.Actor.ToString(),
			genre = queryParams.Genre,
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
		                               AND ($inTheaters IS NULL OR m.inTheaters = $inTheaters)
		                               RETURN COUNT(m) AS totalCount
		                               """;

		var totalCountParameters = new
		{
			userId = userId.ToString(),
			title = queryParams.Title,
			actor = queryParams.Actor.ToString(),
			inTheaters = queryParams.InTheaters
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
		                           OPTIONAL MATCH (a:Actor)
		                           WHERE a.id IN $actorIds
		                           CALL apoc.do.when(
		                             a IS NOT NULL,
		                             'CREATE (a)-[:PLAYED_IN]->(m)
		                              RETURN a',
		                             'RETURN a',
		                             { a: a, m: m }
		                           ) YIELD value AS actors
		                           WITH m, COLLECT(
		                             CASE
		                               WHEN actors.a IS NULL THEN null
		                               ELSE {
		                                 id: actors.a.id,
		                                 firstName: actors.a.firstName,
		                                 lastName: actors.a.lastName,
		                                 dateOfBirth: actors.a.dateOfBirth,
		                                 biography: actors.a.biography,
		                                 pictureAbsoluteUri: actors.a.pictureAbsoluteUri
		                               }
		                             END
		                           ) AS actors
		                           OPTIONAL MATCH (g:Genre)
		                           WHERE g.name IN $genres
		                           CALL apoc.do.when(
		                             g IS NOT NULL,
		                             'CREATE (m)-[:IS]->(g)
		                              RETURN g',
		                             'RETURN g',
		                             { g: g, m: m }
		                           ) YIELD value AS genres
		                           WITH m, COLLECT(
		                             CASE
		                               WHEN genres.g IS NOT NULL THEN genres.g.name
		                             END
		                           ) AS genres, actors
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
		                             COALESCE(actors, []) AS actors,
		                             COALESCE(genres, []) AS genres,
		                             [] AS comments,
		                             0 AS averageReviewScore
		                           """;
		
		var movieWithActorsParameters = new
		{
			title = movieDto.Title,
			actorIds = movieDto.ActorIds.Select(a => a.ToString()),
			genres = movieDto.Genres,
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

	public async Task<MovieDetailsDto> EditMovie(IAsyncQueryRunner tx, Guid movieId, Guid userId, EditMovieDto movieDto)
	{
		// language=Cypher
		const string query = """
		                     MATCH (m:Movie { id: $movieId })
		                     SET m.title = $title,
		                         m.description = $description,
		                         m.inTheaters = $inTheaters,
		                         m.releaseDate = $releaseDate,
		                         m.minimumAge = $minimumAge,
		                         m.trailerAbsoluteUri = $trailerAbsoluteUri
		                     WITH m
		                     OPTIONAL MATCH (m)-[r:IS]->(:Genre)
		                     DELETE r
		                     WITH m
		                     OPTIONAL MATCH (m)<-[r:PLAYED_IN]-(:Actor)
		                     DELETE r
		                     WITH m
		                     OPTIONAL MATCH (a:Actor)
		                     WHERE a.id IN $actorIds
		                     CALL apoc.do.when(
		                       a IS NOT NULL,
		                       'CREATE (a)-[:PLAYED_IN]->(m)
		                        RETURN a',
		                       'RETURN a',
		                       { a: a, m: m }
		                     ) YIELD value AS actors
		                     WITH m, COLLECT(
		                       CASE
		                         WHEN actors.a IS NULL THEN null
		                         ELSE {
		                           id: actors.a.id,
		                           firstName: actors.a.firstName,
		                           lastName: actors.a.lastName,
		                           dateOfBirth: actors.a.dateOfBirth,
		                           biography: actors.a.biography,
		                           pictureAbsoluteUri: actors.a.pictureAbsoluteUri
		                         }
		                       END
		                     ) AS actors
		                     OPTIONAL MATCH (g:Genre)
		                     WHERE g.name IN $genres
		                     CALL apoc.do.when(
		                       g IS NOT NULL,
		                       'CREATE (m)-[:IS]->(g)
		                        RETURN g',
		                       'RETURN g',
		                       { g: g, m: m }
		                     ) YIELD value AS genres
		                     WITH m, COLLECT(
		                       CASE
		                         WHEN genres.g IS NOT NULL THEN genres.g.name
		                       END
		                     ) AS genres, actors
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     OPTIONAL MATCH (m)<-[c:COMMENTED]-(u:User)
		                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { id: $userId })
		                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { id: $userId })
		                     OPTIONAL MATCH (m)<-[ur:REVIEWED]-(:User { id: $userId })
		                     WITH m, genres, actors,
		                     COLLECT(
		                       CASE
		                         WHEN u is NOT NULL AND c is NOT NULL THEN {
		                           id: c.id,
		                           movieId: m.id,
		                           userId: u.id,
		                           username: u.name,
		                           text: c.text,
		                           createdAt: c.createdAt,
		                           isEdited: c.isEdited
		                         }
		                       END
		                     ) AS comments, AVG(r.score) AS averageReviewScore, COUNT(w) > 0 AS onWatchlist, COUNT(f) > 0 AS isFavourite, COUNT(r) AS reviewsCount, CASE WHEN ur IS NULL THEN NULL ELSE { id: ur.id, score: ur.score } END AS userReviewScore
		                     RETURN
		                       m.id AS id,
		                       m.title AS title,
		                       m.description AS description,
		                       m.inTheaters AS inTheaters,
		                       m.trailerAbsoluteUri AS trailerAbsoluteUri,
		                       m.pictureAbsoluteUri AS pictureAbsoluteUri,
		                       m.releaseDate AS releaseDate,
		                       m.minimumAge AS minimumAge,
		                       onWatchlist AS onWatchlist,
		                       isFavourite AS isFavourite,
		                       userReviewScore AS userReviewScore,
		                       reviewsCount AS reviewsCount,
		                       COALESCE(actors, []) AS actors,
		                       COALESCE(genres, []) AS genres,
		                       COALESCE(comments, []) AS comments,
		                       0 AS averageReviewScore
		                     """;

		var parameters = new
		{
			userId = userId.ToString(),
			movieId = movieId.ToString(),
			title = movieDto.Title,
			actorIds = movieDto.ActorIds.Select(a => a.ToString()),
			genres = movieDto.Genres,
			description = movieDto.Description,
			inTheaters = movieDto.InTheaters,
			releaseDate = movieDto.ReleaseDate,
			minimumAge = movieDto.MinimumAge,
			trailerAbsoluteUri = movieDto.TrailerUrl
		};
		
		var cursor = await tx.RunAsync(query, parameters);
		return await cursor.SingleAsync(record => record.ConvertToMovieDetailsDto());
	}

	public async Task<bool> MoviePictureExists(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=cypher
		const string query = """
		                     MATCH (m:Movie { id: $movieId })
		                     RETURN m.pictureAbsoluteUri IS NOT NULL OR m.picturePublicId IS NOT NULL AS moviePictureExists
		                     """;

		var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString() });
		return await cursor.SingleAsync(record => record["moviePictureExists"].As<bool>());
	}

	public async Task DeleteMoviePicture(IAsyncQueryRunner tx, Guid movieId)
	{
		// language=cypher
		const string query = """
		                     MATCH (m:Movie { id: $movieId })
		                     SET m.pictureAbsoluteUri = null,
		                         m.picturePublicId = null
		                     """;

		await tx.RunAsync(query, new { movieId = movieId.ToString() });
	}

	public async Task AddMoviePicture(IAsyncQueryRunner tx, Guid movieId, string pictureAbsoluteUri, string picturePublicId)
	{
		// language=cypher
		const string query = """
		                     MATCH (m:Movie { id: $movieId })
		                     SET m.pictureAbsoluteUri = $pictureAbsoluteUri,
		                         m.picturePublicId = $picturePublicId
		                     """;
		
		var parameters = new
		{
			movieId = movieId.ToString(),
			pictureAbsoluteUri,
			picturePublicId
		};

		await tx.RunAsync(query, parameters);
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
			                     OPTIONAL MATCH (m)-[:IS]->(g:Genre)
			                     OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
			                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
			                     OPTIONAL MATCH (m)<-[c:COMMENTED]-(u:User)
			                     OPTIONAL MATCH (m)<-[w:WATCHLIST]-(:User { id: $userId })
			                     OPTIONAL MATCH (m)<-[f:FAVOURITE]-(:User { id: $userId })
			                     OPTIONAL MATCH (m)<-[ur:REVIEWED]-(:User { id: $userId })
			                     WITH m, COLLECT(DISTINCT
			                       CASE
			                         WHEN a IS NOT NULL THEN {
			                           id: a.id,
			                           firstName: a.firstName,
			                           lastName: a.lastName,
			                           dateOfBirth: a.dateOfBirth,
			                           biography: a.biography,
			                           pictureAbsoluteUri: a.pictureAbsoluteUri
			                         }
			                       END
			                     ) AS actors, 
			                     COLLECT(DISTINCT
			                       CASE
			                         WHEN u is NOT NULL AND c is NOT NULL THEN {
			                           id: c.id,
			                           movieId: m.id,
			                           userId: u.id,
			                           username: u.name,
			                           text: c.text,
			                           createdAt: c.createdAt,
			                           isEdited: c.isEdited
			                         }
			                       END
			                     ) AS comments,
			                     COLLECT(DISTINCT
			                       CASE
			                         WHEN g IS NOT NULL THEN g.name
			                       END
			                     ) AS genres, AVG(r.score) AS averageReviewScore, COUNT(w) > 0 AS onWatchlist, COUNT(f) > 0 AS isFavourite, COUNT(r) AS reviewsCount, CASE WHEN ur IS NULL THEN NULL ELSE { id: ur.id, score: ur.score } END AS userReviewScore 
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
			                       COALESCE(genres, []) AS genres,
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
		                       AND ($genre IS NULL OR $genre = "" OR EXISTS {
		                         MATCH (m)-[:IS]->(g:Genre)
		                         WHERE toLower(g.name) = toLower($genre)
		                       })
		                       AND ($inTheaters IS NULL OR m.inTheaters = $inTheaters)
		                     OPTIONAL MATCH (g:Genre)<-[:IS]-(m)
		                     OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
		                     WITH m, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(r) AS reviewsCount, COLLECT(
		                       CASE
		                         WHEN g IS NOT NULL THEN g.name
		                       END
		                     ) AS genres
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
		                       averageReviewScore,
		                       genres
		                     SKIP $skip
		                     LIMIT $limit
		                     """;
		
		var parameters = new
		{
			title = queryParams.Title,
			actor = queryParams.Actor.ToString(),
			genre = queryParams.Genre,
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
		                               AND ($inTheaters IS NULL OR m.inTheaters = $inTheaters)
		                               RETURN COUNT(m) AS totalCount
		                               """;
		
		var totalCountParameters = new
		{
			title = queryParams.Title,
			actor = queryParams.Actor.ToString(),
			inTheaters = queryParams.InTheaters
		};

		var totalCountCursor = await tx.RunAsync(totalCountQuery, totalCountParameters);
		var totalCount = await totalCountCursor.SingleAsync(record => record["totalCount"].As<int>());
		return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
	}
}
