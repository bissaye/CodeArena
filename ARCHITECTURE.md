# Architecture Technique — CodeArena Cameroun

> Document de référence pour tout développeur rejoignant le projet.
> Mis à jour : Sprint INFRA + Sprint V2-4.

---

## Table des matières

1. [Vision et contexte](#1-vision-et-contexte)
2. [Vue d'ensemble](#2-vue-densemble)
3. [Monorepo — organisation des dossiers](#3-monorepo--organisation-des-dossiers)
4. [Backend — Clean Architecture](#4-backend--clean-architecture)
   - 4.1 [CodeArena.Domain](#41-codearena-domain)
   - 4.2 [CodeArena.Application](#42-codearena-application)
   - 4.3 [CodeArena.Infrastructure](#43-codearena-infrastructure)
   - 4.4 [CodeArena.API](#44-codearena-api)
   - 4.5 [CodeArena.Worker](#45-codearena-worker)
5. [Frontend — Angular 21](#5-frontend--angular-21)
6. [Base de données](#6-base-de-données)
7. [Infrastructure transversale](#7-infrastructure-transversale)
8. [Authentification et sécurité](#8-authentification-et-sécurité)
9. [Fichiers uploadés](#9-fichiers-uploadés)
10. [Emails transactionnels](#10-emails-transactionnels)
11. [Temps réel — SignalR + Redis](#11-temps-réel--signalr--redis)
12. [Gamification — Badges et niveaux](#12-gamification--badges-et-niveaux)
13. [Déploiement — Docker et CI/CD](#13-déploiement--docker-et-cicd)
14. [Variables d'environnement](#14-variables-denvironnement)
15. [Flux de données clés](#15-flux-de-données-clés)
16. [Patterns implémentés](#16-patterns-implémentés)
17. [Conventions de code](#17-conventions-de-code)
18. [Pièges connus et décisions importantes](#18-pièges-connus-et-décisions-importantes)
19. [Roadmap et sprints](#19-roadmap-et-sprints)
20. [Production — VPS Hostinger](#20-production--vps-hostinger)

---

## 1. Vision et contexte

CodeArena Cameroun est une **plateforme de compétition algorithmique** (similaire à Codeforces/CodeChef) ciblant le marché camerounais. Les participants téléchargent un fichier d'entrée, exécutent leur algorithme localement, et soumettent leur fichier de sortie. Le juge compare le fichier soumis au fichier de sortie de référence.

**Modèle de jugement : Output-only**
- Pas d'exécution de code sur le serveur (pas de sandbox)
- Le participant soumet uniquement le résultat (`output.txt`)
- Optionnellement le code source (`.c/.cpp/.py/.java/.js`) pour affichage
- Comparaison byte-à-byte côté serveur

**Acteurs :**
| Rôle | Capacités |
|---|---|
| Participant | S'inscrit, soumet des solutions, consulte son profil et classement |
| Modérateur | Crée/modifie compétitions et exercices, voit les brouillons |
| Admin | Tout ce que fait le modérateur + gestion des modérateurs |

---

## 2. Vue d'ensemble

```
┌─────────────────────────────────────────────────────────────────────┐
│                          INTERNET / VPS                             │
│                                                                     │
│  ┌──────────────┐     ┌──────────────────────────────────────────┐  │
│  │   Nginx      │────▶│         Angular 21 SPA                   │  │
│  │  :4200/:80   │     │     (build statique, nginx sert)         │  │
│  └──────┬───────┘     └──────────────────────────────────────────┘  │
│         │ proxy /api, /hubs, /swagger, /uploads                     │
│         ▼                                                           │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │               ASP.NET Core 10 API   :5000/8080               │   │
│  │   Controllers · Services · SignalR Hub · Hangfire (3 workers)│   │
│  └───────┬──────────────────────┬───────────────────────────────┘   │
│          │                      │                                   │
│          ▼                      ▼                                   │
│  ┌──────────────┐     ┌─────────────────┐                          │
│  │  PostgreSQL  │     │   Redis 7       │                          │
│  │  :5432       │     │   :6379         │                          │
│  │  EF Core 10  │     │ Cache + pub/sub │                          │
│  └──────────────┘     └────────┬────────┘                          │
│                                │ pub/sub                            │
│                       ┌────────▼────────┐                          │
│                       │ CodeArena.Worker │                          │
│                       │ Hangfire :5001   │                          │
│                       │ 10 workers       │                          │
│                       └─────────────────┘                          │
└─────────────────────────────────────────────────────────────────────┘
```

**Flux principal :**
1. L'Angular SPA parle à l'API via Nginx (proxy `/api/`, `/hubs/`, `/uploads/`)
2. L'API persiste en PostgreSQL via EF Core
3. Les jobs lourds (emails, badges, notifications) sont délégués à Hangfire (Worker)
4. Les notifications temps réel transitent par Redis pub/sub → SignalR → navigateur
5. Le cache du leaderboard est stocké dans Redis (`IDistributedCache`, TTL 30s)

---

## 3. Monorepo — organisation des dossiers

```
CodeArenaCamer/
├── backend/
│   ├── CodeArena.Domain/           # Entités, Enums — zéro dépendance externe
│   ├── CodeArena.Application/      # Services, DTOs, interfaces, validateurs
│   ├── CodeArena.Infrastructure/   # EF Core, Redis, Email, File, JWT, BCrypt
│   ├── CodeArena.API/              # Controllers, Program.cs, SignalR hub
│   └── CodeArena.Worker/           # Hangfire worker dédié
├── frontend/
│   ├── src/app/
│   │   ├── core/                   # Guards, interceptors, services globaux, modèles
│   │   ├── shared/                 # Composants/pipes réutilisables
│   │   └── features/               # Pages de l'application (lazy-loaded)
│   ├── public/assets/              # i18n JSON, images, badges SVG
│   ├── nginx.conf                  # Config Nginx (prod)
│   └── Dockerfile                  # Multi-stage : Node build → Nginx runtime
├── docker-compose.yml              # Dev + prod (services communs)
├── docker-compose.prod.yml         # Override prod (ports internes masqués)
├── .env.example                    # Toutes les variables à configurer
├── CLAUDE.md                       # Instructions Claude Code (ne pas supprimer)
├── ARCHITECTURE.md                 # Ce document
├── context.md                      # User Stories, sprints, endpoints détaillés
└── design-system.md                # Tokens CSS, composants Angular documentés
```

---

## 4. Backend — Clean Architecture

Le backend suit une **Clean Architecture stricte** avec 5 projets .NET 10. La règle de dépendance est unidirectionnelle : les couches internes n'ont aucune connaissance des couches externes.

```
Domain ← Application ← Infrastructure ← API
                     ←               ← Worker
```

### Graphe de dépendances NuGet

```
Domain          : (aucun package NuGet)
Application     : FluentValidation 12 · Hangfire.Core 1.8 · Markdig 0.41 · EF Core 10 (abstractions)
Infrastructure  : EF Core 10 · Npgsql · BCrypt.Net 4 · MailKit 4 · ImageSharp 3 ·
                  StackExchange.Redis · Microsoft.Extensions.Caching.StackExchangeRedis ·
                  Hangfire 1.8 · Hangfire.PostgreSql 1.20 · System.IdentityModel.Tokens.Jwt
API             : Hangfire.AspNetCore · SignalR.StackExchangeRedis · JwtBearer · Swashbuckle
Worker          : Hangfire.AspNetCore · Hangfire.PostgreSql
```

---

### 4.1 CodeArena.Domain

**Rôle :** Cœur métier pur. Aucune dépendance externe.

**Contenu :**

#### Entités (`Entities/`)

| Entité | Champs clés | Notes |
|---|---|---|
| `User` | Id, Username, PasswordHash, Email, Country, Region, School, AvatarUrl, Role, TotalScore, Level (computed) | `Level` = propriété C# calculée sur `TotalScore` (non persistée) |
| `Competition` | Id, Name, StartDate, Duration, Status, CreatedByUserId, StartReminderSentAt | `EndDate` = `StartDate + Duration` (computed) |
| `Problem` | Id, CompetitionId, Title, Body (Markdown), Points, InputFileUrl, OutputFileUrl, CreatedByUserId | `OutputFileUrl` jamais exposé en API publique |
| `Submission` | Id, ProblemId, UserId, SubmittedAt, ResultFileUrl, SourceFileUrl?, Status, IsFirstAccepted | `IsFirstAccepted` déclenche la mise à jour du score |
| `UserProblemStatus` | UserId (PK), ProblemId (PK), Solved, AttemptCount, LastAttemptAt, InputFirstDownloadedAt | Clé composite — une ligne par (user, problème) |
| `Notification` | Id, UserId, Type, Title, Body, IsRead, ReadAt?, CreatedAt, RelatedUrl? | |
| `Badge` | Id, Slug (unique), Name, Description, IconUrl, Condition (enum) | 7 badges seedés avec UUIDs fixes |
| `UserBadge` | Id, UserId, BadgeId, EarnedAt | Index unique (UserId, BadgeId) |
| `EmailVerification` | Id, UserId, Token, ExpiresAt, UsedAt? | |
| `PasswordResetToken` | Id, UserId, Token, ExpiresAt, UsedAt? | |

#### Enums (`Enums/`)

```csharp
enum UserRole          { Participant = 0, Moderator = 1, Admin = 2 }
enum CompetitionStatus { Draft = 0, Upcoming = 1, Ongoing = 2, Finished = 3 }
enum SubmissionStatus  { Pending = 0, Accepted = 1, Wrong = 2 }
enum BadgeCondition    { FirstAccepted, SpeedSolver, WeekStreak, Top10Competition,
                         Top3National, Centurion, Mentor }
enum NotificationType  { SubmissionAccepted, SubmissionWrong, CompetitionStarting,
                         CompetitionStarted, BadgeEarned }
```

---

### 4.2 CodeArena.Application

**Rôle :** Logique métier, orchestration, DTOs, interfaces. Toute règle "ce que l'application fait" vit ici.

#### Interfaces (`Interfaces/`)

Chaque service infrastructure est abstrait derrière une interface définie ici. L'Application ne connaît que les interfaces.

| Interface | Implémentation (Infrastructure) | Usage |
|---|---|---|
| `IAppDbContext` | `CodeArenaDbContext` | Accès EF Core depuis les services |
| `IJwtService` | `JwtService` | Génération/validation JWT |
| `IPasswordHasher` | `PasswordHasher` | BCrypt hash/verify |
| `IFileStorageService` | `FileStorageService` | Sauvegarde/lecture fichiers locaux |
| `IEmailService` | `EmailService` | Envoi d'emails via MailKit |
| `INotificationPusher` | `RedisPublishPusher` | Pub/sub Redis pour notifications temps réel |
| `ILeaderboardPusher` | `RedisLeaderboardPusher` | Pub/sub Redis pour leaderboard temps réel |
| `ILeaderboardBroadcastService` | `LeaderboardBroadcastService` | Déclenche la diffusion du leaderboard |

#### Services (`Services/`)

| Service | Responsabilités clés |
|---|---|
| `AuthService` | Register (hash + EmailVerification), Login (verify hash + JWT), ChangePassword, ForgotPassword/ResetPassword, ResendVerification. Enqueue les emails via Hangfire. |
| `SubmissionService` | Valide que la compétition est Ongoing, que l'exercice n'est pas déjà résolu (409), sauvegarde le fichier uploadé, compare contre `OutputFileUrl`, met à jour `UserProblemStatus` et `TotalScore` dans une transaction, enqueue badge check + notification via Hangfire. |
| `CompetitionService` | CRUD compétitions, filtrage Draft selon rôle, stats (taux de réussite par exercice). |
| `ProblemService` | CRUD exercices, sanitisation Markdown via `IMarkdownSanitizerService`, gestion fichiers input/output. |
| `LeaderboardService` | Global (filtré, paginé, Redis cache 30s), mini (top-N), compétition. |
| `BadgeService` | `CheckAndAwardBadgesAsync(userId, problemId)` : vérifie les 7 conditions, insère `UserBadge`, enqueue notification BadgeEarned via `INotificationService`. `RecordInputDownloadAsync` : met à jour `InputFirstDownloadedAt`. |
| `NotificationService` | `CreateAsync` : insère en base, appelle `INotificationPusher.PushAsync` pour push temps réel. Liste paginée, mark as read. |
| `UserService` | Profil public (+ 20 dernières activités), update profil, upload avatar (resize 200×200 via ImageSharp). |
| `AdminService` | Liste/Promote/Demote modérateur. |
| `MarkdownSanitizerService` | Singleton. Pipeline Markdig `DisableHtml().UseAdvancedExtensions()` : HTML brut supprimé → XSS impossible. |

#### Validateurs FluentValidation

Tous les validateurs sont dans `Application/` et enregistrés automatiquement via `AddValidatorsFromAssembly`. Les Controllers les injectent et appellent `ValidateAsync` explicitement (pas de filtre automatique).

#### DI Registration (`DependencyInjection.cs`)

```csharp
services.AddApplication(); // ajoute tous les services + validators
```

---

### 4.3 CodeArena.Infrastructure

**Rôle :** Implémentations concrètes : EF Core, Redis, email, filesystem, JWT, BCrypt.

#### Persistence (`Persistence/`)

**`CodeArenaDbContext`** — hérite de `DbContext`, implémente `IAppDbContext`.

DbSets : `Users`, `Competitions`, `Problems`, `Submissions`, `UserProblemStatuses`, `EmailVerifications`, `PasswordResetTokens`, `Notifications`, `Badges`, `UserBadges`.

**Configurations EF Core** (`Configurations/`) — chaque entité a sa propre classe `IEntityTypeConfiguration<T>` :

| Configuration | Points notables |
|---|---|
| `UserProblemStatusConfiguration` | Clé composite `(UserId, ProblemId)` |
| `UserBadgeConfiguration` | `HasIndex(UserId, BadgeId).IsUnique()` ; Id généré par `gen_random_uuid()` |
| `SubmissionConfiguration` | Index composite `(ProblemId, UserId, Status)` et `(UserId, Status)` pour les queries de badges |
| `NotificationConfiguration` | Index `(UserId, IsRead)` pour le filtre non-lues paginé |

**`DbSeeder.SeedAsync`** — s'exécute au démarrage. Crée admin + 5 users test + 2 compétitions (1 Finished, 1 Ongoing) + 4 exercices + fichiers seed. Idempotent : skip si des users existent déjà.

**`DemoSeeder.SeedDemoAsync`** — déclenchable via `POST /api/admin/seed-demo`. 100 participants camerounais, 20 compétitions, 100 exercices, ~1200 soumissions simulées, badges. Idempotent : skip si `demo_participant` existe.

#### Jobs (`Jobs/`)

**`CompetitionStatusJob`** — job Hangfire récurrent (minutely). Remplace le `BackgroundService` initial.

Logique :
1. Charge les compétitions Upcoming dont `StartDate <= now` → passe en Ongoing, enqueue `CompetitionStarted` notification pour chaque user actif.
2. Charge les compétitions Upcoming dont `StartDate - 1h <= now` et `StartReminderSentAt is null` → enqueue `CompetitionStarting` reminder, set `StartReminderSentAt`.
3. Charge les compétitions Ongoing dont `EndDate <= now` → passe en Finished, enqueue broadcast leaderboard.

**Pourquoi Hangfire remplace BackgroundService ?**
- `BackgroundService` utilise le DbContext de l'hôte → problèmes de concurrence avec les requêtes HTTP (même scope)
- Hangfire crée un scope isolé par job → DbContext frais, pas de conflits

#### Services (`Services/`)

| Service | Technologie | Notes |
|---|---|---|
| `JwtService` | `System.IdentityModel.Tokens.Jwt` | HS256, claims courts (`sub`, `unique_name`, `role`, `jti`, `exp`). Jamais `ClaimTypes.*`. |
| `PasswordHasher` | `BCrypt.Net-Next` | `HashPassword` / `Verify`. Cost factor par défaut (10). |
| `FileStorageService` | System.IO local | Sauvegarde dans `{uploadsBasePath}/{subfolder}/{guid}{ext}`. Retourne chemin relatif `uploads/{subfolder}/{file}`. `GetAbsolutePath(relPath)` = `Path.Combine(contentRootPath, relPath)`. Avatar : resize 200×200 JPEG via ImageSharp 3.x. |
| `EmailService` | MailKit 4, Brevo SMTP | `smtp-relay.brevo.com:587`, StartTLS. Fire-and-forget intégré à Hangfire. HTML templates dans `EmailTemplates.cs`. |
| `RedisPublishPusher` | StackExchange.Redis | Publie sur `notifications:push:{userId}` un JSON `NotificationDto`. |
| `RedisLeaderboardPusher` | StackExchange.Redis | Publie sur `leaderboard:updated`. |
| `LeaderboardBroadcastService` | Appelle `ILeaderboardService` + `ILeaderboardPusher` | Déclenché par `CompetitionStatusJob` à la fin d'une compétition. |

#### DI Registration (`DependencyInjection.cs`)

```csharp
services.AddInfrastructure(configuration); // EF Core, Redis, services, Hangfire storage
```

---

### 4.4 CodeArena.API

**Rôle :** Point d'entrée HTTP. Controllers, middlewares, configuration ASP.NET, SignalR hub.

#### `Program.cs` — pipeline de démarrage

```
1. AddApplication() + AddInfrastructure()
2. AddControllers()
3. AddCors("AllowFrontend") — origine depuis FRONTEND_URL, credentials autorisés
4. AddAuthentication(JwtBearer) — lecture token depuis query string pour SignalR
5. AddAuthorization() — 2 policies : ModeratorOrAdmin, AdminOnly
6. AddSwaggerGen() — Swagger UI + sécurité Bearer
7. AddHangfire() + AddHangfireServer(3 workers, "codearena-api")
8. AddSignalR().AddStackExchangeRedis() — backplane Redis
9. AddHostedService<RedisNotificationRelay>()
10. Build() → Migrate() + SeedAsync()
11. RegisterHangfireRecurringJob(CompetitionStatusJob, Cron.Minutely)
12. UseStaticFiles("/uploads") → fichiers uploadés accessibles
13. UseSwagger/UseSwaggerUI
14. UseCors → UseAuthentication → UseAuthorization
15. MapControllers() + MapHub<NotificationHub>("/hubs/notifications")
16. UseHangfireDashboard("/hangfire") — HangfireAuthFilter (rôle Admin)
```

#### Controllers

Tous héritent de `ControllerBase` avec `[ApiController]`. Aucune logique métier dans les controllers — ils valident → appellent le service → retournent le code HTTP approprié.

**Codes HTTP stricts :**
- `200` success (lecture)
- `201` created (avec `CreatedAtAction` ou `Created`)
- `400` validation échouée ou règle métier (ex: compétition non en cours)
- `401` token absent ou invalide
- `403` rôle insuffisant
- `404` ressource inexistante
- `409` conflit (username existant, exercice déjà résolu)

#### `NotificationHub` (`Hubs/`)

```csharp
[Authorize]  // JWT obligatoire
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // L'user rejoint le groupe nommé par son userId
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
```

Événements client : `ReceiveNotification(NotificationDto)`, `LeaderboardUpdated(LeaderboardUpdateEvent)`.

#### `RedisNotificationRelay` (HostedService)

Subscribe aux canaux Redis `notifications:push:*` et `leaderboard:updated`. À chaque message reçu, utilise `IHubContext<NotificationHub>` pour pusher vers le groupe SignalR correspondant.

**Pourquoi deux processus pour la même chose ?**
L'API publie sur Redis (via `RedisPublishPusher`), `RedisNotificationRelay` subscribe dans l'API et pousse vers SignalR. Ce pattern permet :
- Que le Worker (processus séparé) publie des notifications → l'API les reçoit et les push aux clients
- Scale-out : plusieurs instances API reçoivent toutes les notifications Redis

#### `HangfireAuthFilter`

Implémente `IDashboardAuthorizationFilter`. Permet l'accès au dashboard `/hangfire` uniquement si l'utilisateur est authentifié avec le rôle Admin.

---

### 4.5 CodeArena.Worker

**Rôle :** Processus Hangfire dédié. Traite les jobs lourds en isolation de l'API.

SDK `Microsoft.NET.Sdk.Web` (pour servir le dashboard Hangfire sur :5001).

**Queues traitées :** `default`, `emails`, `badges`, `notifications`.

**Workers :** 10 (vs 3 dans l'API — le Worker est le processeur principal).

**Dashboard :** `/hangfire` sur le port 5001 — accessible uniquement en interne (non exposé en prod).

---

## 5. Frontend — Angular 21

### Technologie et configuration globale

| Choix | Raison |
|---|---|
| **Angular 21 standalone components** | Pas de NgModules, tree-shaking optimal, chargement lazy natif |
| **Zoneless change detection** (`provideZonelessChangeDetection`) | Meilleures performances, pas de Zone.js, basé sur Signals |
| **Lazy loading** (`loadComponent`) | Chaque feature ne charge que son bundle à la demande |
| **Reactive Forms** partout | Pas de Template-driven forms — meilleur contrôle, testabilité |
| **ngx-translate 18** | i18n FR/EN, clés en `fr.json` et `en.json` dans `public/assets/i18n/` |
| **@microsoft/signalr ^8.0.7** | Notifications push temps réel depuis l'API |

### Structure des dossiers

```
frontend/src/app/
├── core/
│   ├── guards/
│   │   ├── auth.guard.ts          # [Authorize] — redirige /login si non authentifié
│   │   ├── moderator.guard.ts     # rôle Moderator ou Admin
│   │   └── admin.guard.ts         # rôle Admin uniquement
│   ├── interceptors/
│   │   ├── jwt.interceptor.ts     # Injecte Authorization: Bearer <token>
│   │   └── error.interceptor.ts   # Gestion globale des erreurs HTTP
│   ├── models/                    # Interfaces TypeScript (auth, competition, problem...)
│   └── services/
│       ├── auth.service.ts        # BehaviorSubject<User?>, localStorage, parseJwtPayload
│       ├── notification.service.ts # SignalR connection, refresh$, badgeEarned$
│       ├── competition.service.ts
│       ├── problem.service.ts
│       ├── leaderboard.service.ts
│       ├── user.service.ts
│       ├── badge.service.ts
│       ├── toast.service.ts
│       └── admin.service.ts
├── shared/
│   ├── components/
│   │   ├── header/               # Navigation, dropdown user, switch langue, notif bell
│   │   ├── toast/                # Toasts (success/error) via ToastService
│   │   ├── notification-bell/    # Badge compteur non-lues, dropdown 5 dernières
│   │   ├── competition-card/     # Carte compétition réutilisable (home + liste)
│   │   ├── leaderboard-mini/     # Sidebar leaderboard top-N
│   │   └── countdown-timer/      # Compte à rebours compétition
│   └── pipes/
│       ├── markdown.pipe.ts      # Transform Markdown → HTML (marked + DomSanitizer)
│       └── country-flag.pipe.ts  # Code ISO → emoji drapeau
└── features/
    ├── home/                     # Page d'accueil : En cours / À venir / Passées + sidebar
    ├── auth/                     # Login, Register, ForgotPassword, ResetPassword, VerifyEmail
    ├── competition/              # Liste compétitions, détail compétition
    ├── problem/                  # Détail exercice, soumission, historique
    ├── profile/                  # Profil utilisateur public, modification
    ├── leaderboard/              # Classement global filtré/paginé
    ├── notifications/            # Page notifications complète
    ├── admin/                    # Gestion modérateurs + forms compétition/exercice
    ├── not-found/                # Page 404
    └── forbidden/                # Page 403
```

### Services Angular clés

#### `AuthService`

```typescript
// Stockage
private currentUser$ = new BehaviorSubject<User | null>(null);
// Lecture token localStorage au démarrage → parseJwtPayload
// Login → stocke token → met à jour BehaviorSubject
// Logout → vide localStorage + BehaviorSubject
```

#### `NotificationService`

```typescript
// SignalR connection lifecycle
startConnection(): void          // HubConnectionBuilder + withAutomaticReconnect
stopConnection(): void

// Streams
refresh$ = new Subject<void>()   // émet quand nouvelles notifs arrivent
badgeEarned$ = new Subject<void>() // émet quand badge BadgeEarned reçu

// Sur événement SignalR "ReceiveNotification":
//   refresh$.next()
//   if (type === BadgeEarned) badgeEarned$.next()
```

Connection démarrée dans `NotificationBellComponent.ngOnInit()` si l'user est authentifié.

### Règle critique — Zoneless et markForCheck

En zoneless, Angular ne détecte pas automatiquement les changements dans les callbacks async non-interceptés (ex: `setTimeout`, callbacks raw). Règle :

```typescript
// Tout changement dans un callback async → forcer la détection
this.someData = result;
this.cdr.markForCheck(); // ← OBLIGATOIRE
```

Les callbacks RxJS (HttpClient, SignalR via Subject) sont automatiquement détectés. Les timers bruts (rarement utilisés depuis la migration SignalR) nécessitent `markForCheck()`.

### Règle critique — Formulaires sans FormsModule

```html
<!-- MAUVAIS : (ngSubmit) sans FormsModule → soumission GET native -->
<form (ngSubmit)="onSubmit()">

<!-- CORRECT : intercepte l'événement natif manuellement -->
<form (submit)="$event.preventDefault(); onSubmit()">
```

La règle s'applique partout où `[formGroup]` n'est pas présent et où `FormsModule` n'est pas importé.

### Routing

Toutes les routes utilisent `loadComponent` pour le lazy-loading. Les guards sont des `CanActivateFn` fonctionnels (pas de classes).

```typescript
// Exemple
{
  path: 'problems/:id',
  loadComponent: () => import('./features/problem/problem-detail/...'),
  canActivate: [authGuard]
}
```

### Assets statiques

Les assets sont dans `public/` (et non `src/assets/`). C'est la configuration `angular.json` → `"input": "public"`. Mettre un fichier dans `src/assets/` n'aurait aucun effet — il ne sera pas copié au build.

```
public/
├── assets/
│   ├── i18n/fr.json              # Traductions françaises
│   ├── i18n/en.json              # Traductions anglaises
│   ├── badges/                   # SVG des 7 badges
│   └── logo.svg                  # Logo CodeArena (lion + accolades)
```

---

## 6. Base de données

**PostgreSQL 16-alpine** via EF Core 10 + Npgsql.

### Schéma — tables principales

```
users
  id uuid PK
  username varchar UNIQUE
  password_hash varchar
  email varchar
  phone_number varchar
  country varchar
  region varchar
  school varchar
  avatar_url varchar
  role int (0=Participant, 1=Moderator, 2=Admin)
  total_score int DEFAULT 0
  created_at timestamptz
  promoted_at timestamptz
  is_active bool
  email_verified_at timestamptz
  password_reset_requested_at timestamptz
  notification_email_enabled bool

competitions
  id uuid PK
  name varchar
  start_date timestamptz
  duration interval
  status int (0=Draft, 1=Upcoming, 2=Ongoing, 3=Finished)
  created_by_user_id uuid FK → users
  last_modified_by_user_id uuid FK → users
  created_at timestamptz
  last_modified_at timestamptz
  start_reminder_sent_at timestamptz

problems
  id uuid PK
  competition_id uuid FK → competitions
  title varchar
  body text (Markdown sanitisé)
  points int
  input_file_url varchar   (chemin relatif ex: uploads/demo/c00_p00_input.txt)
  output_file_url varchar  (JAMAIS exposé en API publique)
  created_by_user_id uuid FK → users
  created_at timestamptz
  last_modified_by_user_id uuid FK → users
  last_modified_at timestamptz

submissions
  id uuid PK
  problem_id uuid FK → problems
  user_id uuid FK → users
  submitted_at timestamptz
  result_file_url varchar
  source_file_url varchar
  status int (0=Pending, 1=Accepted, 2=Wrong)
  is_first_accepted bool
  -- Index: (problem_id, user_id, status), (user_id, status)

user_problem_statuses
  user_id uuid FK → users       PK composite
  problem_id uuid FK → problems PK composite
  solved bool
  attempt_count int
  last_attempt_at timestamptz
  input_first_downloaded_at timestamptz  (pour badge speed-solver)

notifications
  id uuid PK
  user_id uuid FK → users
  type int (enum NotificationType)
  title varchar
  body varchar
  is_read bool DEFAULT false
  read_at timestamptz
  created_at timestamptz
  related_url varchar
  -- Index: (user_id, is_read), (created_at DESC)

badges
  id uuid PK (UUIDs fixes préfixe 10000000-...)
  slug varchar UNIQUE
  name varchar
  description varchar
  icon_url varchar
  condition int (enum BadgeCondition)

user_badges
  id uuid PK DEFAULT gen_random_uuid()
  user_id uuid FK → users
  badge_id uuid FK → badges
  earned_at timestamptz
  -- Index unique: (user_id, badge_id)

email_verifications
  id uuid PK
  user_id uuid FK → users
  token varchar
  expires_at timestamptz
  used_at timestamptz

password_reset_tokens
  id uuid PK
  user_id uuid FK → users
  token varchar
  expires_at timestamptz
  used_at timestamptz
```

### Migrations

| Nom | Contenu |
|---|---|
| `20260819052330_InitialSchema` | Tables : users, competitions, problems, submissions, user_problem_statuses |
| `20260819112351_AddUserTotalScore` | Colonne `total_score` sur users |
| `20260819162605_AddUserPromotedAt` | Colonne `promoted_at` sur users |
| `20260820180742_AddEmailVerificationAndPasswordReset` | Tables email_verifications, password_reset_tokens |
| `20260820200713_AddNotifications` | Table notifications + index (user_id, is_read) |
| `20260821075504_AddBadgesAndUserBadges` | Tables badges, user_badges + colonne input_first_downloaded_at |
| `20260821090945_AddBadgePerformanceIndexes` | Index composites sur submissions |

**Ajouter une migration :**
```bash
cd backend
dotnet ef migrations add NomExplicite \
  --project CodeArena.Infrastructure \
  --startup-project CodeArena.API
```

**Les migrations s'appliquent automatiquement au démarrage** (`db.Database.Migrate()` dans Program.cs).

---

## 7. Infrastructure transversale

### Redis

**Image :** `redis:7-alpine`  
**Port interne :** 6379  
**Variable :** `REDIS_CONNECTION=redis:6379`

Deux usages distincts :

| Usage | Clés / Canaux | Détail |
|---|---|---|
| Cache leaderboard | `leaderboard_global_{top}`, `leaderboard_filtered|{hash}` | `IDistributedCache` (StackExchangeRedis), TTL 30s, JSON sérialisé |
| Pub/sub notifications | `notifications:push:{userId}` | `IConnectionMultiplexer` Singleton, publisher = Worker/API, subscriber = `RedisNotificationRelay` dans l'API |
| Pub/sub leaderboard | `leaderboard:updated` | Même pattern |
| SignalR backplane | Interne SignalR | `.AddStackExchangeRedis(conn)` — scale-out multi-instances |

### Hangfire

**Version :** 1.8.22 + Hangfire.PostgreSql 1.20.11  
**Storage :** PostgreSQL (tables créées automatiquement au 1er démarrage, pas de migration EF)

**Deux serveurs :**

| Processus | Workers | Serveur | Dashboard |
|---|---|---|---|
| `CodeArena.API` | 3 | `codearena-api` | `/hangfire` (port 5000, Admin uniquement) |
| `CodeArena.Worker` | 10 | `codearena-worker` | `/hangfire` (port 5001, interne) |

**Jobs récurrents :**
- `competition-status-update` → `CompetitionStatusJob.ExecuteAsync` → `Cron.Minutely()`

**Jobs fire-and-forget (enqueued via `IBackgroundJobClient.Enqueue`) :**

| Déclencheur | Job |
|---|---|
| Soumission Accepted | `BadgeService.CheckAndAwardBadgesAsync` |
| Soumission (Accepted ou Wrong) | `NotificationService.CreateAsync` (jugement) |
| Register / ForgotPassword | `EmailService.SendEmailVerificationAsync` / `SendPasswordResetAsync` |
| Compétition Upcoming→Ongoing | `NotificationService.CreateAsync` × N users actifs |
| Compétition Finished | `LeaderboardBroadcastService.BroadcastAsync` |

**Règle :** Tout job qui touche le DbContext utilise `CancellationToken.None` (non sérialisable) dans sa lambda.

---

## 8. Authentification et sécurité

### JWT

**Algorithme :** HS256 (symétrique)  
**Secret :** variable `JWT_SECRET` (min 32 caractères)  
**Durée :** 24h (configurable via `JWT_EXPIRY_HOURS`)

**Claims :**
```
sub           → userId (Guid)
unique_name   → username
role          → "Participant" | "Moderator" | "Admin"
jti           → UUID unique du token
exp           → Unix timestamp expiration
```

**Jamais `ClaimTypes.*`** (noms longs type `http://schemas.microsoft.com/...`) — les claims utilisent les noms courts.

**WebSocket SignalR :** le token ne peut pas être envoyé dans un header HTTP lors d'un upgrade WebSocket. Solution :

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var token = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/hubs"))
            context.Token = token;
        return Task.CompletedTask;
    }
};
```

Côté Angular, le service SignalR ajoute `?access_token=<jwt>` à l'URL de connexion.

### Policies d'autorisation

```csharp
"ModeratorOrAdmin" → RequireRole("Moderator", "Admin")
"AdminOnly"        → RequireRole("Admin")
```

### Règles de sécurité

| Règle | Implémentation |
|---|---|
| Admin via seed uniquement | Pas d'endpoint pour créer un admin — uniquement via `DbSeeder` ou migration |
| Output jamais exposé | `OutputFileUrl` jamais inclus dans les DTOs publics |
| Fichiers uploadés nommés GUID | `FileStorageService` : `{Guid.NewGuid()}{ext}` — pas de nom original (path traversal impossible) |
| Markdown sanitisé | `MarkdownSanitizerService` (Singleton, Markdig `.DisableHtml()`) |
| Avatar redimensionné | ImageSharp : crop 200×200, resauvé en JPEG — supprime EXIF et metadata |
| Score en transaction | `SubmissionService` utilise `BeginTransactionAsync` pour toute mise à jour de `TotalScore` |
| Exercice déjà résolu | `SubmissionService` retourne 409 si `UserProblemStatus.Solved == true` |
| Anti-enumeration email | `ForgotPassword` retourne toujours HTTP 200, même si l'email n'existe pas |

---

## 9. Fichiers uploadés

**Stockage :** système de fichiers local (pas S3).  
**Variable :** `UPLOADS_PATH=/app/uploads` (absolu dans les conteneurs).

**Conventions de chemin :**

```
DB stocke    : uploads/{subfolder}/{guid}.ext
Disque       : {UPLOADS_PATH}/{subfolder}/{guid}.ext
GetAbsPath() : Path.Combine(contentRootPath, relPath)
URL servie   : /uploads/{subfolder}/{guid}.ext  (StaticFiles middleware)
```

Sous-dossiers :

| Dossier | Contenu |
|---|---|
| `uploads/seed/` | Fichiers créés par DbSeeder (input/output exercices de base) |
| `uploads/demo/` | Fichiers créés par DemoSeeder (exercices + résultats soumissions démo) |
| `uploads/results/` | Résultats soumissions réelles (`{guid}.txt`) |
| `uploads/submissions/src/` | Sources soumissions (optionnel, `.c/.cpp/.py/.java/.js`) |
| `uploads/avatars/` | Avatars utilisateurs (JPEG 200×200) |
| `uploads/inputs/` | Inputs créés par modérateur via API |
| `uploads/outputs/` | Outputs créés par modérateur via API (jamais accessibles publiquement) |

**Limites fichiers :**
- Avatar : max 3 MB upload, reshape 200×200 JPEG
- Input/Output exercice : max 12 MB (multipart)
- Résultat soumission : max 5 MB
- Extensions autorisées résultat : `.txt` uniquement
- Extensions autorisées source : `.c .cpp .py .java .js`

---

## 10. Emails transactionnels

**Provider :** Brevo (ex-Sendinblue)  
**Bibliothèque :** MailKit 4.17.0  
**Configuration SMTP :**
```
Host     : smtp-relay.brevo.com
Port     : 587
Security : StartTLS
Login    : format b62dbe001@smtp-brevo.com (PAS l'email du compte)
```

**Emails envoyés :**

| Événement | Template |
|---|---|
| Inscription | Vérification email (lien `/verify-email?token=...`) |
| ForgotPassword | Réinitialisation (lien `/reset-password?token=...`) |
| Renvoi vérification | Même que inscription |

**Fire-and-forget via Hangfire :** l'envoi est enqueued dans la queue `emails`. La réponse HTTP n'est pas bloquée par l'envoi SMTP.

---

## 11. Temps réel — SignalR + Redis

### Architecture de livraison

```
SubmissionService (API process)
  → IBackgroundJobClient.Enqueue<INotificationService>(CreateAsync)
     → Hangfire Worker exécute CreateAsync
        → Sauvegarde en DB (Notification entity)
        → INotificationPusher.PushAsync(userId, dto)
           → RedisPublishPusher.PushAsync
              → Redis PUBLISH notifications:push:{userId} <json>
                 → RedisNotificationRelay (HostedService dans API process)
                    → IHubContext<NotificationHub>.Clients.Group(userId)
                       → "ReceiveNotification" event
                          → Angular NotificationService.refresh$.next()
```

### Pourquoi ce pipeline complexe ?

- Le Worker (processus séparé) ne peut pas accéder directement à SignalR de l'API
- Redis pub/sub sert de bus inter-processus
- Le backplane Redis SignalR permet en plus le scale-out horizontal (plusieurs instances API)

### Côté Angular

```typescript
// NotificationService
private hubConnection: HubConnection;

startConnection() {
  this.hubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications', { accessTokenFactory: () => token })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();
  
  this.hubConnection.on('ReceiveNotification', (dto: NotificationDto) => {
    this.refresh$.next();
    if (dto.type === 'BadgeEarned') this.badgeEarned$.next();
  });
  
  await this.hubConnection.start();
}
```

Le polling `setInterval` a été entièrement supprimé. La connexion SignalR remplace 100% du polling.

---

## 12. Gamification — Badges et niveaux

### Niveaux (propriété computed non persistée)

```csharp
// User.Level
TotalScore >= 1500 → "Expert"
TotalScore >= 500  → "Avancé"
TotalScore >= 100  → "Intermédiaire"
_                  → "Débutant"
```

**Attention EF Core :** `Level` ne peut pas être utilisé dans une requête EF (non traductible en SQL). Les services `LeaderboardService`, `CompetitionService` et `BadgeService` ont chacun une méthode `static GetLevel(int score)` identique pour la projection SQL.

### Badges (7, UUIDs fixes)

| Slug | Condition | Vérification |
|---|---|---|
| `first-ac` | Première soumission Accepted | Après chaque Accepted |
| `speed-solver` | Résolu en < 30 min après téléchargement input | `(LastAttemptAt - InputFirstDownloadedAt) < 30min` |
| `week-streak` | 7 jours consécutifs avec ≥ 1 soumission | Vérifié via historique dates soumissions |
| `top-10` | Top 10 d'une compétition terminée | Calculé à la fin de chaque compétition |
| `top-3-national` | Top 3 classement national | Classement global par TotalScore |
| `centurion` | 100 exercices distincts résolus | Count de UserProblemStatus.Solved = true |
| `mentor` | Exercice créé résolu par 50+ participants | Count de solvers par problème |

**Flow d'attribution :**
1. Soumission Accepted → `SubmissionService` → `IBackgroundJobClient.Enqueue<IBadgeService>(CheckAndAward)`
2. Worker exécute `CheckAndAwardBadgesAsync(userId, problemId)` dans un scope isolé
3. Pour chaque badge non encore obtenu : vérifie la condition → insère `UserBadge` si satisfaite
4. `NotificationService.CreateAsync(BadgeEarned)` → Redis → SignalR → Angular `badgeEarned$.next()`
5. `ProblemDetailComponent` souscrit à `badgeEarned$` → affiche toast inline 6s

---

## 13. Déploiement — Docker et CI/CD

### Commande principale

```bash
# Démarrage (dev et prod)
podman-compose up --build

# Production (ports internes masqués)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

**Note importante Podman :** `podman-compose up --build` réutilise le conteneur existant si l'image a le même tag → les nouvelles sources ne sont pas servies. Pattern obligatoire pour propager un changement :

```bash
podman stop <conteneur> && podman rm <conteneur>
podman-compose build --no-cache <service>
podman-compose up -d <service>
```

### Services Docker

| Service | Image | Ports | Dépend de |
|---|---|---|---|
| `codearena-db` | `postgres:16-alpine` | interne | — |
| `codearena-redis` | `redis:7-alpine` | interne | — |
| `codearena-api` | `backend/Dockerfile` | `5000:8080` | db + redis healthy |
| `codearena-hangfire` | `backend/Dockerfile.hangfire` | `5001:8080` | db + redis healthy |
| `codearena-frontend` | `frontend/Dockerfile` | `4200:80` | api |
| `codearena-pgadmin` | `dpage/pgadmin4` | `5050:80` | profile: tools |

**Volumes persistants :** `pgdata`, `redisdata`, `uploads`

### Dockerfiles

**API (multi-stage) :**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# dotnet restore → dotnet publish CodeArena.API

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# mkdir /app/uploads/seed, /app/uploads/submissions/src
# EXPOSE 8080
# USER supprimé (volumes Podman rootless owned by uid 0)
```

**Worker (multi-stage) :**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# dotnet restore → dotnet publish CodeArena.Worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# mkdir /app/uploads
# EXPOSE 8080
```

**Frontend (multi-stage) :**
```dockerfile
FROM node:24-alpine AS build
# npm ci --legacy-peer-deps
# ng build --configuration production

FROM nginx:alpine
# Copie du build Angular + nginx.conf
# EXPOSE 80
```

### Nginx (frontend)

Le conteneur frontend fait office de reverse-proxy pour l'API :

```nginx
location /api/       { proxy_pass http://api:8080/api/; }
location /swagger    { proxy_pass http://api:8080/swagger; }
location /uploads/   { proxy_pass http://api:8080/uploads/; expires 7d; }
location /hubs/ {
  proxy_pass http://api:8080/hubs/;
  proxy_http_version 1.1;          # Upgrade HTTP/1.1 pour WebSocket
  proxy_set_header Upgrade $http_upgrade;
  proxy_set_header Connection "upgrade";
  proxy_read_timeout 86400;        # Keep-alive WebSocket 24h
}
# SPA fallback
location / { try_files $uri $uri/ /index.html; }
```

### CI/CD GitHub Actions (`.github/workflows/deploy.yml`)

**Trigger :** push ou PR sur `main`

**Job 1 — build-and-test :**
- .NET 10 : `dotnet restore` + `dotnet build --configuration Release`
- Node 24 : `npm ci --legacy-peer-deps` + `ng build --configuration=production`

**Job 2 — deploy** (push main uniquement) :
```bash
# Sur VPS via SSH (appleboy/ssh-action)
cd /opt/codearena
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
# Health check /api/health avec 60s de timeout
```

**Secrets GitHub requis :** `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`, `VPS_PORT`

---

## 14. Variables d'environnement

Copier `.env.example` → `.env` à la racine.

| Variable | Exemple | Description |
|---|---|---|
| `POSTGRES_DB` | `codearena` | Nom de la base PostgreSQL |
| `POSTGRES_USER` | `codearena_user` | Utilisateur PostgreSQL |
| `POSTGRES_PASSWORD` | `changeme` | Mot de passe PostgreSQL |
| `JWT_SECRET` | `supersecret...` | Clé HMAC-SHA256 (min 32 chars) |
| `JWT_EXPIRY_HOURS` | `24` | Durée de vie du token JWT |
| `FRONTEND_URL` | `http://localhost:4200` | CORS — URL du frontend |
| `UPLOADS_PATH` | `/app/uploads` | Chemin absolu stockage fichiers (dans conteneur) |
| `APP_URL` | `https://codearena.cm` | URL base pour les liens dans les emails |
| `REDIS_CONNECTION` | `redis:6379` | Connexion Redis (nom du service Docker) |
| `SMTP_HOST` | `smtp-relay.brevo.com` | Hôte SMTP Brevo |
| `SMTP_PORT` | `587` | Port SMTP (StartTLS) |
| `SMTP_USER` | `b62dbe001@smtp-brevo.com` | Login SMTP Brevo (format spécifique, PAS l'email du compte) |
| `SMTP_PASSWORD` | `...` | Clé SMTP Brevo |
| `SMTP_FROM` | `CodeArena <noreply@...>` | Expéditeur des emails |
| `MAX_FILE_SIZE_MB` | `5` | Taille max résultat soumission |

**Rechargement `.env` :** `podman-compose down && podman-compose up -d` (pas `restart`).

---

## 15. Flux de données clés

### Flux 1 — Soumission d'un exercice

```
Participant clique "Soumettre"
  │
  ▼
Angular ProblemDetailComponent
  → FormData avec résultat.txt (+ source optionnel)
  → POST /api/problems/{id}/submit (Authorization: Bearer JWT)
  │
  ▼
ProblemsController.Submit()
  → Valide extension (.txt) + taille
  → Récupère userId depuis JWT claims
  → submissionService.SubmitAsync(userId, problemId, resultFile, sourceFile)
  │
  ▼
SubmissionService.SubmitAsync()
  1. Vérifie compétition Ongoing → 400 si non
  2. Vérifie UserProblemStatus.Solved == false → 409 si déjà résolu
  3. Sauvegarde resultFile via FileStorageService → "uploads/results/{guid}.txt"
  4. Lit OutputFileUrl du problème (path physique), compare byte-à-byte
  5. BEGIN TRANSACTION
     - Insère Submission (status = Accepted ou Wrong)
     - Si Accepted :
         a. UserProblemStatus.Solved = true, AttemptCount++
         b. User.TotalScore += problem.Points
     - COMMIT
  6. backgroundJobClient.Enqueue<IBadgeService>(CheckAndAward)   (Accepted seulement)
  7. backgroundJobClient.Enqueue<INotificationService>(CreateAsync) (toujours)
  8. return SubmissionResultDto → Controller → 200
  │
  ▼ (en parallèle, dans le Worker)
BadgeService.CheckAndAwardBadgesAsync(userId, problemId)
  → Vérifie les 7 conditions
  → Pour chaque badge gagné :
      - Insère UserBadge
      - NotificationService.CreateAsync(BadgeEarned)
         → DB insert + Redis PUBLISH notifications:push:{userId}
  │
  ▼ (dans l'API process)
RedisNotificationRelay reçoit le message Redis
  → HubContext.Clients.Group(userId).SendAsync("ReceiveNotification", dto)
  │
  ▼ (dans le navigateur)
Angular NotificationService.refresh$.next()
  → Si BadgeEarned : badgeEarned$.next()
     → ProblemDetailComponent toast "Badge débloqué !" (6s)
```

### Flux 2 — Transition automatique des compétitions

```
Hangfire (minutely) → CompetitionStatusJob.ExecuteAsync()
  │
  ├── Upcoming avec StartDate ≤ now
  │     → Status = Ongoing
  │     → Enqueue CreateAsync(CompetitionStarted) × users actifs
  │
  ├── Upcoming avec StartDate - 1h ≤ now ET StartReminderSentAt is null
  │     → StartReminderSentAt = now
  │     → Enqueue CreateAsync(CompetitionStarting) × users actifs
  │
  └── Ongoing avec EndDate ≤ now
        → Status = Finished
        → Enqueue LeaderboardBroadcastService.BroadcastAsync()
```

### Flux 3 — Authentification et navigation

```
POST /api/auth/login → JWT token (24h)
  → Angular AuthService stocke dans localStorage
  → JwtInterceptor injecte Authorization: Bearer sur chaque requête HTTP
  → BehaviorSubject<User> notifie le HeaderComponent
  → AuthGuard vérifie isAuthenticated() avant chaque route protégée
  → ErrorInterceptor intercepte 401 → logout + redirect /login
                             403 → redirect /forbidden
```

---

## 16. Patterns implémentés

### Clean Architecture (backend)

**Principe :** les dépendances pointent toujours vers le centre (Domain → Application → Infrastructure → API). Le domaine ne connaît pas EF Core, Redis, ni ASP.NET.

**Bénéfice :** remplacer PostgreSQL par SQL Server ou Redis par RabbitMQ ne touche que l'Infrastructure.

### Repository Pattern implicite via IAppDbContext

`IAppDbContext` expose les `DbSet<T>` directement. Les services Application l'utilisent comme un repository. Pas de classes Repository séparées — EF Core est le repository.

### CQRS léger

Pas de Mediatr. Les services `XxxService` ont des méthodes distinctes pour les lectures (Query) et les écritures (Command), mais tout est dans la même classe.

### Fire-and-forget via Hangfire

Tout traitement non critique pour la réponse HTTP (emails, badges, notifications) est enqueued dans Hangfire. L'API répond immédiatement, les jobs s'exécutent en arrière-plan avec retry automatique.

**Règle :** jamais `Task.Run(...)` ou `_ = SomeFireAndForgetAsync()` pour du code qui touche un DbContext — utiliser `IBackgroundJobClient.Enqueue`.

### Redis pub/sub pour la communication inter-processus

L'API et le Worker s'exécutent dans des processus différents. Redis est le bus de communication :
- Worker publie les notifications → API les reçoit → SignalR → navigateur
- Backplane SignalR Redis permet de scaler horizontalement sans état partagé

### Strategy Pattern — INotificationPusher / ILeaderboardPusher

L'interface est définie en Application (`INotificationPusher`), l'implémentation Redis en Infrastructure (`RedisPublishPusher`). On peut substituer une autre implémentation (ex: RabbitMQ) sans changer Application.

### Observer Pattern — RxJS Subjects (Angular)

`refresh$` et `badgeEarned$` sont des `Subject<void>`. Les composants s'y abonnent pour réagir aux événements sans couplage direct.

### Lazy Loading + Standalone Components (Angular)

Chaque feature est un module implicite : un composant standalone chargé à la demande. Pas de NgModule partagé → bundles plus petits, pas de rechargement de code inutile.

### Distributed Cache avec TTL (Redis)

Le leaderboard global est coûteux à calculer (JOIN + GROUP BY + tri). Il est mis en cache 30s dans Redis. La clé inclut les paramètres de filtrage pour des caches distincts par requête.

---

## 17. Conventions de code

### Backend

| Convention | Exemple |
|---|---|
| Noms des migrations explicites | `AddUserTotalScore`, `AddEmailVerificationAndPasswordReset` |
| DTOs séparés des entités | `CompetitionDto`, `ProblemDto` — jamais retourner une entité EF directement |
| Exceptions custom → codes HTTP | `NotFoundException` → 404, `ConflictException` → 409, `UnauthorizedException` → 401 |
| ILogger sur actions critiques | Soumission reçue, jugement, score mis à jour, badge attribué |
| FluentValidation | Validateur par classe de requête, validé manuellement dans le Controller |
| Claims JWT courts | `"role"` pas `ClaimTypes.Role`, `"sub"` pas `ClaimTypes.NameIdentifier` |
| Pas de DbContext dans Controller | Le Controller ne connaît que les `IXxxService` |

### Frontend

| Convention | Exemple |
|---|---|
| Pas de couleur hardcodée | `var(--color-primary)` pas `#FF5733` |
| 3 états obligatoires | `isLoading`, `error`, `data` dans chaque composant |
| Liens pseudos | `[routerLink]="['/u', username]"` partout où un pseudo est affiché |
| i18n obligatoire | Pas de string hardcodé dans les templates — toujours via `translate.instant()` ou `| translate` |
| Mobile-first | Media queries à partir de 375px |

### Nommage i18n

Deux espaces de noms :
- **Legacy (V1) :** `UPPER_CASE.DOT.NOTATION` (ex: `AUTH.LOGIN.TITLE`)
- **V2+ :** `lowercase.dot.notation` au niveau racine (ex: `notifications.title`, `badges.first-ac.name`)

Ne jamais imbriquer `lowercase` dans `UPPER_CASE`.

---

## 18. Pièges connus et décisions importantes

### `USER app` supprimé du Dockerfile backend

Volumes Podman rootless sont owned by uid 0. Ajouter `USER app` casserait l'accès aux volumes au runtime. **Ne pas remettre.**

### EF Core — GroupBy + Join non traduisible

```csharp
// MAUVAIS : crash runtime "could not be translated"
db.Submissions.GroupBy(s => s.UserId).Select(g => new {
    g.Key,
    Score = g.Join(db.Problems, ...).Sum(...)  // ← Join dans Select sur GroupBy
});

// CORRECT : Join d'abord, GroupBy ensuite
db.Submissions
    .Join(db.Problems, ...)
    .GroupBy(x => x.UserId)
    .Select(g => new { g.Key, Score = g.Sum(...) });
```

### `(ngSubmit)` sans FormsModule — soumission GET native

Sans `FormsModule` et sans `[formGroup]`, `(ngSubmit)` ne fonctionne pas — le navigateur fait une soumission GET native. **Utiliser toujours `(submit)="$event.preventDefault(); onHandler()"`.**

### Concurrence DbContext dans CompetitionStatusJob

L'ancien `CompetitionStatusUpdater` (`BackgroundService`) utilisait le DbContext du scope hôte, ce qui causait des `InvalidOperationException` lors de requêtes concurrentes. Solution : Hangfire crée un scope isolé par job. **Ne jamais utiliser `IServiceScopeFactory` dans un job Hangfire — Hangfire gère le scope automatiquement.**

### Fire-and-forget avec scope HTTP jetable

```csharp
// MAUVAIS : le scope HTTP est disposé avant la fin du Task
_ = _notificationService.CreateAsync(...); // ObjectDisposedException

// CORRECT : via Hangfire
_backgroundJobClient.Enqueue<INotificationService>(s => s.CreateAsync(..., CancellationToken.None));
```

### ImageSharp — version 3.x uniquement

Version 3.x = Apache 2.0 (libre). Version 4.x nécessite une clé de licence en Release. **Ne pas upgrader.**

### Rechargement .env

`podman-compose restart` ne relit pas le `.env`. Toujours utiliser `podman-compose down && podman-compose up -d`.

### `translate.currentLang` — Signal dans ngx-translate 18

`translate.currentLang` est un Signal, pas une simple string. Pour réagir aux changements de langue : utiliser `translate.onLangChange` (Observable) et non lire `currentLang` en dehors d'un contexte réactif.

### `ToastService.success/error` — chaîne déjà traduite

```typescript
// MAUVAIS
this.toast.success('AUTH.LOGIN.SUCCESS'); // affiche la clé brute

// CORRECT
this.toast.success(this.translate.instant('AUTH.LOGIN.SUCCESS'));
```

---

## 19. Roadmap et sprints

### Sprints terminés

| Sprint | Contenu | Date |
|---|---|---|
| Sprint 0 | Setup infrastructure, Docker, migrations, seed | 2026-08-19 |
| Sprint 1 | Auth (register/login/JWT), navigation Angular | 2026-08-19 |
| Sprint 2 | Home, compétitions, leaderboard global | 2026-08-19 |
| Sprint 3 | Exercices, soumissions, jugement | 2026-08-19 |
| Sprint 4 | Profil utilisateur, classement global filtré | 2026-08-19 |
| Sprint 5 | Back-office modérateur (CRUD compétitions/exercices) | 2026-08-19 |
| Sprint 6 | Administration, polish (404/403, toasts, i18n switch) | 2026-08-19 |
| Sprint V2-1 | Emails transactionnels, récupération compte | 2026-08-20 |
| Sprint V2-2 | Notifications in-app | 2026-08-21 |
| Sprint V2-3 | Gamification (badges, niveaux) | 2026-08-21 |
| Sprint INFRA | Redis cache, Hangfire Worker, SignalR temps réel | 2026-08-21 |

### Sprint en cours

**Sprint V2-4 — Compétitions privées**

### Backlog connu

- Page `/competitions` (liste dédiée avec filtres)
- Datalist région/école sur le formulaire d'inscription
- Hero card "compétition en cours" sur la home

---

## 20. Production — VPS Hostinger

### Infrastructure hébergée

```
URL publique : https://codearena.bissaye.online
VPS          : Hostinger — 195.35.3.89 (Ubuntu)
Domaine      : DNS A record codearena → 195.35.3.89 (TTL 3600s)
SSL          : Let's Encrypt (certbot certonly --standalone), renouvellement cron 3h00
```

**Firewall (Hostinger panel) :** seuls les ports 22 (SSH), 80 (HTTP) et 443 (HTTPS) sont ouverts. PostgreSQL (5432), API (5000), Hangfire (5001), et Frontend (4200) ne sont PAS accessibles depuis internet.

### Nginx VPS — reverse proxy HTTPS

Le VPS fait tourner un **nginx hôte** (distinct du nginx dans le conteneur frontend) qui est le point d'entrée HTTPS :

```
Internet HTTPS:443 / WSS
   ↓
nginx (VPS hôte)
   ├── /api/*      → localhost:5000  (conteneur API .NET)
   ├── /hubs/*     → localhost:5000  (SignalR WebSocket, headers Upgrade + Connection)
   ├── /uploads/*  → localhost:5000  (fichiers statiques, cache 7j)
   ├── /hangfire/* → localhost:5000  (dashboard Hangfire, protégé JWT Admin)
   ├── /swagger/*  → localhost:5000  (API docs)
   └── /*          → localhost:4200  (conteneur Frontend Angular)
```

**Headers critiques pour SignalR (WebSocket) :**
```nginx
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
proxy_read_timeout 86400;   # 24h keep-alive
```

**Config SSL :**
```nginx
ssl_certificate /etc/letsencrypt/live/codearena.bissaye.online/fullchain.pem;
ssl_certificate_key /etc/letsencrypt/live/codearena.bissaye.online/privkey.pem;
ssl_protocols TLSv1.2 TLSv1.3;
```

**Headers de sécurité :**
```nginx
add_header X-Frame-Options "SAMEORIGIN";
add_header X-Content-Type-Options "nosniff";
add_header Referrer-Policy "strict-origin-when-cross-origin";
```

### Docker en production

```bash
# Lancer (prod — ports DB/Redis masqués)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# JAMAIS podman en prod — le VPS utilise Docker
```

`docker-compose.prod.yml` masque les ports exposés en dev (5432, 6379) pour ne les laisser que sur le réseau interne Docker.

Utilisateur de déploiement : **`deployer`** (pas root), membre du groupe `docker`, homedir `/home/deployer`, code dans `/opt/codearena`.

### CI/CD GitHub Actions

**Déclencheur :** push ou PR vers `main`

**Job 1 — `build-and-test`** (sur toutes les branches) :
- `.NET 10` : `dotnet restore` + `dotnet build --configuration Release`
- `Node 24` : `npm ci --legacy-peer-deps` + `ng build --configuration=production`

**Job 2 — `deploy`** (push `main` uniquement) :
```bash
# Sur le VPS via SSH (appleboy/ssh-action)
cd /opt/codearena
git pull origin main
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
# health check /api/health (60s timeout)
```

**Secrets GitHub requis :**

| Secret | Valeur |
|---|---|
| `VPS_HOST` | `195.35.3.89` |
| `VPS_USER` | `deployer` |
| `VPS_SSH_KEY` | Contenu de `~/.ssh/deploy_key` (Ed25519) |
| `VPS_PORT` | `22` |

### Commandes de maintenance VPS

```bash
# Connexion SSH
ssh root@195.35.3.89
# ou terminal web hpanel.hostinger.com → VPS → Terminal

# État des conteneurs
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

# Logs en temps réel
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs api --tail=50

# Mise à jour manuelle (sans attendre CI/CD)
su - deployer
cd /opt/codearena
git pull origin main
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Sauvegarde base de données
docker exec codearena-db pg_dump -U codearena_user codearena > backup_$(date +%Y%m%d).sql

# Vérifier Redis
docker exec codearena-redis redis-cli ping             # → PONG
docker exec codearena-redis redis-cli keys "leaderboard*"  # → clés cache

# Seed démo en production (une seule fois)
TOKEN=$(curl -s -X POST https://codearena.bissaye.online/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"<mdp>"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
curl -X POST https://codearena.bissaye.online/api/admin/seed-demo \
  -H "Authorization: Bearer $TOKEN"

# Vérifier jobs Hangfire en base
docker exec codearena-db psql -U codearena_user -d codearena \
  -c "SELECT key FROM hangfire.hash WHERE key LIKE 'recurring-job:%';"

# Certificat SSL — vérifier expiration
certbot certificates

# Nginx — tester et recharger
nginx -t && systemctl reload nginx
```

### Procédure — Refaire le déploiement sur un nouveau VPS

```
1.  DNS         → Enregistrement A dans le panel du domaine
2.  Firewall    → Ouvrir ports 22, 80, 443 uniquement
3.  Docker      → curl -fsSL https://get.docker.com | sh + apt install docker-compose-plugin
4.  Utilisateur → useradd -m -s /bin/bash deployer + usermod -aG docker deployer
5.  Dossier     → mkdir -p /opt/codearena && chown deployer:deployer /opt/codearena
6.  Clés SSH    → ssh-keygen -t ed25519 deploy_key + authorized_keys
7.  Secrets     → openssl rand -base64 32 (POSTGRES_PASSWORD) + openssl rand -base64 64 (JWT_SECRET)
8.  Certbot     → apt install certbot + certbot certonly --standalone -d <domaine>
9.  Cron SSL    → 0 3 * * * certbot renew --quiet && docker compose ... restart frontend
10. Clone       → git clone https://TOKEN@github.com/... /opt/codearena
11. .env        → cp .env.example .env + remplir valeurs prod
12. nginx VPS   → apt install nginx + config sites-available + désactiver default + nginx -t
13. Docker up   → docker compose -f ... -f docker-compose.prod.yml up -d --build
14. GitHub      → Deploy Key (clé pub) + 4 Secrets Actions + workflow déjà présent
15. Test final  → push commit → GitHub Actions → https://<domaine>
```

---

## Démarrage rapide pour un nouveau développeur

```bash
# 1. Cloner
git clone <repo>
cd CodeArenaCamer

# 2. Configurer l'environnement
cp .env.example .env
# Éditer .env : JWT_SECRET, SMTP_*, POSTGRES_PASSWORD

# 3. Lancer (tout démarre, migrations appliquées, seed exécuté)
podman-compose up --build

# 4. Accès
# Frontend        : http://localhost:4200
# API Swagger     : http://localhost:5000/swagger
# Hangfire Worker : http://localhost:5001/hangfire
# PgAdmin         : podman-compose --profile tools up -d → http://localhost:5050

# Compte admin de démo : admin / Admin123!

# 5. Seed de démo (présentation client — une seule fois)
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
curl -X POST http://localhost:5000/api/admin/seed-demo \
  -H "Authorization: Bearer $TOKEN"

# 6. Ajouter une migration EF Core
cd backend
dotnet ef migrations add NomDeLaMigration \
  --project CodeArena.Infrastructure \
  --startup-project CodeArena.API
```

### Lire avant de coder

| Fichier | Contenu |
|---|---|
| `CLAUDE.md` | Règles du projet, commandes, décisions techniques par sprint |
| `context.md` | User Stories, endpoints détaillés, modèle de données |
| `design-system.md` | Tokens CSS, composants Angular, palette, typographie |
| `ARCHITECTURE.md` | Ce document |