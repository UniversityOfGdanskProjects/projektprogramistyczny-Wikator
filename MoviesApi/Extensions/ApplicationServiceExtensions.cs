﻿using MoviesApi.Repository;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Extensions;

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
		services.AddScoped<IMovieRepository, MovieRepository>();
		services.AddScoped<IActorRepository, ActorRepository>();
		services.AddScoped<IAccountRepository, AccountRepository>();
		services.AddScoped<ITokenService, TokenService>();
	}
}