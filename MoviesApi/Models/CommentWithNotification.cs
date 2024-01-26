using MoviesApi.DTOs.Responses;

namespace MoviesApi.Models;

public record CommentWithNotification(
    CommentDto Comment, RealTimeNotification Notification);