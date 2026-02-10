using CodeArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Competition> Competitions { get; }
    DbSet<Problem> Problems { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<UserProblemStatus> UserProblemStatuses { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
