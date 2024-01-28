using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
public class MessageController(IDriver driver, IMessageRepository messageRepository) : BaseApiController(driver)
{
    private IMessageRepository MessageRepository { get; } = messageRepository;
    
    [HttpGet]
    public Task<IActionResult> GetMostRecentMessagesAsync()
    {
        return ExecuteReadAsync(async tx =>
        {
            var messages = await MessageRepository.GetMostRecentMessagesAsync(tx);
            return Ok(messages);
        });
    }
    
    [HttpPost]
    [Authorize]
    public Task<IActionResult> CreateMessageAsync(CreateMessageDto messageDto)
    {
        return ExecuteWriteAsync(async tx =>
        {
            var userId = User.GetUserId();
            var message = await MessageRepository.CreateMessageAsync(tx, userId, messageDto.Content);
            return Ok(message);
        });
    }
}