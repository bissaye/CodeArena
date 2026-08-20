using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Infrastructure.Persistence;

public class CodeArenaDbContext(DbContextOptions<CodeArenaDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<UserProblemStatus> UserProblemStatuses => Set<UserProblemStatus>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CodeArenaDbContext).Assembly);
    }
}
