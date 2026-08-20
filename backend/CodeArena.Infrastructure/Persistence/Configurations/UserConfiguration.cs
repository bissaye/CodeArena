using CodeArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeArena.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Username).HasMaxLength(30).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Country).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.Region).HasMaxLength(100);
        builder.Property(u => u.School).HasMaxLength(200);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);

        builder.Property(u => u.Role).HasConversion<string>();
        builder.Property(u => u.NotificationEmailEnabled).HasDefaultValue(true);
    }
}
