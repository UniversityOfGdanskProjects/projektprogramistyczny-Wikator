using MoviesApi.Repository;
using MoviesApi.Repository.IRepository;
using Neo4j.Driver;

namespace MoviesApi.Extensions
{
	public static class ApplicationServiceExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services,
			IConfiguration config)
		{
			string server = config.GetConnectionString("Server")
				?? throw new Exception("Connection string not found");

			string userName = config.GetConnectionString("UserName")
				?? throw new Exception("Connection string not found");

			string password = config.GetConnectionString("Password")
				?? throw new Exception("Connection string not found");

			services.AddSingleton(GraphDatabase.Driver(server, AuthTokens.Basic(userName, password)));
			services.AddScoped<IMovieRepository, MovieRepository>();

			return services;
		}
	}
}
