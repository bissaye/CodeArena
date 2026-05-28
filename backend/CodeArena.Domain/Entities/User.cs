using CodeArena.Domain.Enums;

namespace CodeArena.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? School { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Participant;
    public int TotalScore { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PromotedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? PasswordResetRequestedAt { get; set; }
    public bool NotificationEmailEnabled { get; set; } = true;

    public string Level => TotalScore switch
    {
        >= 1500 => "Expert",
        >= 500  => "Avancé",
        >= 100  => "Intermédiaire",
        _       => "Débutant"
    };

    public ICollection<Submission> Submissions { get; set; } = [];
    public ICollection<UserProblemStatus> ProblemStatuses { get; set; } = [];
    public ICollection<EmailVerification> EmailVerifications { get; set; } = [];
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<UserBadge> UserBadges { get; set; } = [];
}
