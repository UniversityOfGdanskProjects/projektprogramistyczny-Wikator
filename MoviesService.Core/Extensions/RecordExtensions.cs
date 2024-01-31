using MoviesService.Models;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.Core.Extensions;

public static class RecordExtensions
{
    public static MovieDetailsDto ConvertToMovieDetailsDto(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            Title: record["title"].As<string>(),
            Description: record["description"].As<string>(),
            InTheaters: record["inTheaters"].As<bool>(),
            AverageScore: record["averageReviewScore"].As<double>(),
            TrailerUrl: record["trailerAbsoluteUri"].As<string?>(),
            PictureUri: record["pictureAbsoluteUri"].As<string?>(),
            ReleaseDate: DateOnly.FromDateTime(record["releaseDate"].As<DateTime>()),
            MinimumAge: record["minimumAge"].As<int>(),
            OnWatchlist: record["onWatchlist"].As<bool>(),
            IsFavourite: record["isFavourite"].As<bool>(),
            UserReview: ConvertToReviewIdAndScoreDto(record["userReviewScore"].As<IDictionary<string, object>?>()),
            ReviewsCount: record["reviewsCount"].As<int>(),
            Actors: record["actors"].As<List<IDictionary<string, object>>>().Select(ConvertToActorDto),
            Comments: record["comments"].As<List<IDictionary<string, object>>>().Select(ConvertToCommentDto),
            Genres: record["genres"].As<List<string>>()
        );

    public static MovieDto ConvertToMovieDto(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            Title: record["title"].As<string>(),
            AverageScore: record["averageReviewScore"].As<double>(),
            PictureUri: record["pictureAbsoluteUri"].As<string?>(),
            OnWatchlist: record["onWatchlist"].As<bool>(),
            IsFavourite: record["isFavourite"].As<bool>(),
            UserReview: ConvertToReviewIdAndScoreDto(record["userReviewScore"].As<IDictionary<string, object>?>()),
            ReviewsCount: record["reviewsCount"].As<int>(),
            MinimumAge: record["minimumAge"].As<int>(),
            Genres: record["genres"].As<List<string>>()
        );

    public static ActorDto ConvertToActorDto(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            FirstName: record["firstName"].As<string>(),
            LastName: record["lastName"].As<string>(),
            DateOfBirth: DateOnly.FromDateTime(record["dateOfBirth"].As<DateTime>()),
            Biography: record["biography"].As<string>(),
            PictureUri: record["pictureAbsoluteUri"].As<string?>()
        );

    public static CommentDto ConvertToCommentDto(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            MovieId: Guid.Parse(record["movieId"].As<string>()),
            UserId: Guid.Parse(record["userId"].As<string>()),
            Username: record["username"].As<string>(),
            Text: record["text"].As<string>(),
            CreatedAt: record["createdAt"].As<DateTime>(),
            IsEdited: record["isEdited"].As<bool>()
        );

    public static CommentWithNotification ConvertToCommentWithNotification(this IRecord record)
    {
        var comment = record.ConvertToCommentDto();
        
        var notificationDictionary = record["notification"].As<IDictionary<string, object>>();
        var notification = new RealTimeNotification(
            CommentUsername: notificationDictionary["commentUsername"].As<string>(),
            CommentText: notificationDictionary["commentText"].As<string>(),
            MovieTitle: notificationDictionary["movieTitle"].As<string>(),
            MovieId: Guid.Parse(notificationDictionary["movieId"].As<string>()));

        return new CommentWithNotification(
            Comment: comment,
            Notification: notification);
    }

    public static ReviewDto ConvertToReviewDto(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            UserId: Guid.Parse(record["userId"].As<string>()),
            MovieId: Guid.Parse(record["movieId"].As<string>()),
            Score: record["score"].As<int>()
        );

    public static NotificationDto ConvertToNotificationDto(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            IsRead: record["isRead"].As<bool>(),
            CreatedAt: record["createdAt"].As<DateTime>(),
            CommentUsername: record["commentUsername"].As<string>(),
            CommentText: record["commentText"].As<string>(),
            MovieId: Guid.Parse(record["movieId"].As<string>()),
            MovieTitle: record["movieTitle"].As<string>()
        );

    public static User ConvertToUser(this IRecord record) =>
        new(
            Id: Guid.Parse(record["id"].As<string>()),
            Name: record["name"].As<string>(),
            Email: record["email"].As<string>(),
            Role: record["role"].As<string>()
        );
    
    private static ActorDto ConvertToActorDto(IDictionary<string, object> dictionary) =>
        new(
            Id: Guid.Parse(dictionary["id"].As<string>()),
            FirstName: dictionary["firstName"].As<string>(),
            LastName: dictionary["lastName"].As<string>(),
            DateOfBirth: DateOnly.FromDateTime(dictionary["dateOfBirth"].As<DateTime>()),
            Biography: dictionary["biography"].As<string>(),
            PictureUri: dictionary["pictureAbsoluteUri"].As<string?>()
        );
    
    private static CommentDto ConvertToCommentDto(IDictionary<string, object> dictionary) =>
        new(
            Id: Guid.Parse(dictionary["id"].As<string>()),
            MovieId: Guid.Parse(dictionary["movieId"].As<string>()),
            UserId: Guid.Parse(dictionary["userId"].As<string>()),
            Username: dictionary["username"].As<string>(),
            Text: dictionary["text"].As<string>(),
            CreatedAt: dictionary["createdAt"].As<DateTime>(),
            IsEdited: dictionary["isEdited"].As<bool>()
        );

    private static ReviewIdAndScoreDto? ConvertToReviewIdAndScoreDto(IDictionary<string, object>? dictionary)
    {
        return dictionary switch
        {
            null => null,
            _ => new ReviewIdAndScoreDto(
                Id: Guid.Parse(dictionary["id"].As<string>()),
                Score: dictionary["score"].As<int>()
            )
        };
    }
}
