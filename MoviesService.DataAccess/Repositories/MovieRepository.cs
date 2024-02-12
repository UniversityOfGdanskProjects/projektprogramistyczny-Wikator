using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Helpers;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using MoviesService.Models.Parameters;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

public class MovieRepository : IMovieRepository
{
    public async Task<PagedList<MovieDto>> GetMoviesExcludingIgnored(IAsyncQueryRunner tx, Guid userId,
        MovieQueryParams queryParams)
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
                             WITH m, COLLECT(
                               CASE
                                 WHEN g IS NOT NULL THEN g.name
                               END
                             ) AS genres
                             OPTIONAL MATCH (m)<-[ur:REVIEWED]-(:User { id: $userId })
                             WITH m, CASE WHEN ur IS NOT NULL THEN { id: ur.id, score: ur.score } END AS userReviewScore, genres
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             WITH m, userReviewScore, genres, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(r) AS reviewsCount
                             ORDER BY
                             CASE WHEN $sortOrder = "ascending" THEN CASE WHEN $sortBy = "averageReviewScore" THEN averageReviewScore ELSE m[$sortBy] END ELSE null END ASC,
                             CASE WHEN $sortOrder = "descending" THEN CASE WHEN $sortBy = "averageReviewScore" THEN averageReviewScore ELSE m[$sortBy] END ELSE null END DESC
                             RETURN
                               m.id AS id,
                               m.title AS title,
                               m.pictureAbsoluteUri AS pictureAbsoluteUri,
                               m.minimumAge AS minimumAge,
                               EXISTS { MATCH (:User { id: $userId })-[:WATCHLIST]->(m) } AS onWatchlist,
                               EXISTS { MATCH (:User { id: $userId })-[:FAVOURITE]->(m) } AS isFavourite,
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
                                       AND ($genre IS NULL OR $genre = "" OR EXISTS {
                                         MATCH (m)-[:IS]->(g:Genre)
                                         WHERE toLower(g.name) = toLower($genre)
                                       })
                                       RETURN COUNT(m) AS totalCount
                                       """;

        var totalCountParameters = new
        {
            userId = userId.ToString(),
            title = queryParams.Title,
            actor = queryParams.Actor.ToString(),
            inTheaters = queryParams.InTheaters,
            genre = queryParams.Genre
        };

        var totalCountCursor = await tx.RunAsync(totalCountQuery, totalCountParameters);
        var totalCount = await totalCountCursor.SingleAsync(record => record["totalCount"].As<int>());
        return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
    }

    public async Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             CALL apoc.when(
                               EXISTS { MATCH (:Movie {id: $movieId }) },
                               'MATCH (m:Movie {id: $movieId }) RETURN m.picturePublicId AS picturePublicId',
                               'RETURN null AS picturePublicId',
                               { movieId: $movieId }
                             ) YIELD value
                             RETURN value.picturePublicId AS picturePublicId
                             """;

        var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["picturePublicId"].As<string?>());
    }

    public async Task<string> GetMostPopularMovieTitle(IAsyncQueryRunner tx)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)
                             RETURN m.title AS title
                             ORDER BY m.popularity DESC
                             LIMIT 1
                             """;

        var cursor = await tx.RunAsync(query);
        return await cursor.SingleAsync(record => record["title"].As<string>());
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
                                   WITH m, actors, COLLECT(
                                     CASE
                                       WHEN genres.g IS NOT NULL THEN genres.g.name
                                     END
                                   ) AS genres
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
                             MATCH (g:Genre)
                             CALL apoc.do.when(
                               g IS NOT NULL AND g.name IN $genres,
                               'MERGE (m)-[:IS]->(g)
                                RETURN g',
                               'OPTIONAL MATCH (m)-[r:IS]->(g)
                                DELETE r
                                RETURN g',
                               { g: g, m: m }
                             ) YIELD value AS genres
                             WITH m, COLLECT(
                               CASE
                                 WHEN genres.g IS NOT NULL THEN genres.g.name
                               END
                             ) AS genres
                             OPTIONAL MATCH (a:Actor)
                             CALL apoc.do.when(
                               a IS NOT NULL AND a.id IN $actorIds,
                               'MERGE (a)-[:PLAYED_IN]->(m)
                                RETURN a',
                               'OPTIONAL MATCH (a)-[r:PLAYED_IN]->(m)
                                DELETE r
                                RETURN a',
                               { a: a, m: m }
                             ) YIELD value AS actors
                             WITH m, genres, COLLECT(
                               CASE
                                 WHEN actors.a IS NOT NULL THEN {
                                   id: actors.a.id,
                                   firstName: actors.a.firstName,
                                   lastName: actors.a.lastName,
                                   dateOfBirth: actors.a.dateOfBirth,
                                   biography: actors.a.biography,
                                   pictureAbsoluteUri: actors.a.pictureAbsoluteUri
                                 }
                               END
                             ) AS actors
                             OPTIONAL MATCH (m)<-[c:COMMENTED]-(u:User)
                             WITH m, genres, actors,
                             COLLECT(
                               CASE
                                 WHEN u is NOT NULL THEN {
                                   id: c.id,
                                   movieId: m.id,
                                   userId: u.id,
                                   username: u.name,
                                   text: c.text,
                                   createdAt: c.createdAt,
                                   isEdited: c.isEdited
                                 }
                               END
                             ) AS comments
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             WITH m, actors, genres, comments, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(r) AS reviewsCount
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User { id: $userId })
                             WITH m, actors, genres, comments, averageReviewScore, reviewsCount, CASE WHEN r IS NULL THEN NULL ELSE { id: r.id, score: r.score } END AS userReviewScore
                             RETURN
                               m.id AS id,
                               m.title AS title,
                               m.description AS description,
                               m.inTheaters AS inTheaters,
                               m.trailerAbsoluteUri AS trailerAbsoluteUri,
                               m.pictureAbsoluteUri AS pictureAbsoluteUri,
                               m.releaseDate AS releaseDate,
                               m.minimumAge AS minimumAge,
                               EXISTS { MATCH (:User { id: $userId })-[:WATCHLIST]->(m) } AS onWatchlist,
                               EXISTS { MATCH (:User { id: $userId })-[:FAVOURITE]->(m) } AS isFavourite,
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

    public async Task AddMoviePicture(IAsyncQueryRunner tx, Guid movieId, string pictureAbsoluteUri,
        string picturePublicId)
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
        const string query = "RETURN EXISTS { (m:Movie { id: $movieId }) } AS movieExists";

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
                                 WITH m, COLLECT(
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
                                 ) AS actors
                                 OPTIONAL MATCH (m)<-[c:COMMENTED]-(u:User)
                                 WITH m, actors, COLLECT(
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
                                 ) AS comments
                                 OPTIONAL MATCH (m)-[:IS]->(g:Genre)
                                 WITH m, actors, comments, COLLECT(
                                   CASE
                                     WHEN g IS NOT NULL THEN g.name
                                   END
                                 ) AS genres
                                 OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                                 WITH m, actors, comments, genres, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(r) AS reviewsCount
                                 OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User { id: $userId })
                                 WITH m, actors, comments, genres, averageReviewScore, reviewsCount, CASE WHEN r IS NULL THEN NULL ELSE { id: r.id, score: r.score } END AS userReviewScore
                                 RETURN
                                   m.id AS id,
                                   m.title AS title,
                                   m.description AS description,
                                   m.inTheaters AS inTheaters,
                                   m.trailerAbsoluteUri AS trailerAbsoluteUri,
                                   m.pictureAbsoluteUri AS pictureAbsoluteUri,
                                   m.releaseDate AS releaseDate,
                                   m.minimumAge AS minimumAge,
                                   EXISTS { MATCH (:User { id: $userId })-[:WATCHLIST]->(m) } AS onWatchlist,
                                   EXISTS { MATCH (:User { id: $userId })-[:FAVOURITE]->(m) } AS isFavourite,
                                   userReviewScore AS userReviewScore,
                                   reviewsCount AS reviewsCount,
                                   actors,
                                   comments,
                                   COALESCE(genres, []) AS genres,
                                   averageReviewScore AS averageReviewScore
                                 """;

            var cursor = await tx.RunAsync(query, new { movieId = movieId.ToString(), userId = userId?.ToString() });
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
                             WITH m
                             OPTIONAL MATCH (g:Genre)<-[:IS]-(m)
                             WITH m, COLLECT(
                               CASE
                                 WHEN g IS NOT NULL THEN g.name
                               END
                             ) AS genres
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             WITH m, COALESCE(AVG(r.score), 0) AS averageReviewScore, COUNT(r) AS reviewsCount, genres
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
                                       AND ($genre IS NULL OR $genre = "" OR EXISTS {
                                         MATCH (m)-[:IS]->(g:Genre)
                                         WHERE toLower(g.name) = toLower($genre)
                                       })
                                       RETURN COUNT(m) AS totalCount
                                       """;

        var totalCountParameters = new
        {
            title = queryParams.Title,
            actor = queryParams.Actor.ToString(),
            inTheaters = queryParams.InTheaters,
            genre = queryParams.Genre
        };

        var totalCountCursor = await tx.RunAsync(totalCountQuery, totalCountParameters);
        var totalCount = await totalCountCursor.SingleAsync(record => record["totalCount"].As<int>());
        return new PagedList<MovieDto>(items, queryParams.PageNumber, queryParams.PageSize, totalCount);
    }
}