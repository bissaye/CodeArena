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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CodeArenaDbContext).Assembly);
    }
}
