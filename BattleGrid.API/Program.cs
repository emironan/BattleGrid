using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NSwag.AspNetCore;
using System.Text;
using BattleGrid.Application.Interfaces;
using BattleGrid.Application.Services;
using BattleGrid.Application.Helpers;
using BattleGrid.Infrastructure.Data;
using BattleGrid.Domain.Entities;
using BattleGrid.API.Hubs;
using BattleGrid.API.Matchmaking;
using BattleGrid.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<BattleGridDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("BattleGridDB");
    if (string.IsNullOrWhiteSpace(cs))
    {
        throw new InvalidOperationException("Connection string 'BattleGridDB' is missing.");
    }
    options.UseNpgsql(cs)
           //.EnableSensitiveDataLogging()
           //.LogTo(Console.WriteLine)
           ;
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var signingKey = builder.Configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey is missing.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(15),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/queue") || path.StartsWithSegments("/game")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddSingleton<MatchmakingCoordinator>();
builder.Services.AddHostedService<MatchmakingBackgroundService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BattleGrid API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddScoped<IJwtHelper, JwtHelper>();
builder.Services.AddScoped<INormalizationHelper, NormalizationHelper>();

//aaa TODO: Un-comment them as we implement them. Some might be unnecessary and be deleted if not implemented at all
builder.Services.AddScoped<IAuthServices, AuthServices>(); // Some but not all Session AND User table related services such as; register, login, logout, and verify password
builder.Services.AddScoped<IAdminServices, AdminServices>(); // Admin related services such as; banning players, and resetting leaderboards
builder.Services.AddScoped<IUserServices, UserServices>(); // Rest of the User table related services. Also, include PlayerStat table related services here
//aaa builder.Services.AddScoped<ISessionServices, SessionServices>(); // Rest of the Session table related services. Mostly, HTTP GET methods
builder.Services.AddScoped<IShipTypeServices, ShipTypeServices>();
builder.Services.AddScoped<IPlayerStatSeasonService, PlayerStatSeasonService>();
builder.Services.AddScoped<IMatchServices, MatchServices>();
builder.Services.AddScoped<IShipPlacementServices, ShipPlacementServices>();
builder.Services.AddSingleton<InMemoryMatchGameService>();
//aaa builder.Services.AddScoped<IReplayServices, ReplayServices>(); // Spectator table related services AND for normal users to replay their own matches
//aaa builder.Services.AddScoped<IMatchMakingServices, MatchmakingServices>(); // If we can handle this in-memory with SignalR, we may get rid of MatchmakingQueue table and this service
//aaa builder.Services.AddScoped<IMatchMoveServices, MatchMoveServices>();
//aaa builder.Services.AddScoped<IBanListServices, BanListServices>(); // Rest of the BanList table related services. Mostly HTTP GET methods for checking if a player is banned, when their ban is going to be lifted, or if it is permanent etc

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<QueueHub>("/queue");
app.MapHub<GameHub>("/game/{matchId:int}");

app.MapGet("/", () => "BattleGrid API is running.");

app.Run();