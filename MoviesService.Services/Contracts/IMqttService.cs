namespace MoviesService.Services.Contracts;

public interface IMqttService
{
    Task SendNotificationAsync(string topic, object message);
}