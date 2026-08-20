using CodeArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeArena.Infrastructure.Persistence.Configurations;

public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Body).IsRequired();
        builder.Property(p => p.InputFileUrl).HasMaxLength(500).IsRequired();
        builder.Property(p => p.OutputFileUrl).HasMaxLength(500).IsRequired();

        builder.HasOne(p => p.Competition)
            .WithMany(c => c.Problems)
            .HasForeignKey(p => p.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LastModifiedBy)
            .WithMany()
            .HasForeignKey(p => p.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
