using CodeArena.Application;
using CodeArena.Infrastructure;
using CodeArena.Infrastructure.Jobs;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Application services (validators, all IService registrations)
builder.Services.AddApplication();

// Infrastructure (EF Core + Redis + INotificationPusher + IEmailService, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Hangfire — processes jobs stored in PostgreSQL
var pgConn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(pgConn)));

// Dedicated Hangfire server — higher worker count for background processing
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10;
    options.ServerName = "codearena-worker";
    options.Queues = ["default", "emails", "badges", "notifications"];
});

var app = builder.Build();

// Register recurring job (competition status transitions every minute)
RecurringJob.AddOrUpdate<CompetitionStatusJob>(
    "competition-status-update",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Minutely());

// Hangfire dashboard — internal only, no auth required (not exposed publicly)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [],
    IgnoreAntiforgeryToken = true
});

app.Run();
