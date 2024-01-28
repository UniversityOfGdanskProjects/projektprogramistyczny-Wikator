﻿using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MoviesApi.Repository.Contracts;
using MQTTnet;
using MQTTnet.Client;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;
using Newtonsoft.Json;

namespace MoviesApi.Services;
    
public class MqttService : IMqttService
{
    private readonly IMqttClient _mqttClient;

    public MqttService(IConfiguration config, IDriver driver, IMessageRepository messageRepository)
    {
        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("85e67d8c7c8a4955a07dbd76348a5bba.s2.eu.hivemq.cloud", 8883)
            .WithCredentials("Wiktor", config["MqttPassword"])
            .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
            .Build();
        
        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var message = JsonConvert.DeserializeObject<Message>(e.ApplicationMessage.ConvertPayloadToString());
            
            if (message?.Jwt is null || message.Content is null)
                return;
            
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"] ?? throw new Exception("Token key not found"))),
                ValidIssuer = "https://moviesapiwebtest.azurewebsites.net",
                ValidateAudience = false,
            };
            
            try
            {
                handler.ValidateToken(message.Jwt, validationParameters, out _);
            }
            catch
            {
                return;
            }
                
            var token = handler.ReadJwtToken(message.Jwt);
            var userId = token.Claims.FirstOrDefault(claim => claim.Type == "nameid")?.Value;
            
            if (userId is null)
                return;
            
            await using var session = driver.AsyncSession();

            var messageDto = await session.ExecuteWriteAsync(async tx =>
                await messageRepository.CreateMessageAsync(tx, Guid.Parse(userId), message.Content));
            
            if (messageDto is null)
                return;
                
            await SendNotificationAsync("chat/message/validated", messageDto);
        };

        _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None).Wait();
        
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(
                f =>
                {
                    f.WithTopic("chat/message/api");
                })
            .Build();
        
        _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None).Wait();
    }

    public async Task SendNotificationAsync(string topic, object message)
    {
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message.ToString())
            .Build();

        await _mqttClient.PublishAsync(applicationMessage);
    }
    
    private record Message(string? Jwt, string? Content);
}
