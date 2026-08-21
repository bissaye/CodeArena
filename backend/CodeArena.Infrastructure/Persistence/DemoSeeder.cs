using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Infrastructure.Persistence;

/// <summary>
/// Demo data seeder for client presentations.
/// Triggered via POST /api/admin/seed-demo (AdminOnly).
/// Idempotent: skips silently if demo_participant already exists.
/// </summary>
public static class DemoSeeder
{
    #region Static Data

    private static readonly string[] MaleFirstNames =
    [
        "jean", "pierre", "paul", "emmanuel", "daniel", "francois", "michel", "claude",
        "victor", "alain", "serge", "christophe", "thierry", "martin", "patrice", "samuel",
        "ibrahim", "moussa", "amadou", "yannick", "rodrigue", "cedric", "boris", "armand",
        "joel", "kevin", "patrick", "fabrice", "gilbert", "herve", "alex", "etienne",
        "remy", "bertrand", "aurelien"
    ];

    private static readonly string[] FemaleFirstNames =
    [
        "marie", "anne", "claire", "christine", "marianne", "sylvie", "josephine", "fatima",
        "aminata", "astrid", "carole", "sandrine", "patricia", "stephanie", "veronique",
        "pauline", "isabelle", "nathalie", "laure", "celine", "fatoumata", "aissatou",
        "ngozi", "precious", "aurore", "helene", "rachel", "esther", "ruth", "alice"
    ];

    private static readonly string[] LastNames =
    [
        "nkomo", "mbeki", "fotso", "nguele", "tchoupo", "atangana", "eyebe", "ngono",
        "talla", "mbarga", "essomba", "owona", "ondoua", "ndjom", "zang", "mvondo",
        "amvela", "nkoa", "toukam", "fongang", "kamdoum", "kamdem", "sigha", "feudjio",
        "foka", "kana", "mbem", "tagne", "nkuissi", "bello", "yaya", "hamadou",
        "mahamat", "oumarou", "bikele", "manga", "meka", "nga", "nlend", "dongmo"
    ];

    private static readonly (string Region, string?[] Schools, int Count)[] RegionBuckets =
    [
        ("Centre",       ["Université de Yaoundé I", "Université de Yaoundé II", "ENSP Yaoundé", "SUP'TIC Yaoundé", "ESIG Yaoundé"], 35),
        ("Littoral",     ["IUT de Douala", "Université de Douala", "ISTDI Douala"], 30),
        ("Ouest",        ["Université de Dschang", "IUT Fotso Victor"], 20),
        ("Extrême-Nord", [null], 3),
        ("Nord",         [null], 3),
        ("Est",          [null], 2),
        ("Adamaoua",     [null], 2),
        ("Sud-Ouest",    [null], 2),
        ("Nord-Ouest",   [null], 3),
    ];

    private record ProblemTemplate(string Title, string Body, int Points, string Input, string Output);

    private static readonly ProblemTemplate[] Templates =
    [
        /* 0 */ new("Somme de deux entiers",
            "## Description\n\nÉtant donné deux entiers **A** et **B**, calculez leur somme.\n\n## Format d'entrée\n\nDeux entiers A et B séparés par un espace.\n\n**Contraintes :** 0 ≤ A, B ≤ 10⁹\n\n## Format de sortie\n\nUn entier : la valeur de A + B.\n\n## Exemple\n\n**Entrée :**\n```\n3 7\n```\n**Sortie :**\n```\n10\n```",
            100, "3 7\n", "10\n"),

        /* 1 */ new("Nombre premier",
            "## Description\n\nDéterminez si un entier **N** est un **nombre premier**.\n\n## Format d'entrée\n\nUn entier N.\n\n**Contraintes :** 2 ≤ N ≤ 10⁶\n\n## Format de sortie\n\n`OUI` si N est premier, `NON` sinon.\n\n## Exemple\n\n**Entrée :**\n```\n17\n```\n**Sortie :**\n```\nOUI\n```",
            150, "17\n", "OUI\n"),

        /* 2 */ new("Détection d'anagramme",
            "## Description\n\nDéterminez si deux mots sont des **anagrammes** (mêmes lettres, ordre différent).\n\n## Format d'entrée\n\nDeux mots sur deux lignes distinctes (lettres minuscules, longueur ≤ 100).\n\n## Format de sortie\n\n`OUI` si les mots sont anagrammes, `NON` sinon.\n\n## Exemple\n\n**Entrée :**\n```\nlisten\nsilent\n```\n**Sortie :**\n```\nOUI\n```",
            150, "listen\nsilent\n", "OUI\n"),

        /* 3 */ new("FizzBuzz",
            "## Description\n\nAffichez les nombres de 1 à N avec les règles **FizzBuzz** :\n- Multiple de 3 uniquement → `Fizz`\n- Multiple de 5 uniquement → `Buzz`\n- Multiple de 15 → `FizzBuzz`\n- Sinon → le nombre\n\n## Format d'entrée\n\nUn entier N (1 ≤ N ≤ 100).\n\n## Format de sortie\n\nN lignes selon les règles FizzBuzz.\n\n## Exemple\n\n**Entrée :**\n```\n5\n```\n**Sortie :**\n```\n1\n2\nFizz\n4\nBuzz\n```",
            100, "15\n", "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz\n"),

        /* 4 */ new("PGCD de deux entiers",
            "## Description\n\nCalculez le **Plus Grand Commun Diviseur** de A et B via l'algorithme d'Euclide.\n\n## Format d'entrée\n\nDeux entiers A et B séparés par un espace.\n\n**Contraintes :** 1 ≤ A, B ≤ 10⁹\n\n## Format de sortie\n\nLe PGCD de A et B.\n\n## Exemple\n\n**Entrée :**\n```\n48 18\n```\n**Sortie :**\n```\n6\n```",
            150, "48 18\n", "6\n"),

        /* 5 */ new("Inversion de chaîne",
            "## Description\n\nÉcrivez l'**inverse** d'une chaîne de caractères donnée.\n\n## Format d'entrée\n\nUne chaîne de caractères (longueur ≤ 1000).\n\n## Format de sortie\n\nLa chaîne inversée caractère par caractère.\n\n## Exemple\n\n**Entrée :**\n```\nCameroun\n```\n**Sortie :**\n```\nnuoremaC\n```",
            100, "Cameroun\n", "nuoremaC\n"),

        /* 6 */ new("Tri croissant",
            "## Description\n\nTriez un tableau d'entiers dans l'**ordre croissant**.\n\n## Format d'entrée\n\nLigne 1 : N (nombre d'éléments).\nLigne 2 : N entiers séparés par des espaces.\n\n**Contraintes :** 1 ≤ N ≤ 1000\n\n## Format de sortie\n\nLes N entiers triés, séparés par des espaces.\n\n## Exemple\n\n**Entrée :**\n```\n5\n64 25 12 22 11\n```\n**Sortie :**\n```\n11 12 22 25 64\n```",
            200, "5\n64 25 12 22 11\n", "11 12 22 25 64\n"),

        /* 7 */ new("Distance de Manhattan",
            "## Description\n\nCalculez la **distance de Manhattan** entre deux points (x₁,y₁) et (x₂,y₂).\n\nDistance = |x₁ − x₂| + |y₁ − y₂|\n\n## Format d'entrée\n\nQuatre entiers x₁ y₁ x₂ y₂ séparés par des espaces.\n\n**Contraintes :** -10⁴ ≤ xi, yi ≤ 10⁴\n\n## Format de sortie\n\nLa distance de Manhattan.\n\n## Exemple\n\n**Entrée :**\n```\n1 2 4 6\n```\n**Sortie :**\n```\n7\n```",
            150, "1 2 4 6\n", "7\n"),

        /* 8 */ new("Comptage de voyelles",
            "## Description\n\nComptez le nombre de **voyelles** (a, e, i, o, u — insensible à la casse) dans une phrase.\n\n## Format d'entrée\n\nUne ligne de texte (longueur ≤ 500).\n\n## Format de sortie\n\nLe nombre de voyelles.\n\n## Exemple\n\n**Entrée :**\n```\nBonjour tout le monde\n```\n**Sortie :**\n```\n8\n```",
            100, "Bonjour tout le monde\n", "8\n"),

        /* 9 */ new("Factorielle",
            "## Description\n\nCalculez la **factorielle** de N (N! = 1 × 2 × ... × N, avec 0! = 1).\n\n## Format d'entrée\n\nUn entier N.\n\n**Contraintes :** 0 ≤ N ≤ 12\n\n## Format de sortie\n\nLa valeur de N!\n\n## Exemple\n\n**Entrée :**\n```\n10\n```\n**Sortie :**\n```\n3628800\n```",
            200, "10\n", "3628800\n"),

        /* 10 */ new("Conversion décimale-binaire",
            "## Description\n\nConvertissez un entier décimal en sa représentation **binaire** (base 2).\n\n## Format d'entrée\n\nUn entier N.\n\n**Contraintes :** 0 ≤ N ≤ 10⁹\n\n## Format de sortie\n\nLa représentation binaire de N, sans zéros non significatifs.\n\n## Exemple\n\n**Entrée :**\n```\n42\n```\n**Sortie :**\n```\n101010\n```",
            200, "42\n", "101010\n"),

        /* 11 */ new("Compter les nombres pairs",
            "## Description\n\nComptez le nombre d'entiers **pairs** dans un tableau.\n\n## Format d'entrée\n\nLigne 1 : N (nombre d'éléments).\nLigne 2 : N entiers séparés par des espaces.\n\n**Contraintes :** 1 ≤ N ≤ 10⁵\n\n## Format de sortie\n\nLe nombre d'entiers pairs.\n\n## Exemple\n\n**Entrée :**\n```\n6\n1 2 3 4 5 6\n```\n**Sortie :**\n```\n3\n```",
            100, "6\n1 2 3 4 5 6\n", "3\n"),

        /* 12 */ new("Maximum d'un tableau",
            "## Description\n\nTrouvez la valeur **maximale** dans un tableau d'entiers.\n\n## Format d'entrée\n\nLigne 1 : N (nombre d'éléments).\nLigne 2 : N entiers séparés par des espaces.\n\n**Contraintes :** 1 ≤ N ≤ 10⁵\n\n## Format de sortie\n\nL'élément le plus grand.\n\n## Exemple\n\n**Entrée :**\n```\n5\n3 8 1 9 2\n```\n**Sortie :**\n```\n9\n```",
            100, "5\n3 8 1 9 2\n", "9\n"),

        /* 13 */ new("Détection de palindrome",
            "## Description\n\nDéterminez si une chaîne est un **palindrome** (identique dans les deux sens de lecture).\n\n## Format d'entrée\n\nUne chaîne de caractères en minuscules (longueur ≤ 1000).\n\n## Format de sortie\n\n`OUI` si c'est un palindrome, `NON` sinon.\n\n## Exemple\n\n**Entrée :**\n```\nracecar\n```\n**Sortie :**\n```\nOUI\n```",
            150, "racecar\n", "OUI\n"),

        /* 14 */ new("Somme des chiffres",
            "## Description\n\nCalculez la **somme des chiffres** composant un entier.\n\n## Format d'entrée\n\nUn entier N.\n\n**Contraintes :** 0 ≤ N ≤ 10⁹\n\n## Format de sortie\n\nLa somme des chiffres décimaux de N.\n\n## Exemple\n\n**Entrée :**\n```\n12345\n```\n**Sortie :**\n```\n15\n```",
            100, "12345\n", "15\n"),

        /* 15 */ new("Puissance entière",
            "## Description\n\nCalculez A^B (A à la puissance B) par **exponentiation itérative** (sans utiliser `pow`).\n\n## Format d'entrée\n\nDeux entiers A et B séparés par un espace.\n\n**Contraintes :** 1 ≤ A ≤ 100, 0 ≤ B ≤ 10\n\n## Format de sortie\n\nLa valeur de A^B.\n\n## Exemple\n\n**Entrée :**\n```\n2 10\n```\n**Sortie :**\n```\n1024\n```",
            200, "2 10\n", "1024\n"),

        /* 16 */ new("Comptage de mots",
            "## Description\n\nComptez le nombre de **mots** dans une phrase (séparés par des espaces).\n\n## Format d'entrée\n\nUne ligne de texte (longueur ≤ 500).\n\n## Format de sortie\n\nLe nombre de mots.\n\n## Exemple\n\n**Entrée :**\n```\nLe Cameroun est magnifique\n```\n**Sortie :**\n```\n4\n```",
            100, "Le Cameroun est magnifique\n", "4\n"),

        /* 17 */ new("Suite arithmétique",
            "## Description\n\nAffichez les termes d'une **suite arithmétique** de premier terme D, de dernier terme maximal F et de raison R.\n\n## Format d'entrée\n\nTrois entiers D F R séparés par des espaces.\n\n**Contraintes :** D ≤ F, 1 ≤ R ≤ 100\n\n## Format de sortie\n\nLes termes de la suite séparés par des espaces.\n\n## Exemple\n\n**Entrée :**\n```\n2 14 3\n```\n**Sortie :**\n```\n2 5 8 11 14\n```",
            150, "2 14 3\n", "2 5 8 11 14\n"),

        /* 18 */ new("Fibonacci",
            "## Description\n\nCalculez le **N-ième terme** de la suite de Fibonacci.\n\nDéfinition : F(0) = 0, F(1) = 1, F(N) = F(N−1) + F(N−2) pour N ≥ 2.\n\n## Format d'entrée\n\nUn entier N.\n\n**Contraintes :** 0 ≤ N ≤ 40\n\n## Format de sortie\n\nLa valeur de F(N).\n\n## Exemple\n\n**Entrée :**\n```\n7\n```\n**Sortie :**\n```\n13\n```",
            200, "7\n", "13\n"),

        /* 19 */ new("Rotation de tableau",
            "## Description\n\nEffectuez une **rotation à droite** de K positions sur un tableau.\n\nLes K derniers éléments reviennent au début du tableau.\n\n## Format d'entrée\n\nLigne 1 : N K (taille et nombre de rotations).\nLigne 2 : N entiers séparés par des espaces.\n\n**Contraintes :** 1 ≤ N ≤ 1000, 0 ≤ K ≤ N\n\n## Format de sortie\n\nLe tableau après K rotations à droite, éléments séparés par des espaces.\n\n## Exemple\n\n**Entrée :**\n```\n5 2\n1 2 3 4 5\n```\n**Sortie :**\n```\n4 5 1 2 3\n```",
            250, "5 2\n1 2 3 4 5\n", "4 5 1 2 3\n"),

        /* 20 */ new("Intersection de tableaux",
            "## Description\n\nTrouvez les éléments **communs** à deux tableaux triés (sans doublons dans la sortie).\n\n## Format d'entrée\n\nLigne 1 : N₁ (taille du premier tableau).\nLigne 2 : N₁ entiers triés.\nLigne 3 : N₂ (taille du deuxième tableau).\nLigne 4 : N₂ entiers triés.\n\n## Format de sortie\n\nLes éléments communs triés, séparés par des espaces.\n\n## Exemple\n\n**Entrée :**\n```\n3\n1 2 3\n3\n2 3 4\n```\n**Sortie :**\n```\n2 3\n```",
            300, "3\n1 2 3\n3\n2 3 4\n", "2 3\n"),

        /* 21 */ new("Aire d'un triangle",
            "## Description\n\nCalculez l'**aire** d'un triangle à partir de sa base et sa hauteur.\n\nFormule : Aire = (base × hauteur) / 2\n\n## Format d'entrée\n\nDeux entiers base et hauteur séparés par un espace.\n\n**Contraintes :** 1 ≤ base, hauteur ≤ 10⁴\n\n## Format de sortie\n\nL'aire (entier si exact, sinon une décimale).\n\n## Exemple\n\n**Entrée :**\n```\n3 4\n```\n**Sortie :**\n```\n6\n```",
            150, "3 4\n", "6\n"),

        /* 22 */ new("Compression RLE",
            "## Description\n\nAppliquez la compression **Run-Length Encoding (RLE)** à une chaîne.\n\nChaque séquence de caractères consécutifs identiques est codée : `{count}{char}`.\n\n## Format d'entrée\n\nUne chaîne de lettres majuscules (longueur ≤ 1000).\n\n## Format de sortie\n\nLa chaîne compressée en RLE.\n\n## Exemple\n\n**Entrée :**\n```\nAAAABBBCCDA\n```\n**Sortie :**\n```\n4A3B2C1D1A\n```",
            300, "AAAABBBCCDA\n", "4A3B2C1D1A\n"),

        /* 23 */ new("Sous-suite croissante maximale",
            "## Description\n\nTrouvez la **longueur de la plus longue sous-suite strictement croissante** (LIS) d'un tableau.\n\nUne sous-suite est obtenue en retirant des éléments sans changer l'ordre des restants.\n\n## Format d'entrée\n\nLigne 1 : N (nombre d'éléments).\nLigne 2 : N entiers séparés par des espaces.\n\n**Contraintes :** 1 ≤ N ≤ 1000\n\n## Format de sortie\n\nLa longueur de la LIS.\n\n## Exemple\n\n**Entrée :**\n```\n8\n10 9 2 5 3 7 101 18\n```\n**Sortie :**\n```\n4\n```",
            500, "8\n10 9 2 5 3 7 101 18\n", "4\n"),

        /* 24 */ new("Chemin minimal dans une grille",
            "## Description\n\nTrouvez le **coût minimal** pour traverser une grille N×M du coin supérieur gauche au coin inférieur droit.\n\nDéplacements autorisés : droite ou bas uniquement. Le coût est la somme des valeurs de toutes les cellules visitées.\n\n## Format d'entrée\n\nLigne 1 : N M (dimensions).\nLignes suivantes : M entiers par ligne.\n\n**Contraintes :** 1 ≤ N, M ≤ 100, 1 ≤ coût ≤ 100\n\n## Format de sortie\n\nLe coût minimal du chemin.\n\n## Exemple\n\n**Entrée :**\n```\n3 3\n1 3 1\n1 5 1\n4 2 1\n```\n**Sortie :**\n```\n7\n```",
            500, "3 3\n1 3 1\n1 5 1\n4 2 1\n", "7\n"),
    ];

    // Template indices assigned to each of the 20 competitions (5 problems each)
    private static readonly int[][] CompProblemTemplates =
    [
        [0, 1, 6, 10, 23],   // comp 0  — Finished Jan
        [2, 4, 7, 11, 20],   // comp 1  — Finished Feb
        [3, 5, 8, 14, 21],   // comp 2  — Finished Feb ENSPY
        [9, 12, 13, 16, 24], // comp 3  — Finished Mar
        [0, 4, 15, 18, 22],  // comp 4  — Finished Apr
        [1, 6, 11, 17, 23],  // comp 5  — Finished May
        [2, 8, 13, 19, 24],  // comp 6  — Finished Jun
        [3, 7, 12, 16, 21],  // comp 7  — Finished Jul
        [0, 5, 10, 19, 22],  // comp 8  — Ongoing main
        [1, 4, 9, 17, 20],   // comp 9  — Ongoing Yaoundé
        [2, 6, 14, 18, 23],  // comp 10 — Ongoing Douala
        [3, 8, 11, 15, 24],  // comp 11 — Ongoing Python
        [5, 7, 12, 16, 21],  // comp 12 — Ongoing Web
        [0, 9, 13, 20, 22],  // comp 13 — Upcoming
        [1, 5, 11, 18, 23],  // comp 14 — Upcoming IA
        [4, 8, 14, 19, 24],  // comp 15 — Upcoming Mobile
        [3, 6, 10, 17, 21],  // comp 16 — Upcoming Cyber
        [0, 2, 7, 15, 22],   // comp 17 — Draft
        [1, 9, 13, 16, 20],  // comp 18 — Draft
        [4, 5, 12, 14, 23],  // comp 19 — Draft
    ];

    #endregion

    public static async Task SeedDemoAsync(CodeArenaDbContext db, string uploadsBasePath)
    {
        if (await db.Users.AnyAsync(u => u.Username == "demo_participant"))
            return;

        var rng = new Random(42);
        var now = DateTime.UtcNow;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin)
            ?? throw new InvalidOperationException("Admin user not found — run DbSeeder first.");

        // Pre-compute hashes once (BCrypt is intentionally slow)
        var testHash = BCrypt.Net.BCrypt.HashPassword("Test123!");

        // --- Special users ---
        var modDemo = new User
        {
            Id = Guid.NewGuid(), Username = "moderateur_demo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Moderateur123!"),
            Email = "moderateur.demo@codearena.cm", Country = "Cameroun",
            Region = "Centre", School = "ENSP Yaoundé",
            Role = UserRole.Moderator, IsActive = true,
            EmailVerifiedAt = now, CreatedAt = now.AddDays(-120)
        };
        var demoUser = new User
        {
            Id = Guid.NewGuid(), Username = "demo_participant",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            Email = "demo@codearena.cm", Country = "Cameroun",
            Region = "Littoral", School = "IUT de Douala",
            Role = UserRole.Participant, IsActive = true,
            EmailVerifiedAt = now, CreatedAt = now.AddDays(-90)
        };
        await db.Users.AddRangeAsync(modDemo, demoUser);

        // --- 100 participants (deterministic — same rng seed guarantees same data) ---
        var allFirstNames = MaleFirstNames.Concat(FemaleFirstNames).ToArray();
        var participants = new List<User>(100);
        var userIdx = 0;

        foreach (var (region, schools, count) in RegionBuckets)
        {
            for (var i = 0; i < count; i++)
            {
                var fn = allFirstNames[userIdx % allFirstNames.Length];
                var ln = LastNames[(userIdx * 7) % LastNames.Length];
                var school = schools[0] is null ? null : schools[i % schools.Length];

                participants.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"{fn}_{ln}_{userIdx:D3}",
                    PasswordHash = testHash,
                    Email = $"{fn}.{ln}.{userIdx:D3}@mail.cm",
                    Country = "Cameroun",
                    Region = region,
                    School = school,
                    Role = UserRole.Participant,
                    IsActive = true,
                    EmailVerifiedAt = rng.Next(3) != 0 ? now : null,
                    CreatedAt = now.AddDays(-rng.Next(30, 200))
                });
                userIdx++;
            }
        }

        await db.Users.AddRangeAsync(participants);
        await db.SaveChangesAsync();

        // --- 20 competitions ---
        var competitions = new List<Competition>
        {
            // Finished (8)
            MakeComp("CodeArena Challenge Janvier 2026",
                new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(3),
                CompetitionStatus.Finished, admin.Id, now.AddDays(-200)),
            MakeComp("Hackathon Algorithmique de Douala",
                new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(4),
                CompetitionStatus.Finished, modDemo.Id, now.AddDays(-183)),
            MakeComp("Concours ENSPY — Algorithmique 2026",
                new DateTime(2026, 2, 20, 14, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(2),
                CompetitionStatus.Finished, modDemo.Id, now.AddDays(-164)),
            MakeComp("Grand Prix de Programmation Cameroun",
                new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(6),
                CompetitionStatus.Finished, admin.Id, now.AddDays(-144)),
            MakeComp("Challenge Printemps 2026",
                new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(4),
                CompetitionStatus.Finished, modDemo.Id, now.AddDays(-118)),
            MakeComp("Code Battle Interuniversitaire",
                new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(5),
                CompetitionStatus.Finished, admin.Id, now.AddDays(-81)),
            MakeComp("Semaine Numérique Yaoundé 2026",
                new DateTime(2026, 6, 8, 9, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(4),
                CompetitionStatus.Finished, modDemo.Id, now.AddDays(-54)),
            MakeComp("Tournoi d'Été Algorithmique",
                new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(3),
                CompetitionStatus.Finished, admin.Id, now.AddDays(-18)),

            // Ongoing (5) — se terminent dans le futur au moment du seed
            MakeComp("CodeArena Open Cameroun 2026 — Édition Principale",
                now.AddHours(-2), TimeSpan.FromHours(6),
                CompetitionStatus.Ongoing, admin.Id, now.AddDays(-30)),
            MakeComp("Challenge Algorithmique de Yaoundé",
                now.AddHours(-1), TimeSpan.FromHours(4),
                CompetitionStatus.Ongoing, modDemo.Id, now.AddDays(-14)),
            MakeComp("Douala Code Battle — Saison 2",
                now.AddHours(-3), TimeSpan.FromHours(8),
                CompetitionStatus.Ongoing, modDemo.Id, now.AddDays(-7)),
            MakeComp("Python Masters Cameroun",
                now.AddMinutes(-30), TimeSpan.FromHours(3),
                CompetitionStatus.Ongoing, admin.Id, now.AddDays(-5)),
            MakeComp("Web Dev Challenge Afrique Centrale",
                now.AddHours(-2), TimeSpan.FromHours(5),
                CompetitionStatus.Ongoing, modDemo.Id, now.AddDays(-3)),

            // Upcoming (4)
            MakeComp("CodeArena Challenge Septembre 2026",
                now.AddDays(2), TimeSpan.FromHours(4),
                CompetitionStatus.Upcoming, admin.Id, now.AddDays(-1)),
            MakeComp("Hackathon IA & Algorithmes",
                now.AddDays(4), TimeSpan.FromHours(5),
                CompetitionStatus.Upcoming, modDemo.Id, now.AddDays(-1)),
            MakeComp("Mobile Dev Contest 2026",
                now.AddDays(6), TimeSpan.FromHours(4),
                CompetitionStatus.Upcoming, admin.Id, now.AddDays(-2)),
            MakeComp("Défi Cybersécurité Cameroun",
                now.AddDays(1), TimeSpan.FromHours(3),
                CompetitionStatus.Upcoming, modDemo.Id, now),

            // Draft (3) — visibles modérateur/admin uniquement
            MakeComp("Compétition Nationale Universitaire 2027",
                now.AddDays(60), TimeSpan.FromHours(6),
                CompetitionStatus.Draft, admin.Id, now),
            MakeComp("Challenge Étudiant Avancé (Brouillon)",
                now.AddDays(30), TimeSpan.FromHours(5),
                CompetitionStatus.Draft, modDemo.Id, now),
            MakeComp("Test Interne — Configuration Modération",
                now.AddDays(7), TimeSpan.FromHours(2),
                CompetitionStatus.Draft, modDemo.Id, now),
        };

        await db.Competitions.AddRangeAsync(competitions);
        await db.SaveChangesAsync();

        // --- Problems with seed files (5 per competition = 100 problems total) ---
        var demoDir = Path.Combine(uploadsBasePath, "demo");
        Directory.CreateDirectory(Path.Combine(demoDir, "results"));

        var allProblems = new List<Problem>();
        var problemsByCompIdx = new Dictionary<int, List<Problem>>();

        for (var ci = 0; ci < competitions.Count; ci++)
        {
            var comp = competitions[ci];
            var creatorId = ci % 3 == 0 ? admin.Id : modDemo.Id;
            var compProblems = new List<Problem>();

            for (var pi = 0; pi < CompProblemTemplates[ci].Length; pi++)
            {
                var tpl = Templates[CompProblemTemplates[ci][pi]];
                var inputFile = $"c{ci:D2}_p{pi:D2}_input.txt";
                var outputFile = $"c{ci:D2}_p{pi:D2}_output.txt";

                File.WriteAllText(Path.Combine(demoDir, inputFile), tpl.Input);
                File.WriteAllText(Path.Combine(demoDir, outputFile), tpl.Output);

                var problem = new Problem
                {
                    Id = Guid.NewGuid(),
                    CompetitionId = comp.Id,
                    Title = tpl.Title,
                    Body = tpl.Body,
                    Points = tpl.Points,
                    InputFileUrl = $"uploads/demo/{inputFile}",
                    OutputFileUrl = $"uploads/demo/{outputFile}",
                    CreatedByUserId = creatorId,
                    CreatedAt = comp.CreatedAt
                };
                compProblems.Add(problem);
                allProblems.Add(problem);
            }

            await db.Problems.AddRangeAsync(compProblems);
            problemsByCompIdx[ci] = compProblems;
        }
        await db.SaveChangesAsync();

        // --- Submissions simulation ---
        // demo_participant is last (index 100) → elite skill tier
        var allParticipants = participants.Append(demoUser).ToList();
        var allSubmissions = new List<Submission>();
        var statusMap = new Dictionary<(Guid UserId, Guid ProblemId), UserProblemStatus>();

        // Finished competitions (0-7): 70% participation rate
        for (var ci = 0; ci < 8; ci++)
        {
            var comp = competitions[ci];
            var compProblems = problemsByCompIdx[ci];

            for (var ui = 0; ui < allParticipants.Count; ui++)
            {
                if (rng.Next(100) >= 70) continue;

                var user = allParticipants[ui];
                var skill = GetSkillTier(ui, allParticipants.Count);
                var numAttempts = GetAttemptCount(skill, compProblems.Count, rng);

                foreach (var pi in Enumerable.Range(0, compProblems.Count).OrderBy(_ => rng.Next()).Take(numAttempts))
                {
                    var problem = compProblems[pi];
                    var tplIdx = CompProblemTemplates[ci][pi];
                    var isAccepted = rng.Next(100) < GetAcceptanceProbability(skill, problem.Points);
                    var submittedAt = comp.StartDate.AddSeconds(rng.Next(60, (int)comp.Duration.TotalSeconds - 60));
                    var guid = Guid.NewGuid();

                    File.WriteAllText(Path.Combine(demoDir, "results", $"{guid}.txt"),
                        isAccepted ? Templates[tplIdx].Output : "MAUVAISE_REPONSE\n");

                    allSubmissions.Add(new Submission
                    {
                        Id = guid, ProblemId = problem.Id, UserId = user.Id,
                        SubmittedAt = submittedAt,
                        ResultFileUrl = $"uploads/demo/results/{guid}.txt",
                        Status = isAccepted ? SubmissionStatus.Accepted : SubmissionStatus.Wrong
                    });

                    UpdateStatusMap(statusMap, user.Id, problem.Id, submittedAt, isAccepted, rng);
                }
            }
        }

        // Ongoing competitions (8-12): 40% participation, max 3 attempts
        for (var ci = 8; ci < 13; ci++)
        {
            var comp = competitions[ci];
            var compProblems = problemsByCompIdx[ci];
            var elapsed = (int)Math.Max(120, (now - comp.StartDate).TotalSeconds - 60);

            for (var ui = 0; ui < allParticipants.Count; ui++)
            {
                if (rng.Next(100) >= 40) continue;

                var user = allParticipants[ui];
                var skill = GetSkillTier(ui, allParticipants.Count);
                var numAttempts = Math.Min(GetAttemptCount(skill, compProblems.Count, rng), 3);

                foreach (var pi in Enumerable.Range(0, compProblems.Count).OrderBy(_ => rng.Next()).Take(numAttempts))
                {
                    var problem = compProblems[pi];
                    var tplIdx = CompProblemTemplates[ci][pi];
                    var isAccepted = rng.Next(100) < GetAcceptanceProbability(skill, problem.Points);
                    var submittedAt = comp.StartDate.AddSeconds(rng.Next(60, elapsed));
                    var guid = Guid.NewGuid();

                    File.WriteAllText(Path.Combine(demoDir, "results", $"{guid}.txt"),
                        isAccepted ? Templates[tplIdx].Output : "MAUVAISE_REPONSE\n");

                    allSubmissions.Add(new Submission
                    {
                        Id = guid, ProblemId = problem.Id, UserId = user.Id,
                        SubmittedAt = submittedAt,
                        ResultFileUrl = $"uploads/demo/results/{guid}.txt",
                        Status = isAccepted ? SubmissionStatus.Accepted : SubmissionStatus.Wrong
                    });

                    UpdateStatusMap(statusMap, user.Id, problem.Id, submittedAt, isAccepted, rng);
                }
            }
        }

        await db.Submissions.AddRangeAsync(allSubmissions);
        await db.UserProblemStatuses.AddRangeAsync(statusMap.Values);
        await db.SaveChangesAsync();

        // --- TotalScore = sum of points for each solved problem ---
        var pointsById = allProblems.ToDictionary(p => p.Id, p => p.Points);
        var userTotals = statusMap.Values
            .Where(s => s.Solved)
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.Sum(s => pointsById.GetValueOrDefault(s.ProblemId)));

        foreach (var user in allParticipants.Append(modDemo))
        {
            if (userTotals.TryGetValue(user.Id, out var score))
                user.TotalScore = score;
        }
        await db.SaveChangesAsync();

        // --- Badges ---
        await AwardBadgesAsync(db, admin, modDemo, demoUser,
            allParticipants, competitions, allProblems, allSubmissions, statusMap, now);
        await db.SaveChangesAsync();
    }

    // --- Private Helpers ---

    private static Competition MakeComp(string name, DateTime startDate, TimeSpan duration,
        CompetitionStatus status, Guid createdBy, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(), Name = name, StartDate = startDate, Duration = duration,
        Status = status, CreatedByUserId = createdBy, CreatedAt = createdAt
    };

    private static int GetSkillTier(int userIndex, int total) =>
        ((double)userIndex / total) switch
        {
            < 0.30 => 0, // débutant
            < 0.70 => 1, // moyen
            < 0.90 => 2, // avancé
            _       => 3  // élite (demo_participant = dernier index)
        };

    private static int GetAttemptCount(int skillTier, int maxProblems, Random rng) => skillTier switch
    {
        0 => rng.Next(0, 2),
        1 => rng.Next(1, 4),
        2 => rng.Next(3, maxProblems + 1),
        _ => maxProblems
    };

    private static int GetAcceptanceProbability(int skillTier, int problemPoints) =>
        (skillTier, problemPoints) switch
        {
            (0, <= 150) => 20,
            (0, _)      =>  8,
            (1, <= 150) => 50,
            (1, _)      => 25,
            (2, <= 150) => 78,
            (2, _)      => 50,
            (3, <= 150) => 95,
            (3, _)      => 75,
            _           => 30,
        };

    private static void UpdateStatusMap(
        Dictionary<(Guid, Guid), UserProblemStatus> map,
        Guid userId, Guid problemId, DateTime submittedAt, bool isAccepted, Random rng)
    {
        var key = (userId, problemId);
        if (!map.TryGetValue(key, out var ups))
        {
            map[key] = new UserProblemStatus
            {
                UserId = userId, ProblemId = problemId,
                AttemptCount = 1, LastAttemptAt = submittedAt, Solved = isAccepted,
                InputFirstDownloadedAt = submittedAt.AddMinutes(-rng.Next(5, 90))
            };
        }
        else
        {
            ups.AttemptCount++;
            if (submittedAt > ups.LastAttemptAt) ups.LastAttemptAt = submittedAt;
            if (isAccepted && !ups.Solved) ups.Solved = true;
        }
    }

    private static async Task AwardBadgesAsync(
        CodeArenaDbContext db,
        User admin, User modDemo, User demoUser,
        List<User> allParticipants,
        List<Competition> competitions,
        List<Problem> allProblems,
        List<Submission> allSubmissions,
        Dictionary<(Guid UserId, Guid ProblemId), UserProblemStatus> statusMap,
        DateTime now)
    {
        var badges = await db.Badges.ToDictionaryAsync(b => b.Slug);
        if (badges.Count == 0) return;

        var existingSet = (await db.UserBadges
            .Select(ub => new { ub.UserId, ub.BadgeId })
            .ToListAsync())
            .Select(e => (e.UserId, e.BadgeId))
            .ToHashSet();

        var toAdd = new List<UserBadge>();

        void Grant(Guid userId, string slug, DateTime earnedAt)
        {
            if (!badges.TryGetValue(slug, out var badge)) return;
            if (existingSet.Contains((userId, badge.Id))) return;
            if (toAdd.Any(ub => ub.UserId == userId && ub.BadgeId == badge.Id)) return;
            toAdd.Add(new UserBadge { Id = Guid.NewGuid(), UserId = userId, BadgeId = badge.Id, EarnedAt = earnedAt });
        }

        // first-ac : première soumission acceptée
        foreach (var grp in allSubmissions.Where(s => s.Status == SubmissionStatus.Accepted).GroupBy(s => s.UserId))
            Grant(grp.Key, "first-ac", grp.Min(s => s.SubmittedAt));

        // speed-solver : problème résolu en < 30 min après téléchargement input
        foreach (var s in statusMap.Values.Where(s =>
            s.Solved && s.InputFirstDownloadedAt.HasValue && s.LastAttemptAt.HasValue
            && (s.LastAttemptAt.Value - s.InputFirstDownloadedAt.Value).TotalMinutes < 30))
        {
            Grant(s.UserId, "speed-solver", s.LastAttemptAt!.Value);
        }

        // week-streak : demo_participant + top 10% par score total
        var byScore = allParticipants.OrderByDescending(u => u.TotalScore).ToList();
        foreach (var uid in byScore.Take(Math.Max(1, byScore.Count / 10)).Select(u => u.Id).Append(demoUser.Id).Distinct())
            Grant(uid, "week-streak", now.AddDays(-7));

        // top-10 : par compétition terminée
        var problemPointsByComp = allProblems
            .GroupBy(p => p.CompetitionId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(p => p.Id, p => p.Points));

        foreach (var comp in competitions.Where(c => c.Status == CompetitionStatus.Finished))
        {
            if (!problemPointsByComp.TryGetValue(comp.Id, out var pts)) continue;

            foreach (var (uid, _) in statusMap.Values
                .Where(s => s.Solved && pts.ContainsKey(s.ProblemId))
                .GroupBy(s => s.UserId)
                .Select(g => (UserId: g.Key, Score: g.Sum(s => pts[s.ProblemId])))
                .OrderByDescending(x => x.Score).Take(10))
            {
                Grant(uid, "top-10", comp.StartDate.Add(comp.Duration));
            }
        }

        // top-3-national : top 3 classement global
        foreach (var user in allParticipants.OrderByDescending(u => u.TotalScore).Take(3))
            Grant(user.Id, "top-3-national", now);

        // centurion : accordé manuellement à demo_participant pour la présentation
        Grant(demoUser.Id, "centurion", now.AddDays(-5));

        // mentor : admin/modDemo si un exercice créé par eux a 50+ solveurs
        var solvedCountByProblem = statusMap.Values
            .Where(s => s.Solved)
            .GroupBy(s => s.ProblemId)
            .ToDictionary(g => g.Key, g => g.Count());

        var mentorGranted = new HashSet<Guid>();
        foreach (var problem in allProblems)
        {
            if (mentorGranted.Contains(problem.CreatedByUserId)) continue;
            if (solvedCountByProblem.GetValueOrDefault(problem.Id) >= 50)
            {
                Grant(problem.CreatedByUserId, "mentor", now);
                mentorGranted.Add(problem.CreatedByUserId);
            }
        }

        await db.UserBadges.AddRangeAsync(toAdd);
    }
}
