using CodeArena.Domain.Enums;

namespace CodeArena.Domain.Entities;

public class Badge
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public BadgeCondition Condition { get; set; }

    public ICollection<UserBadge> UserBadges { get; set; } = [];
}
