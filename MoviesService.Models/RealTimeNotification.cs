namespace MoviesService.Models;

public record RealTimeNotification(
    string CommentUsername,
    string CommentText,
    Guid MovieId,
    string MovieTitle);