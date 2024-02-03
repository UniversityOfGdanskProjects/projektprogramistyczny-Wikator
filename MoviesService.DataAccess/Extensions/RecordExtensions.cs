using MoviesService.Models;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Extensions;

public static class RecordExtensions
{
    public static MovieDetailsDto ConvertToMovieDetailsDto(this IRecord record)
    {
        return new MovieDetailsDto(
            Guid.Parse(record["id"].As<string>()),
            record["title"].As<string>(),
            record["description"].As<string>(),
            record["inTheaters"].As<bool>(),
            record["averageReviewScore"].As<double>(),
            record["trailerAbsoluteUri"].As<string?>(),
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
    }

    public static MovieDto ConvertToMovieDto(this IRecord record)
    {
        return new MovieDto(
            Guid.Parse(record["id"].As<string>()),
            record["title"].As<string>(),
            record["averageReviewScore"].As<double>(),
            PictureUri: record["pictureAbsoluteUri"].As<string?>(),
            OnWatchlist: record["onWatchlist"].As<bool>(),
            IsFavourite: record["isFavourite"].As<bool>(),
            UserReview: ConvertToReviewIdAndScoreDto(record["userReviewScore"].As<IDictionary<string, object>?>()),
            ReviewsCount: record["reviewsCount"].As<int>(),
            MinimumAge: record["minimumAge"].As<int>(),
            Genres: record["genres"].As<List<string>>()
        );
    }

    public static ActorDto ConvertToActorDto(this IRecord record)
    {
        return new ActorDto(
            Guid.Parse(record["id"].As<string>()),
            record["firstName"].As<string>(),
            record["lastName"].As<string>(),
            DateOnly.FromDateTime(record["dateOfBirth"].As<DateTime>()),
            record["biography"].As<string>(),
            record["pictureAbsoluteUri"].As<string?>()
        );
    }

    public static CommentDto ConvertToCommentDto(this IRecord record)
    {
        return new CommentDto(
            Guid.Parse(record["id"].As<string>()),
            Guid.Parse(record["movieId"].As<string>()),
            Guid.Parse(record["userId"].As<string>()),
            record["username"].As<string>(),
            record["text"].As<string>(),
            record["createdAt"].As<DateTime>(),
            record["isEdited"].As<bool>()
        );
    }

    public static CommentWithNotification ConvertToCommentWithNotification(this IRecord record)
    {
        var comment = record.ConvertToCommentDto();

        var notificationDictionary = record["notification"].As<IDictionary<string, object>>();
        var notification = new RealTimeNotification(
            notificationDictionary["commentUsername"].As<string>(),
            notificationDictionary["commentText"].As<string>(),
            MovieTitle: notificationDictionary["movieTitle"].As<string>(),
            MovieId: Guid.Parse(notificationDictionary["movieId"].As<string>()));

        return new CommentWithNotification(
            comment,
            notification);
    }

    public static ReviewDto ConvertToReviewDto(this IRecord record)
    {
        return new ReviewDto(
            Guid.Parse(record["id"].As<string>()),
            Guid.Parse(record["userId"].As<string>()),
            Guid.Parse(record["movieId"].As<string>()),
            record["score"].As<int>()
        );
    }

    public static NotificationDto ConvertToNotificationDto(this IRecord record)
    {
        return new NotificationDto(
            Guid.Parse(record["id"].As<string>()),
            record["isRead"].As<bool>(),
            record["createdAt"].As<DateTime>(),
            record["commentUsername"].As<string>(),
            record["commentText"].As<string>(),
            Guid.Parse(record["movieId"].As<string>()),
            record["movieTitle"].As<string>()
        );
    }

    public static User ConvertToUser(this IRecord record)
    {
        return new User(
            Guid.Parse(record["id"].As<string>()),
            record["name"].As<string>(),
            record["email"].As<string>(),
            record["role"].As<string>()
        );
    }

    private static ActorDto ConvertToActorDto(IDictionary<string, object> dictionary)
    {
        return new ActorDto(
            Guid.Parse(dictionary["id"].As<string>()),
            dictionary["firstName"].As<string>(),
            dictionary["lastName"].As<string>(),
            DateOnly.FromDateTime(dictionary["dateOfBirth"].As<DateTime>()),
            dictionary["biography"].As<string>(),
            dictionary["pictureAbsoluteUri"].As<string?>()
        );
    }

    private static CommentDto ConvertToCommentDto(IDictionary<string, object> dictionary)
    {
        return new CommentDto(
            Guid.Parse(dictionary["id"].As<string>()),
            Guid.Parse(dictionary["movieId"].As<string>()),
            Guid.Parse(dictionary["userId"].As<string>()),
            dictionary["username"].As<string>(),
            dictionary["text"].As<string>(),
            dictionary["createdAt"].As<DateTime>(),
            dictionary["isEdited"].As<bool>()
        );
    }

    private static ReviewIdAndScoreDto? ConvertToReviewIdAndScoreDto(IDictionary<string, object>? dictionary)
    {
        return dictionary switch
        {
            null => null,
            _ => new ReviewIdAndScoreDto(
                Guid.Parse(dictionary["id"].As<string>()),
                dictionary["score"].As<int>()
            )
        };
    }
}