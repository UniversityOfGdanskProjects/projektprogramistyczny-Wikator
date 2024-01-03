using MoviesApi.DTOs;
using MoviesApi.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesApi.Extensions;

public static class DictionaryExtensions
{
    public static MovieDto ConvertToMovieDto(this IDictionary<string, object> movie) =>
        new(
            Id: Guid.Parse(movie["Id"].As<string>()),
            Title: movie["Title"].As<string>(),
            Description: movie["Description"].As<string>(),
            InTheaters: movie["InTheaters"].As<bool>(),
            AverageScore: movie["AverageReviewScore"].As<double>(),
            TrailerUrl: movie["TrailerAbsoluteUri"].As<string?>(),
            PictureUri: movie["PictureAbsoluteUri"].As<string?>(),
            ReleaseDate: DateOnly.FromDateTime(movie["ReleaseDate"].As<DateTime>()) ,
            MinimumAge: movie["MinimumAge"].As<int>(),
            Actors: movie["Actors"].As<List<IDictionary<string, object>>>().Select(ConvertToActorDto),
            Comments: movie["Comments"].As<List<IDictionary<string, object>>>().Select(ConvertToCommentDto)
        );


    public static ActorDto ConvertToActorDto(this IDictionary<string, object> actorData) =>
        new(
            Id: Guid.Parse(actorData["Id"].As<string>()),
            FirstName: actorData["FirstName"].As<string>(),
            LastName: actorData["LastName"].As<string>(),
            DateOfBirth: DateOnly.FromDateTime(actorData["DateOfBirth"].As<DateTime>()),
            Biography: actorData["Biography"].As<string>(),
            PictureUri: actorData["PictureAbsoluteUri"].As<string?>()
        );

    public static CommentDto ConvertToCommentDto(this IDictionary<string, object> commentData) =>
        new(
            Id: Guid.Parse(commentData["Id"].As<string>()),
            MovieId: Guid.Parse(commentData["MovieId"].As<string>()),
            UserId: Guid.Parse(commentData["UserId"].As<string>()),
            Username: commentData["Username"].As<string>(),
            Text: commentData["Text"].As<string>(),
            CreatedAt: commentData["CreatedAt"].As<DateTime>(),
            IsEdited: commentData["IsEdited"].As<bool>()
        );
}