using MoviesApi.DTOs;
using Neo4j.Driver;

namespace MoviesApi.Extensions;

public static class DictionaryExtensions
{
    public static MovieDto ConvertToMovieDto(this IDictionary<string, object> movie) =>
         new(
                Id: movie["Id"].As<int>(),
                Title: movie["Title"].As<string>(),
                Description: movie["Description"].As<string>(),
                AverageScore: movie["AverageReviewScore"].As<double>(),
                Actors: movie["Actors"].As<List<IDictionary<string, object>>>().Select(ConvertToActorDto)
        );


    private static ActorDto ConvertToActorDto(this IDictionary<string, object> actorData) =>
        new(
            Id: actorData["Id"].As<int>(),
            FirstName: actorData["FirstName"].As<string>(),
            LastName: actorData["LastName"].As<string>(),
            DateOfBirth: actorData["DateOfBirth"].As<string>(),
            Biography: actorData["Biography"].As<string>()
        );
}