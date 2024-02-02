using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Extensions;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using MoviesService.Services.Contracts;

namespace MoviesService.Api.Controllers;

[Route("api/[controller]")]
public class MessageController(IAsyncQueryExecutor queryExecutor, IMessageRepository messageRepository,
    IMqttService mqttService) : BaseApiController(queryExecutor)
{
    private IMessageRepository MessageRepository { get; } = messageRepository;
    private IMqttService MqttService { get; } = mqttService;
    
    [HttpGet]
    public Task<IActionResult> GetMostRecentMessagesAsync()
    {
        return QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var messages = await MessageRepository.GetMostRecentMessagesAsync(tx);
            return Ok(messages.Reverse());
        });
    }
    
    [HttpPost]
    [Authorize]
    public Task<IActionResult> CreateMessageAsync(CreateMessageDto messageDto)
    {
        return QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            var userId = User.GetUserId();
            var message = await MessageRepository.CreateMessageAsync(tx, userId, messageDto.Content);
            _ = PublishMqttMessageAsync(message!);
            return Ok(message!);
        });
    }
    
    private async Task PublishMqttMessageAsync(MessageDto messageDto)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var payload = JsonSerializer.Serialize(messageDto, options);
        await MqttService.SendNotificationAsync("chat/message/validated", payload);
    }
}