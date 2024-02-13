namespace MoviesService.Models.DTOs.Responses;

public record NotificationDto(
    Guid Id,
    bool IsRead,
    DateTime CreatedAt,
    string CommentUsername,
    string CommentText,
    Guid MovieId,
    string MovieTitle);