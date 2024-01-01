using MoviesApi.DTOs;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ActorRepository(IDriver driver) : Repository(driver), IActorRepository
{
    public async Task<IEnumerable<ActorDto>> GetAllActors()
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (a:Actor)
                                 RETURN {
                                     Id: Id(a),
                                     FirstName: a.FirstName,
                                     LastName: a.LastName,
                                     DateOfBirth: a.DateOfBirth,
                                     Biography: a.Biography
                                 } AS Actors
                                 """;

            var cursor = await tx.RunAsync(query);
            return await cursor.ToListAsync(record =>
            {
                var actor = record["Actors"].As<IDictionary<string, object>>();

                return new ActorDto(
                    Id: actor["Id"].As<int>(),
                    FirstName: actor["FirstName"].As<string>(),
                    LastName: actor["LastName"].As<string>(),
                    DateOfBirth: actor["DateOfBirth"].As<string>(),
                    Biography: actor["Biography"].As<string?>());
            });
        });
    }

    public async Task<ActorDto?> GetActor(int id)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (a:Actor)
                                 WHERE Id(a) = $id
                                 RETURN {
                                     Id: Id(a),
                                     FirstName: a.FirstName,
                                     LastName: a.LastName,
                                     DateOfBirth: a.DateOfBirth,
                                     Biography: a.Biography
                                 } AS Actor
                                 """;
        
            var cursor = await tx.RunAsync(query, new { id });

            try
            {
                var actorNode = await cursor.SingleAsync();

                if (actorNode is null)
                    throw new Exception("Something went wrong when fetching actor");
        
                var actor = actorNode["Actor"].As<IDictionary<string, object>>();
                return new ActorDto(
                    Id: actor["Id"].As<int>(),
                    FirstName: actor["FirstName"].As<string>(),
                    LastName: actor["LastName"].As<string>(),
                    DateOfBirth: actor["DateOfBirth"].As<string>(),
                    Biography: actor["Biography"].As<string?>());
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
            // language=Cypher
            const string actorQuery = """
                                      CREATE (a:Actor { FirstName: $FirstName, LastName: $LastName, DateOfBirth: $DateOfBirth, Biography: $Biography})
                                      RETURN Id(a) as id, a.FirstName as firstName, a.LastName as lastName, a.DateOfBirth as dateOfBirth, a.Biography as biography
                                      """;

            var actorCursor = await tx.RunAsync(actorQuery, new
            {
                actorDto.FirstName, actorDto.LastName, actorDto.DateOfBirth, actorDto.Biography
            });
            
            var actorNode = await actorCursor.SingleAsync();

            if (actorNode is null)
                return null;

            return new ActorDto
            (
                Id: actorNode["id"].As<int>(),
                FirstName: actorNode["firstName"].As<string>(),
                LastName: actorNode["lastName"].As<string>(),
                DateOfBirth: actorNode["dateOfBirth"].As<string>(),
                Biography: actorNode["biography"].As<string>()
            );
        });
    }

    public async Task<ActorDto?> UpdateActor(int id, UpsertActorDto actorDto)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string actorQuery = """
                                      MATCH (a:Actor) WHERE Id(a) = $id
                                      SET a.FirstName = $FirstName, a.LastName = $LastName, a.DateOfBirth = $DateOfBirth, a.Biography = $Biography
                                      RETURN Id(a) as id, a.FirstName as firstName, a.LastName as lastName, a.DateOfBirth as dateOfBirth, a.Biography as biography
                                      """;
            
            var actorCursor = await tx.RunAsync(actorQuery, new
            {
                id, actorDto.FirstName, actorDto.LastName, actorDto.DateOfBirth,
                actorDto.Biography
            });
            var actorNode = await actorCursor.SingleAsync();

            if (actorNode is null)
                return null;

            return new ActorDto
            (
                Id: actorNode["id"].As<int>(),
                FirstName: actorNode["firstName"].As<string>(),
                LastName: actorNode["lastName"].As<string>(),
                DateOfBirth: actorNode["dateOfBirth"].As<string>(),
                Biography: actorNode["biography"].As<string>()
            );
        });
    }

    public async Task<bool> DeleteActor(int id)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string matchQuery = "MATCH (a:Actor) WHERE Id(a) = $id RETURN a";
            var matchCursor = await tx.RunAsync(matchQuery, new { id });

            try
            {
                await matchCursor.SingleAsync();
                // language=Cypher
                const string deleteQuery = "MATCH (a:Actor) WHERE Id(a) = $id DETACH DELETE a";
                await tx.RunAsync(deleteQuery, new { id });
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });
    }
}
