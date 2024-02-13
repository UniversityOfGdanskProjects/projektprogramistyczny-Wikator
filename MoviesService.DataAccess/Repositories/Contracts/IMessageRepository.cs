using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IMessageRepository
{
    Task<MessageDto> CreateMessageAsync(IAsyncQueryRunner tx, Guid userId, string messageContent);
    Task<IEnumerable<MessageDto>> GetMostRecentMessagesAsync(IAsyncQueryRunner tx);
}