using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ActorRepository : IActorRepository
{
    public async Task<IEnumerable<ActorDto>> GetAllActors(IAsyncQueryRunner tx)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor)
                             RETURN {
                                 Id: a.Id,
                                 FirstName: a.FirstName,
                                 LastName: a.LastName,
                                 DateOfBirth: a.DateOfBirth,
                                 Biography: a.Biography,
                                 PictureAbsoluteUri: a.PictureAbsoluteUri
                             } AS Actors
                             """;

        var cursor = await tx.RunAsync(query);
        return await cursor.ToListAsync(record =>
        {
            var actor = record["Actors"].As<IDictionary<string, object>>();

            return actor.ConvertToActorDto();
        });
    }

    public async Task<ActorDto?> GetActor(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor { Id: $id })
                             RETURN {
                                 Id: a.Id,
                                 FirstName: a.FirstName,
                                 LastName: a.LastName,
                                 DateOfBirth: a.DateOfBirth,
                                 Biography: a.Biography,
                                 PictureAbsoluteUri: a.PictureAbsoluteUri
                             } AS Actor
                             """;
    
        var cursor = await tx.RunAsync(query, new { id = id.ToString() });
        
        try
        {
            return await cursor.SingleAsync(record =>
                record["Actor"].As<Dictionary<string, object>>().ConvertToActorDto());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<ActorDto> CreateActor(IAsyncQueryRunner tx, UpsertActorDto actorDto, string? pictureAbsoluteUri, string? picturePublicId)
    {
        // language=Cypher
        const string query = """
                             CREATE (a:Actor { 
                               Id: randomUUID(),
                               FirstName: $FirstName,
                               LastName: $LastName,
                               DateOfBirth: $DateOfBirth,
                               Biography: $Biography,
                               PictureAbsoluteUri: $PictureAbsoluteUri,
                               PicturePublicId: $PicturePublicId
                             })
                             RETURN {
                               Id: a.Id,
                               FirstName: a.FirstName,
                               LastName: a.LastName,
                               DateOfBirth: a.DateOfBirth,
                               Biography: a.Biography,
                               PictureAbsoluteUri: a.PictureAbsoluteUri
                             } As Actor
                             """;

        var cursor = await tx.RunAsync(query, new
        {
            actorDto.FirstName, actorDto.LastName, actorDto.DateOfBirth, actorDto.Biography,
            PictureAbsoluteUri = pictureAbsoluteUri, PicturePublicId = picturePublicId
        });

        return await cursor.SingleAsync(record =>
            record["Actor"].As<Dictionary<string, object>>().ConvertToActorDto());
    }

    public async Task<ActorDto> UpdateActor(IAsyncQueryRunner tx, Guid id, UpsertActorDto actorDto)
    {
        // language=Cypher
        const string query = """
                                  MATCH (a:Actor {Id: $id})
                                  SET
                                    a.FirstName = $FirstName,
                                    a.LastName = $LastName,
                                    a.DateOfBirth = $DateOfBirth,
                                    a.Biography = $Biography
                                  RETURN {
                                    Id: a.Id,
                                    FirstName: a.FirstName,
                                    LastName: a.LastName,
                                    DateOfBirth: a.DateOfBirth,
                                    Biography: a.Biography,
                                    PictureAbsoluteUri: a.PictureAbsoluteUri
                                  } As Actor
                                  """;
        
        var cursor = await tx.RunAsync(query, new
        {
            id = id.ToString(), actorDto.FirstName, actorDto.LastName,
            actorDto.DateOfBirth, actorDto.Biography
        });

        return await cursor.SingleAsync(record =>
            record["Actor"].As<IDictionary<string, object>>().ConvertToActorDto());
    }

    public async Task DeleteActor(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string deleteQuery = "MATCH (a:Actor) WHERE Id(a) = $id DETACH DELETE a";
        await tx.RunAsync(deleteQuery, new { id = id.ToString() });
    }

    public async Task<bool> ActorExists(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor {Id: $id})
                             WITH COUNT(a) > 0 AS actorExists
                             RETURN actorExists
                             """;

        var actorExistsCursor = await tx.RunAsync(query, new { id = id.ToString() });

        return await actorExistsCursor.SingleAsync(record => record["actorExists"].As<bool>());
    }

    public async Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid actorId)
    {
        // language=Cypher
        const string matchQuery = "MATCH (a:Actor) WHERE a.Id = $id RETURN a.PicturePublicId AS PicturePublicId";
        var cursor = await tx.RunAsync(matchQuery, new { id = actorId.ToString() });
        return await cursor.SingleAsync(record => record["PicturePublicId"].As<string?>());
    }
}
