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
using BattleGrid.Domain;
using BattleGrid.Domain.Entities;
using BattleGrid.API.BackgroundServices;
using BattleGrid.API.Hubs;
using BattleGrid.API.Matchmaking;
using BattleGrid.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

    // Add CORS policy for deployment
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", policy =>
    {
        // Since we are using cookies/credentials with SignalR, we MUST specify exact URLs:
        policy//.AllowAnyOrigin()
              .WithOrigins("http://localhost:4746",
                           "http://127.0.0.1:4746",
                           "https://localhost:4745",
                           "https://127.0.0.1:4745",
                           "http://api:8080",
                           "https://api:8080",
                           "http://web:8080",
                           "https://web:8080")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // This accepts the cookies to be sent along with the request
    });
});

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
                    return Task.CompletedTask;
                }

                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue("AccessToken", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddSingleton<MatchmakingCoordinator>();
builder.Services.AddHostedService<MatchmakingBackgroundService>();
builder.Services.AddHostedService<StaleMatchCleanupBackgroundService>();
builder.Services.AddHostedService<ExpiredBanCleanupBackgroundService>();

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
builder.Services.AddScoped<IAuthServices, AuthServices>();              // Some but not all Session AND User table related services such as; register, login, logout, and verify password
builder.Services.AddScoped<IAdminServices, AdminServices>();            // Admin related services such as; banning players, and resetting leaderboards
builder.Services.AddScoped<IUserServices, UserServices>();              // Rest of the User table related services. Also, include PlayerStat table related services here
//aaa builder.Services.AddScoped<ISessionServices, SessionServices>();  // Rest of the Session table related services. Mostly, HTTP GET methods
builder.Services.AddScoped<IShipTypeServices, ShipTypeServices>();
builder.Services.AddScoped<IPlayerStatSeasonService, PlayerStatSeasonService>();
builder.Services.Configure<RatingFormulaSettings>(builder.Configuration.GetSection(RatingFormulaSettings.SectionName));
builder.Services.AddScoped<IMatchServices, MatchServices>();
builder.Services.AddScoped<IShipPlacementServices, ShipPlacementServices>();
builder.Services.AddSingleton<InMemoryMatchGameService>();
builder.Services.AddScoped<IReplayServices, ReplayServices>();
builder.Services.AddScoped<IBanListServices, BanListServices>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// This is commented out in case the hosting provider handles SSL termination at he proxy level
// Leaving enabled can sometimes cause infinite redirect loops in free Docker containers
//aaa app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowBlazorFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Hub URLs for queue and game
app.MapHub<QueueHub>("/queue");
app.MapHub<GameHub>("/game/{matchId:int}");

// Standart response from API base URL. Will be used to detect API status in frontend
app.MapGet("/", () => "BattleGrid API is running.");

app.MapGet("/health", async (BattleGridDbContext db, CancellationToken cancellationToken) =>
{
    var dbOk = false;
    try
    {
        dbOk = await db.Database.CanConnectAsync(cancellationToken);
    }
    catch
    {
        dbOk = false;
    }

    return Results.Json(new BattleGrid.Contracts.ResponseDtos.HealthResponseDto
    {
        Status = "ok",
        Database = dbOk ? "connected" : "unreachable"
    });
});

app.Run();