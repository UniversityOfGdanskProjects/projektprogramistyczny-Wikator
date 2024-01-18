using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface ICommentRepository
{
    Task<CommentDto?> GetCommentAsync(IAsyncQueryRunner tx, Guid commentId);
    Task<CommentDto> AddCommentAsync(IAsyncQueryRunner tx, Guid userId, AddCommentDto addCommentDto);
    Task<CommentDto> EditCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId, EditCommentDto addCommentDto);
    Task DeleteCommentAsync(IAsyncQueryRunner tx, Guid commentId, Guid userId);
    Task<bool> CommentExistsAsOwnerOrAdmin(IAsyncQueryRunner tx, Guid commentId, Guid userId);
}