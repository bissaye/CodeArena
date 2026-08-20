# CodeArena Cameroun

Plateforme de compétition de programmation open-source, inspirée de Codeforces et CodeChef, destinée au marché camerounais.

---

## Prérequis

| Outil | Version testée |
|---|---|
| Podman (ou Docker) | Podman 5.8+ / Docker 24+ |
| podman-compose (ou docker compose) | 1.5+ / v2 |
| .NET SDK | 10.0 (uniquement pour modifier le backend en local) |
| Node.js | 24.x (uniquement pour modifier le frontend en local) |

> L'application tourne entièrement dans des conteneurs — seuls Podman/Docker sont nécessaires pour la démo.

---

## Installation locale (3 commandes)

```bash
git clone <url-du-dépôt> codearena
cd codearena
cp .env.example .env
podman-compose up --build
```

C'est tout. Les conteneurs démarrent, la migration est appliquée automatiquement et les données de démonstration sont insérées.

> Si vous utilisez Docker : remplacez `podman-compose` par `docker compose` partout.

---

## URLs de démo

| Service | URL | Description |
|---|---|---|
| **Application web** | http://localhost:4200 | Interface Angular |
| **API Swagger** | http://localhost:5000/swagger | Documentation interactive |
| **API Health** | http://localhost:5000/api/health | Vérification santé |
| **PgAdmin** | http://localhost:5050 | Interface base de données (optionnel) |

### Lancer PgAdmin

```bash
podman-compose --profile tools up -d pgadmin
```

---

## Comptes de démonstration

| Rôle | Username | Mot de passe |
|---|---|---|
| **Admin** | `admin` | `Admin123!` |
| **Modérateur** | `moderateur1` | `Test123!` |
| **Participant** | `alice_yaounde` | `Test123!` |
| Participant | `bob_douala` | `Test123!` |
| Participant | `charlie_bafang` | `Test123!` |

---

## Variables d'environnement

Copiez `.env.example` vers `.env` et ajustez si nécessaire :

```env
POSTGRES_DB=codearena
POSTGRES_USER=codearena_user
POSTGRES_PASSWORD=changeme           # Changer en production
JWT_SECRET=supersecretkey_min32chars_codearena2026  # Minimum 32 caractères
JWT_EXPIRY_HOURS=24
FRONTEND_URL=http://localhost:4200   # URL du frontend (pour CORS)
UPLOADS_PATH=/app/uploads            # Chemin interne au conteneur
```

---

## Déploiement VPS Linux

### 1. Préparer le serveur

```bash
# Ubuntu/Debian
apt update && apt install -y podman podman-compose

# Ou Docker
curl -fsSL https://get.docker.com | sh
```

### 2. Cloner et configurer

```bash
git clone <url-du-dépôt> /opt/codearena
cd /opt/codearena
cp .env.example .env
nano .env   # Configurer les variables de production
```

Variables critiques à modifier pour la production :
```env
POSTGRES_PASSWORD=<mot-de-passe-fort-aléatoire>
JWT_SECRET=<chaîne-aléatoire-min-32-chars>
FRONTEND_URL=https://votre-domaine.cm
```

### 3. Lancer en production

```bash
podman-compose up -d
```

### 4. Configurer un reverse proxy nginx (optionnel mais recommandé)

```nginx
server {
    listen 80;
    server_name votre-domaine.cm;

    location / {
        proxy_pass http://127.0.0.1:4200;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        client_max_body_size 12M;
    }

    location /uploads/ {
        proxy_pass http://127.0.0.1:5000/uploads/;
        proxy_cache_valid 200 7d;
    }
}
```

Obtenir un certificat HTTPS avec Certbot :
```bash
apt install -y certbot python3-certbot-nginx
certbot --nginx -d votre-domaine.cm
```

### 5. Persister les données

Les volumes Docker/Podman (`pgdata`, `uploads`) persistent automatiquement entre les redémarrages. Pour sauvegarder la base :

```bash
podman exec codearena-db pg_dump -U codearena_user codearena > backup_$(date +%Y%m%d).sql
```

### 6. Mettre à jour l'application

```bash
cd /opt/codearena
git pull
podman-compose down
podman-compose up --build -d
```

---

## Architecture

```
CodeArenaCamer/
├── backend/          # ASP.NET Core 10 — Clean Architecture
│   ├── CodeArena.Domain/        # Entités, Enums
│   ├── CodeArena.Application/   # Services, DTOs, Interfaces
│   ├── CodeArena.Infrastructure/ # EF Core, PostgreSQL, FileStorage
│   └── CodeArena.API/           # Controllers, Program.cs
├── frontend/         # Angular 21 — Standalone Components
│   └── src/app/
│       ├── core/     # Auth, Guards, Interceptors, Services
│       ├── shared/   # Composants réutilisables, Pipes
│       └── features/ # Pages (auth, home, competition, problem…)
├── docker-compose.yml
├── .env.example
└── README.md
```

## Stack technique

| Couche | Technologie |
|---|---|
| Backend | ASP.NET Core 10, Entity Framework Core 10, PostgreSQL 16 |
| Frontend | Angular 21, SCSS, ngx-translate |
| Auth | JWT HS256, BCrypt |
| Conteneurs | Podman / Docker + nginx |
| Markdown | Markdig (serveur, sanitisation XSS), marked (client) |
| Images | SixLabors.ImageSharp 3.x (resize avatar) |

---

## Développement local sans conteneurs

### Backend

```bash
cd backend
dotnet run --project CodeArena.API
# API disponible sur http://localhost:5000
```

Nécessite une instance PostgreSQL locale. Configurer `appsettings.Development.json` avec la chaîne de connexion.

### Frontend

```bash
cd frontend
npm install
ng serve
# App disponible sur http://localhost:4200
```

Le proxy Angular (`proxy.conf.json`) redirige `/api` vers `http://localhost:5000`.

---

## Fonctionnalités

- ✅ Inscription / Connexion (JWT)
- ✅ Profil utilisateur avec avatar (resize 200×200)
- ✅ Page d'accueil adaptative (hero card / grille selon le nombre de compétitions en cours)
- ✅ Page `/competitions` — liste paginée avec indicateurs visuels de statut
- ✅ Exercices avec soumission de résultats (comparaison fichier)
- ✅ Classement global avec filtres (pays, région, école, compétition)
- ✅ Datalist région/école (autocomplete sur inscription, profil, classement)
- ✅ Back-office modérateur (création compétitions/exercices, Markdown, aperçu live)
- ✅ Administration (gestion des modérateurs)
- ✅ Notifications toast globales
- ✅ Interface bilingue FR/EN
- ✅ Pages d'erreur 404/403

---

*Développé pour le marché camerounais — CodeArena Cameroun 2026*
