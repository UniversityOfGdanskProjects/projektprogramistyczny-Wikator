using MQTTnet;
using MQTTnet.Client;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Services
{
    public class MqttService : IMqttService
    {
        private readonly IMqttClient _mqttClient;
        private readonly IConfiguration _config;

        public MqttService(IConfiguration config)
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("85e67d8c7c8a4955a07dbd76348a5bba.s2.eu.hivemq.cloud", 8883)
                .WithCredentials("Wiktor", config["MqttPassword"])
                .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
                .Build();

            _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None).Wait();
            _config = config;
        }

        public async Task SendNotificationAsync(string topic, object message)
        {
            if (!_mqttClient.IsConnected)
            {
                Console.WriteLine("Reconnecting...");
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("85e67d8c7c8a4955a07dbd76348a5bba.s2.eu.hivemq.cloud", 8883)
                    .WithCredentials("Wiktor", _config["MqttPassword"])
                    .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
                    .Build();

                await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                if (!_mqttClient.IsConnected)
                {
                    Console.WriteLine("Reconnecting failed");
                    return;
                }
            }
            
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message.ToString())
                .Build();

            await _mqttClient.PublishAsync(applicationMessage);
        }
    }
}