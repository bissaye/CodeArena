using CodeArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeArena.Infrastructure.Persistence.Configurations;

public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>();

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.LastModifiedBy)
            .WithMany()
            .HasForeignKey(c => c.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.EndDate);
    }
}
