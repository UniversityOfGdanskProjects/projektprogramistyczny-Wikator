using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoviesApi.Extensions;
using System.Text;
using MoviesApi.Configurations;
using MoviesApi.DatabaseSetup;
using MoviesApi.Middleware;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

var tokenKey = builder.Configuration["TokenKey"]
    ?? throw new Exception("Token key not found");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
			ValidIssuer = "https://moviesapiwebtest.azurewebsites.net",
			ValidateAudience = false
		};
	});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(b => b
	.AllowAnyHeader()
	.AllowAnyMethod()
	.AllowCredentials()
	.WithOrigins("http://localhost:3000"));

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<UserExistsInDatabaseMiddleware>();
app.UseMiddleware<LogUserActivityMiddleware>();
app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var driver = services.GetRequiredService<IDriver>();
var setup = new Setup(driver);
await setup.SetupJobs();
await setup.CreateAdmin(app.Configuration);
await setup.SeedGenres();

app.Run();
