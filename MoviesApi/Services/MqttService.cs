using MoviesApi.Services.Contracts;
using MQTTnet;
using MQTTnet.Client;

namespace MoviesApi.Services;

public class MqttService(IConfiguration config) : IMqttService
{
    private IConfiguration Config { get; } = config;
    
    public async Task SendNotificationAsync(string topic, object message)
    {
        var mqttFactory = new MqttFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("85e67d8c7c8a4955a07dbd76348a5bba.s2.eu.hivemq.cloud", 8883)
            .WithCredentials("Wiktor", Config["MqttPassword"])
            .WithTlsOptions(
                o => o.WithCertificateValidationHandler(_ => true))
            .Build();
        
        using (var timeout = new CancellationTokenSource(5000))
        {
            await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);
        }
        
        
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message.ToString())
            .Build();
        
        await mqttClient.PublishAsync(applicationMessage);
    }
}