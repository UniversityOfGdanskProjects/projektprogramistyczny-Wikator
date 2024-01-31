using MoviesService.DataAccess.Repositories;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Services;
using MoviesService.Services.Contracts;
using Neo4j.Driver;

namespace MoviesService.Extensions;

public static class ApplicationServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        var server = config.GetConnectionString("Server")
                     ?? throw new Exception("Connection string not found");

        var userName = config.GetConnectionString("UserName")
                       ?? throw new Exception("Connection string not found");

        var password = config.GetConnectionString("Password")
                       ?? throw new Exception("Connection string not found");

        services.AddSingleton(GraphDatabase.Driver(server, AuthTokens.Basic(userName, password)));
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IActorRepository, ActorRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IFavouriteRepository, FavouriteRepository>();
        services.AddScoped<IIgnoresRepository, IgnoresRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddSingleton<IMessageRepository, MessageRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IMqttService, MqttService>();
    }
}