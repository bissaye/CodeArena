using System.Text;
using CodeArena.Application;
using CodeArena.Infrastructure;
using CodeArena.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Application (services, validators)
builder.Services.AddApplication();

// Infrastructure (EF Core + PostgreSQL + JwtService + PasswordHasher)
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

// JWT Authentication
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ModeratorOrAdmin", policy =>
        policy.RequireRole("Moderator", "Admin"));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// Swagger / OpenAPI (Swashbuckle 10 + Microsoft.OpenApi 2.x)
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

// Memory Cache (leaderboard)
builder.Services.AddMemoryCache();

// Multipart file upload limits (max 5 MB per file, 12 MB total)
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

// Serve uploaded files (avatars, inputs) as static files at /uploads/*
// Files are stored outside webroot — sécurité : GUID names, pas de path traversal
var uploadsPhysicalPath = uploadsPath;
if (Directory.Exists(uploadsPhysicalPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPhysicalPath),
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

app.Run();
