using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface ICommentRepository
{
    Task<CommentDto?> GetCommentAsync(IAsyncQueryRunner tx, Guid commentId);
    Task<CommentWithNotification> AddCommentAsync(IAsyncQueryRunner tx, Guid userId, AddCommentDto addCommentDto);
    Task<CommentDto> EditCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId, EditCommentDto addCommentDto);
    Task DeleteCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId);
    Task<bool> CommentExistsAsOwnerOrAdmin(IAsyncQueryRunner tx, Guid commentId, Guid userId);
    Task<Guid?> GetMovieIdFromCommentAsOwnerOrAdminAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId);
}