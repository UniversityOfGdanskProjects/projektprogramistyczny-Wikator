﻿using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class FavouriteRepository : IFavouriteRepository
{
    public async Task<IEnumerable<MovieDetailsDto>> GetAllFavouriteMovies(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)<-[:FAVOURITE]-(u:User {Id: $userId})
                             OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
                             OPTIONAl MATCH (m)<-[c:COMMENTED]-(u2:User)
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
                               OnWatchlist: true,
                               Comments: Comments,
                               AverageReviewScore: COALESCE(AverageReviewScore, 0)
                             } AS MovieWithActors
                             """;

        var result = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await result.ToListAsync(record =>
        {
            var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
            return movieWithActorsDto.ConvertToMovieDetailsDto();
        });
    }

    public async Task SetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId }), (m:Movie { Id: $movieId })
                             CREATE (u)-[r:FAVOURITE]->(m)
                             """;
        
        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task UnsetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:FAVOURITE]->(:Movie { Id: $movieId })
                             DELETE r
                             """;

        await tx.RunAsync(query,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
    }
    
    public async Task<bool> MovieIsFavourite(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string checkIfWatchlistExistsQuery = """
                                                   MATCH (:User { Id: $userId })-[r:FAVOURITE]->(:Movie { Id: $movieId })
                                                   WITH COUNT(r) > 0 AS favouriteExists
                                                   RETURN favouriteExists
                                                   """;
        
        var cursor = await tx.RunAsync(checkIfWatchlistExistsQuery,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["favouriteExists"].As<bool>());
    }
}