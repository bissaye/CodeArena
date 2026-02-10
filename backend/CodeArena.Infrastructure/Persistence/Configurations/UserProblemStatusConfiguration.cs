using CodeArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeArena.Infrastructure.Persistence.Configurations;

public class UserProblemStatusConfiguration : IEntityTypeConfiguration<UserProblemStatus>
{
    public void Configure(EntityTypeBuilder<UserProblemStatus> builder)
    {
        builder.HasKey(ups => new { ups.UserId, ups.ProblemId });

        builder.HasOne(ups => ups.User)
            .WithMany(u => u.ProblemStatuses)
            .HasForeignKey(ups => ups.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ups => ups.Problem)
            .WithMany(p => p.UserStatuses)
            .HasForeignKey(ups => ups.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
