using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ActorRepository(IPhotoService photoService, IDriver driver) : Repository(driver), IActorRepository
{
    private IPhotoService PhotoService { get; } = photoService;
    
    public async Task<IEnumerable<ActorDto>> GetAllActors()
    {
        return await ExecuteAsync(async tx =>
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
        });
    }

    public async Task<ActorDto?> GetActor(Guid id)
    {
        return await ExecuteAsync(async tx =>
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
                return await ConvertCursorToActorDto(cursor);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        });
    }

    public async Task<ActorDto?> CreateActor(UpsertActorDto actorDto)
    {
        return await ExecuteAsync(async tx =>
        {
            string? pictureAbsoluteUri = null;
            string? picturePublicId = null;
			
            if (actorDto.FileContent is not null)
            {
                var file = new FormFile(
                    new MemoryStream(actorDto.FileContent),
                    0,
                    actorDto.FileContent.Length,
                    "file", actorDto.FileName ?? $"movie-{new Guid()}");

                var uploadResult = await PhotoService.AddPhotoAsync(file);
                if (uploadResult.Error is not null)
                    throw new Exception("Image failed to add");

                pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
                picturePublicId = uploadResult.PublicId;
            }
            
            // language=Cypher
            const string actorQuery = """
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

            var actorCursor = await tx.RunAsync(actorQuery, new
            {
                actorDto.FirstName, actorDto.LastName, actorDto.DateOfBirth, actorDto.Biography,
                PictureAbsoluteUri = pictureAbsoluteUri, PicturePublicId = picturePublicId
            });

            return await ConvertCursorToActorDto(actorCursor);
        });
    }

    public async Task<ActorDto?> UpdateActor(Guid id, UpsertActorDto actorDto)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string actorQuery = """
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
            
            var actorCursor = await tx.RunAsync(actorQuery, new
            {
                id = id.ToString(), actorDto.FirstName, actorDto.LastName,
                actorDto.DateOfBirth, actorDto.Biography
            });
            
            return await ConvertCursorToActorDto(actorCursor);
        });
    }

    public async Task<QueryResult> DeleteActor(Guid id)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string matchQuery = "MATCH (a:Actor) WHERE a.Id = $id RETURN a.PicturePublicId AS PicturePublicId";
            var matchCursor = await tx.RunAsync(matchQuery, new { id = id.ToString() });

            try
            {
                var actor = await matchCursor.SingleAsync();
                var publicId = actor["PicturePublicId"].As<string?>();

                if (publicId is not null && (await PhotoService.DeleteASync(publicId)).Error is not null)
                    return QueryResult.PhotoFailedToDelete;
                
                // language=Cypher
                const string deleteQuery = "MATCH (a:Actor) WHERE Id(a) = $id DETACH DELETE a";
                await tx.RunAsync(deleteQuery, new { id = id.ToString() });
                return QueryResult.Completed;
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }
        });
    }
    
    private static async Task<ActorDto?> ConvertCursorToActorDto(IResultCursor cursor)
    {
        try
        {
            var result = await cursor.SingleAsync();
            var actor = result["Actor"].As<Dictionary<string, object>>();
            return actor.ConvertToActorDto();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
