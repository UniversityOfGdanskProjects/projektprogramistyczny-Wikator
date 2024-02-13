using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

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

    public async Task<ActorDto> UpdateActor(IAsyncQueryRunner tx, Guid id, EditActorDto actorDto)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor {id: $id})
                             SET
                               a.firstName = $firstName,
                               a.lastName = $lastName,
                               a.dateOfBirth = $dateOfBirth,
                               a.biography = $biography
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
            biography = actorDto.Biography
        });

        return await cursor.SingleAsync(record => record.ConvertToActorDto());
    }

    public async Task DeleteActor(IAsyncQueryRunner tx, Guid id)
    {
        // language=Cypher
        const string deleteQuery = "MATCH (a:Actor) WHERE a.id = $id DETACH DELETE a";
        await tx.RunAsync(deleteQuery, new { id = id.ToString() });
    }

    public async Task AddActorPicture(IAsyncQueryRunner tx, Guid actorId, string pictureAbsoluteUri,
        string picturePublicId)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor {id: $id})
                             SET
                               a.pictureAbsoluteUri = $pictureAbsoluteUri,
                               a.picturePublicId = $picturePublicId
                             """;

        var parameters = new
        {
            id = actorId.ToString(),
            pictureAbsoluteUri,
            picturePublicId
        };

        await tx.RunAsync(query, parameters);
    }

    public async Task DeleteActorPicture(IAsyncQueryRunner tx, Guid actorId)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor {id: $id})
                             SET
                               a.pictureAbsoluteUri = NULL,
                               a.picturePublicId = NULL
                             """;

        await tx.RunAsync(query, new { id = actorId.ToString() });
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

    public async Task<bool> ActorPictureExists(IAsyncQueryRunner tx, Guid actorId)
    {
        // language=Cypher
        const string query = """
                             MATCH (a:Actor {id: $id})
                             RETURN a.picturePublicId IS NOT NULL AS actorPictureExists
                             """;

        var cursor = await tx.RunAsync(query, new { id = actorId.ToString() });
        return await cursor.SingleAsync(record => record["actorPictureExists"].As<bool>());
    }
}