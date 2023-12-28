using System.Security.Cryptography;
using System.Text;
using MoviesApi.DTOs;
using MoviesApi.Models;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class AccountRepository(IDriver driver) : IAccountRepository
{
    private IDriver Driver { get; } = driver;
    
    public async Task<User?> RegisterAsync(LoginDto registerDto)
    {
        var session = Driver.AsyncSession();

        try
        {
            return await session.ExecuteWriteAsync(tx => CreateAndReturnNewUser(tx, registerDto));
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

    public async Task<User?> LoginASync(LoginDto loginDto)
    {
        var session = Driver.AsyncSession();

        try
        {
            return await session.ExecuteWriteAsync(tx => CreateAndReturnNewUser(tx, loginDto));
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


    private static async Task<User?> CreateAndReturnNewUser(IAsyncQueryRunner tx, LoginDto registerDto)
    {
        using HMACSHA512 hmac = new();
        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        var passwordSalt = hmac.Key;
        
        var query = $$"""
                            CREATE (a:User {
                                Name: "{{registerDto.Name}}",
                                PasswordHash: "{{Convert.ToBase64String(passwordHash)}}",
                                PasswordSalt: "{{Convert.ToBase64String(passwordSalt)}}",
                                Role: "User"
                            })
                            RETURN a.Name as name, a.Role as role, ID(a) as id
                      """;
        
        var cursor = await tx.RunAsync(query);
        var node = await cursor.SingleAsync();
				
        return new User 
        {
            Id = node["id"].As<int>(),
            Name = node["name"].As<string>(),
            Role = (Role)Enum.Parse(typeof(Role), node["role"].As<string>())
        };
    }

    private static async Task<User?> CheckPasswordAndReturnUser(IAsyncQueryRunner tx, LoginDto loginDto)
    {
        var query = $$"""
                         MATCH (a:User { Name: "{{loginDto.Name}}" })
                         RETURN a.Name as name, a.Role as role, a.PasswordHash as passwordHash, a.PasswordSalt as passwordSalt, ID(a) as id
                      """;
        var cursor = await tx.RunAsync(query);
        var node = await cursor.SingleAsync();

        if (node is null)
            return null;
				
        var storedPasswordHash = node["passwordHash"].As<string>();
        var storedPasswordSalt = node["passwordSalt"].As<string>();

        Console.WriteLine();
        using HMACSHA512 hmac = new(Convert.FromBase64String(storedPasswordSalt));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
        var storedHash = Convert.FromBase64String(storedPasswordHash);

        if (computedHash.Where((t, i) => t != storedHash[i]).Any())
            return null;
				
        return new User
        {
            Id = node["id"].As<int>(),
            Name = node["name"].As<string>(),
            Role = (Role)Enum.Parse(typeof(Role), node["role"].As<string>())
        };
    }
}