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
                             RETURN 
                                 a.id AS id,
                                 a.firstName AS firstName,
                                 a.lastName AS lastName,
                                 a.dateOfBirth AS dateOfBirth,
                                 a.biography AS biography,
                                 a.pictureAbsoluteUri AS pictureAbsoluteUri
                             """;

        var cursor = await tx.RunAsync(query);
        return await cursor.ToListAsync(record => record.ConvertToActorDto());
    }

    public async Task<ActorDto?> GetActor(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor { id: $id })
                             RETURN
                                 a.id AS id,
                                 a.firstName AS firstName,
                                 a.lastName AS lastName,
                                 a.dateOfBirth AS dateOfBirth,
                                 a.biography AS biography,
                                 a.pictureAbsoluteUri AS pictureAbsoluteUri
                             """;
    
        var cursor = await tx.RunAsync(query, new { id = id.ToString() });
        
        try
        {
            return await cursor.SingleAsync(record => record.ConvertToActorDto());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<ActorDto> CreateActor(IAsyncQueryRunner tx, AddActorDto actorDto,
        string? pictureAbsoluteUri, string? picturePublicId)
    {
        // language=Cypher
        const string query = """
                             CREATE (a:Actor { 
                               id: apoc.create.uuid(),
                               firstName: $firstName,
                               lastName: $lastName,
                               dateOfBirth: $dateOfBirth,
                               biography: $biography,
                               pictureAbsoluteUri: $pictureAbsoluteUri,
                               picturePublicId: $picturePublicId
                             })
                             RETURN
                               a.id AS id,
                               a.firstName AS firstName,
                               a.lastName AS lastName,
                               a.dateOfBirth AS dateOfBirth,
                               a.biography AS biography,
                               a.pictureAbsoluteUri AS pictureAbsoluteUri
                             """;

        var cursor = await tx.RunAsync(query, new
        {
            firstName = actorDto.FirstName,
            lastName = actorDto.LastName,
            dateOfBirth = actorDto.DateOfBirth,
            biography = actorDto.Biography,
            pictureAbsoluteUri,
            picturePublicId
        });

        return await cursor.SingleAsync(record => record.ConvertToActorDto());
    }

    public async Task<ActorDto> UpdateActor(IAsyncQueryRunner tx, Guid id,
        UpdateActorDto actorDto, string? pictureAbsoluteUri, string? picturePublicId)
    {
        // language=Cypher
        const string query = """
                                  MATCH (a:Actor {id: $id})
                                  SET
                                    a.firstName = coalesce($firstName, a.firstName),
                                    a.lastName = coalesce($lastName, a.lastName),
                                    a.dateOfBirth = coalesce($dateOfBirth, a.dateOfBirth),
                                    a.biography = $biography,
                                    a.pictureAbsoluteUri = $pictureAbsoluteUri,
                                    a.picturePublicId = $picturePublicId
                                  RETURN
                                    a.id AS id,
                                    a.firstName AS firstName,
                                    a.lastName AS lastName,
                                    a.dateOfBirth as dateOfBirth,
                                    a.biography AS biography,
                                    a.pictureAbsoluteUri AS pictureAbsoluteUri
                                  """;
        
        var cursor = await tx.RunAsync(query, new
        {
            id = id.ToString(),
            firstName = actorDto.FirstName, 
            lastName = actorDto.LastName,
            dateOfBirth = actorDto.DateOfBirth,
            biography = actorDto.Biography,
            pictureAbsoluteUri,
            picturePublicId
        });

        return await cursor.SingleAsync(record => record.ConvertToActorDto());
    }

    public async Task DeleteActor(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string deleteQuery = "MATCH (a:Actor) WHERE a.id = $id DETACH DELETE a";
        await tx.RunAsync(deleteQuery, new { id = id.ToString() });
    }

    public async Task<bool> ActorExists(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor {id: $id})
                             RETURN COUNT(a) > 0 AS actorExists
                             """;

        var actorExistsCursor = await tx.RunAsync(query, new { id = id.ToString() });
        return await actorExistsCursor.SingleAsync(record => record["actorExists"].As<bool>());
    }

    public async Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid actorId)
    {
        // language=Cypher
        const string matchQuery = "MATCH (a:Actor) WHERE a.id = $id RETURN a.picturePublicId AS picturePublicId";
        var cursor = await tx.RunAsync(matchQuery, new { id = actorId.ToString() });
        return await cursor.SingleAsync(record => record["picturePublicId"].As<string?>());
    }
}
