using MoviesApi.DTOs;
using MoviesApi.Models;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ActorRepository(IDriver driver) : Repository(driver), IActorRepository
{
    public async Task<Actor?> CreateActor(AddActorDto actor)
    {
        var session = Driver.AsyncSession();

        try
        {
            return await session.ExecuteWriteAsync(tx => CreateAndReturnActor(tx, actor));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private static async Task<Actor?> CreateAndReturnActor(IAsyncQueryRunner tx, AddActorDto actorDto)
    {
        var actorQuery = $"" +
                         $"CREATE (a:Movie {{ FirstName: \"{actorDto.FirstName}\", LastName: \"{actorDto.LastName}\", DateOfBirth: \"{actorDto.DateOfBirth}\", Biography:  \"{actorDto.Biography}\"}})" +
                         $"RETURN Id(a) as id, a.FirstName as firstName, a.LastName as lastName, a.DateOfBirth as dateOfBirth, a.Biography as biography";
        var actorCursor = await tx.RunAsync(actorQuery);
        var actorNode = await actorCursor.SingleAsync();

        if (actorNode is null)
            return null;

        return new Actor
        {
            Id = actorNode["id"].As<int>(),
            FirstName = actorNode["firstName"].As<string>(),
            LastName = actorNode["lastName"].As<string>(),
            DateOfBirth = actorNode["dateOfBirth"].As<string>(),
            Biography = actorNode["biography"].As<string>(),
        };
    }
}