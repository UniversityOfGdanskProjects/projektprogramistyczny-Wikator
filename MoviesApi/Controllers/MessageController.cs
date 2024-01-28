using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
public class MessageController(IDriver driver, IMessageRepository messageRepository,
    IMqttService mqttService) : BaseApiController(driver)
{
    private IMessageRepository MessageRepository { get; } = messageRepository;
    private IMqttService MqttService { get; } = mqttService;
    
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