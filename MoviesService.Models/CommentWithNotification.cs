using MoviesService.Models.DTOs.Responses;

namespace MoviesService.Models;

public record CommentWithNotification(
    CommentDto Comment,
    RealTimeNotification Notification);