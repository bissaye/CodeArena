using System.Text;
using CodeArena.API.HostedServices;
using CodeArena.API.Hubs;
using CodeArena.Application;
using CodeArena.Infrastructure;
using CodeArena.Infrastructure.Jobs;
using CodeArena.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Application (services, validators)
builder.Services.AddApplication();

// Infrastructure (EF Core + PostgreSQL + Redis + JwtService + PasswordHasher)
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// CORS
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:4200";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// JWT Authentication — with SignalR WebSocket support (token from query string)
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET is not configured.");
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        // SignalR WebSocket connections cannot send an Authorization header — read token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ModeratorOrAdmin", policy =>
        policy.RequireRole("Moderator", "Admin"));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeArena API",
        Version = "v1",
        Description = "API de la plateforme CodeArena Cameroun"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Entrez votre JWT token"
    });
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            []
        }
    });
});

// Hangfire (jobs enqueued by services are processed here and in the Worker)
var pgConn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(pgConn)));

// Run Hangfire server in the API process (handles jobs if Worker is not running)
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 3;
    options.ServerName = "codearena-api";
});

// SignalR with Redis backplane (scale-out ready for multi-instance)
var redisConn = builder.Configuration["REDIS_CONNECTION"] ?? "redis:6379";
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConn);

// Redis notification relay — subscribes to Redis pub/sub and pushes to connected SignalR clients
builder.Services.AddHostedService<RedisNotificationRelay>();

// File upload size limit
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 12 * 1024 * 1024;
});

var app = builder.Build();

// Auto-migrate + seed
var uploadsPath = app.Configuration["UPLOADS_PATH"]
    ?? Path.Combine(app.Environment.ContentRootPath, "uploads");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CodeArenaDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, uploadsPath);
}

// Register Hangfire recurring job (competition status transitions, every minute)
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<CompetitionStatusJob>(
        "competition-status-update",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Minutely());
}

// Serve uploaded files (avatars, inputs)
if (Directory.Exists(uploadsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeArena API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SignalR hub endpoint
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire dashboard — Admin only, internal use
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()],
    IgnoreAntiforgeryToken = true
});

app.Run();
