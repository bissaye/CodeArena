using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(CodeArenaDbContext db, string uploadsBasePath)
    {
        // Always ensure seed files exist on disk (idempotent)
        await EnsureSeedFilesAsync(uploadsBasePath);

        if (await db.Users.AnyAsync())
            return;

        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Country = "Cameroun",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Username = "alice_yaounde",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"), Country = "Cameroun", Region = "Centre",     School = "ENSPY", Role = UserRole.Participant, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Username = "bob_douala",     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"), Country = "Cameroun", Region = "Littoral",                     Role = UserRole.Participant, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Username = "charlie_bafang", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"), Country = "Cameroun", Region = "Ouest",      School = "UDs",   Role = UserRole.Participant, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Username = "diana_bamenda",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"), Country = "Cameroun", Region = "Nord-Ouest",                   Role = UserRole.Participant, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Username = "moderateur1",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"), Country = "Cameroun",                                          Role = UserRole.Moderator,   IsActive = true, CreatedAt = DateTime.UtcNow },
        };

        await db.Users.AddAsync(admin);
        await db.Users.AddRangeAsync(users);
        await db.SaveChangesAsync();

        // --- Compétition 1 : terminée (Sprint 2 — historique / classement) ---
        var finishedCompetitionId = Guid.NewGuid();
        var finishedCompetition = new Competition
        {
            Id = finishedCompetitionId,
            Name = "CodeArena Open 2026 — Édition Inauguration",
            StartDate = DateTime.UtcNow.AddDays(-1),
            Duration = TimeSpan.FromHours(3),
            Status = CompetitionStatus.Finished,
            CreatedByUserId = adminId,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        // --- Compétition 2 : en cours (Sprint 3 — tests soumission) ---
        var ongoingCompetitionId = Guid.NewGuid();
        var ongoingCompetition = new Competition
        {
            Id = ongoingCompetitionId,
            Name = "CodeArena Challenge Sprint 3 — En cours",
            StartDate = DateTime.UtcNow.AddHours(-1),
            Duration = TimeSpan.FromHours(10),
            Status = CompetitionStatus.Ongoing,
            CreatedByUserId = adminId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await db.Competitions.AddRangeAsync(finishedCompetition, ongoingCompetition);
        await db.SaveChangesAsync();

        // Exercices de la compétition terminée
        await db.Problems.AddRangeAsync(
            new Problem
            {
                Id = Guid.NewGuid(),
                CompetitionId = finishedCompetitionId,
                Title = "Somme de deux entiers",
                Body = "## Description\n\nÉtant donné deux entiers **A** et **B**, calculez leur somme.\n\n## Entrée\n\nDeux entiers A et B séparés par un espace (0 ≤ A, B ≤ 10^9).\n\n## Sortie\n\nUn entier : la somme A + B.\n\n## Exemple\n\nEntrée : `3 7`\nSortie : `10`",
                Points = 100,
                InputFileUrl  = "uploads/seed/problem1_input.txt",
                OutputFileUrl = "uploads/seed/problem1_output.txt",
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Problem
            {
                Id = Guid.NewGuid(),
                CompetitionId = finishedCompetitionId,
                Title = "Nombre palindrome",
                Body = "## Description\n\nDéterminez si un nombre entier est un palindrome.\n\n## Entrée\n\nUn entier N (0 ≤ N ≤ 10^9).\n\n## Sortie\n\n`OUI` si N est un palindrome, `NON` sinon.\n\n## Exemple\n\nEntrée : `121`\nSortie : `OUI`",
                Points = 200,
                InputFileUrl  = "uploads/seed/problem2_input.txt",
                OutputFileUrl = "uploads/seed/problem2_output.txt",
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            }
        );

        // Exercices de la compétition en cours
        await db.Problems.AddRangeAsync(
            new Problem
            {
                Id = Guid.NewGuid(),
                CompetitionId = ongoingCompetitionId,
                Title = "Maximum de trois entiers",
                Body = "## Description\n\nÉtant donné trois entiers **A**, **B** et **C**, trouvez le plus grand.\n\n## Entrée\n\nTrois entiers A, B, C séparés par des espaces (0 ≤ A, B, C ≤ 10^9).\n\n## Sortie\n\nLe plus grand des trois entiers.\n\n## Exemple\n\nEntrée : `3 9 5`\nSortie : `9`",
                Points = 100,
                InputFileUrl  = "uploads/seed/problem3_input.txt",
                OutputFileUrl = "uploads/seed/problem3_output.txt",
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Problem
            {
                Id = Guid.NewGuid(),
                CompetitionId = ongoingCompetitionId,
                Title = "Fibonacci",
                Body = "## Description\n\nCalculez le N-ième terme de la suite de Fibonacci (0-indexé).\n\n## Entrée\n\nUn entier N (0 ≤ N ≤ 40).\n\n## Sortie\n\nF(N), le N-ième terme de la suite (F(0)=0, F(1)=1, F(N)=F(N-1)+F(N-2)).\n\n## Exemple\n\nEntrée : `7`\nSortie : `13`",
                Points = 200,
                InputFileUrl  = "uploads/seed/problem4_input.txt",
                OutputFileUrl = "uploads/seed/problem4_output.txt",
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task EnsureSeedFilesAsync(string uploadsBasePath)
    {
        var seedDir = Path.Combine(uploadsBasePath, "seed");
        Directory.CreateDirectory(seedDir);

        var files = new Dictionary<string, string>
        {
            ["problem1_input.txt"]  = "3 7\n",
            ["problem1_output.txt"] = "10\n",
            ["problem2_input.txt"]  = "121\n",
            ["problem2_output.txt"] = "OUI\n",
            ["problem3_input.txt"]  = "3 9 5\n",
            ["problem3_output.txt"] = "9\n",
            ["problem4_input.txt"]  = "7\n",
            ["problem4_output.txt"] = "13\n",
        };

        foreach (var (fileName, content) in files)
        {
            var path = Path.Combine(seedDir, fileName);
            if (!File.Exists(path))
                await File.WriteAllTextAsync(path, content);
        }
    }
}
