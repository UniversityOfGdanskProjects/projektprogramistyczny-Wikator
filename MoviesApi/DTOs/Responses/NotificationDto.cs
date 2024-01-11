namespace MoviesApi.DTOs.Responses;

public record NotificationDto(Guid Id, bool IsRead, DateTime CreatedAt, CommentDto Comment);
