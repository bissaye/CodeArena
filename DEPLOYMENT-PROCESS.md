# DEPLOYMENT-PROCESS.md — CodeArena Cameroun
> Documentation complète de tout ce qu'on a fait pour déployer l'app en production.
> Chaque commande est expliquée — pourquoi, comment, et ce qui se passe en coulisses.
> À lire si tu dois refaire ce déploiement sur un autre VPS ou comprendre ce qui tourne.

---

## Vue d'ensemble

Voici ce qu'on a mis en place :

```
Ton PC (code)
    ↓ git push origin main
GitHub (repo privé)
    ↓ GitHub Actions (CI/CD)
    ↓ Build .NET + Angular
    ↓ SSH vers VPS
VPS Hostinger (195.35.3.89)
    ↓ git pull
    ↓ docker compose up --build
    ├── conteneur PostgreSQL (base de données + jobs Hangfire)
    ├── conteneur Redis (cache distribué + pub/sub SignalR)
    ├── conteneur API (.NET — API + SignalR hub + Hangfire 3 workers)
    ├── conteneur Worker (Hangfire 10 workers — emails, badges, notifications)
    └── conteneur Frontend (Angular + nginx)
         ↑
      nginx (VPS) — reverse proxy HTTPS + WebSocket SignalR
         ↑
   https://codearena.bissaye.online
```

---

## PARTIE 1 — DNS : faire pointer le domaine vers le VPS

### Concept
Le DNS (Domain Name System) est l'annuaire d'internet. Quand quelqu'un tape `codearena.bissaye.online` dans son navigateur, son ordinateur demande à un serveur DNS "quelle est l'adresse IP de ce domaine ?". On doit lui dire que c'est l'IP de notre VPS.

### Ce qu'on a fait
Sur hpanel.hostinger.com → Domains → bissaye.online → DNS Manager :

| Type | Nom | Valeur | TTL |
|---|---|---|---|
| A | codearena | 195.35.3.89 | 3600 |

**Explication :**
- **Type A** : enregistrement qui associe un nom à une adresse IPv4
- **codearena** : le sous-domaine (donne `codearena.bissaye.online`)
- **195.35.3.89** : l'IP de notre VPS
- **TTL 3600** : durée de vie du cache en secondes (1 heure) — après ce délai, les DNS du monde entier mettront à jour leur cache si on change l'IP

### Vérification
```bash
ping codearena.bissaye.online
# Doit répondre depuis 195.35.3.89
# Le ping timeout est normal si le firewall bloque ICMP — l'important c'est l'IP
```

### Propagation DNS
Le changement DNS peut prendre de 5 minutes à 48 heures selon les fournisseurs. En pratique avec Hostinger c'est souvent 5-30 minutes.

---

## PARTIE 2 — Firewall Hostinger : ouvrir les bons ports

### Concept
Par défaut, Hostinger bloque TOUT le trafic entrant sur le VPS. C'est une bonne pratique de sécurité — on n'ouvre que ce dont on a besoin.

Un **port** c'est comme une porte dans un immeuble. Chaque service écoute sur un port spécifique :
- Port 22 → SSH (accès terminal)
- Port 80 → HTTP (web non sécurisé)
- Port 443 → HTTPS (web sécurisé)

### Ce qu'on a fait
Sur hpanel.hostinger.com → VPS → Firewall :

| Action | Protocol | Port | Source |
|---|---|---|---|
| Accept | TCP | 22 | Any |
| Accept | TCP | 80 | Any |
| Accept | TCP | 443 | Any |
| Drop | Any | Any | Any |

**Explication :**
- **Accept 22** : permet la connexion SSH pour administrer le serveur
- **Accept 80** : permet le trafic HTTP (nginx redirige vers HTTPS)
- **Accept 443** : permet le trafic HTTPS (l'app en production)
- **Drop Any** : bloque tout le reste — PostgreSQL (5432), API (5000), Frontend (4200) ne sont PAS accessibles depuis internet, uniquement en interne

**Important** : Le bouton **Synchronize** applique les règles sur le serveur. Sans synchronisation, les règles sont sauvegardées mais pas actives.

### Pourquoi on n'expose pas les autres ports
- **5432 (PostgreSQL)** : la base de données ne doit jamais être accessible depuis internet. Seul l'API container doit y accéder, via le réseau interne Docker.
- **5000 (API)** : l'API est accessible via nginx sur /api/, pas directement.
- **4200 (Frontend)** : idem, accessible via nginx sur /.

---

## PARTIE 3 — Installation de Docker sur le VPS

### Concept
Docker est un système de **conteneurisation**. Un conteneur c'est comme une boîte isolée qui contient une application avec tout ce dont elle a besoin (dépendances, runtime, config). L'avantage : "ça marche sur mon PC" devient "ça marche partout".

Docker Compose est un outil qui permet de définir et gérer plusieurs conteneurs ensemble (notre API + PostgreSQL + Frontend) dans un seul fichier `docker-compose.yml`.

### Commandes exécutées

```bash
# 1. Mise à jour de la liste des paquets disponibles
apt update
# apt = gestionnaire de paquets Ubuntu (comme un App Store en ligne de commande)
# update = télécharge la liste des versions disponibles (ne met rien à jour encore)

# 2. Mise à jour de tous les paquets installés
apt upgrade -y
# upgrade = installe les nouvelles versions
# -y = répond "oui" automatiquement à toutes les confirmations

# 3. Installation de Docker via le script officiel
curl -fsSL https://get.docker.com | sh
# curl = télécharge le contenu d'une URL
# -fsSL = options : fail silently, silent, follow redirects, SSL
# https://get.docker.com = script officiel Docker qui détecte l'OS et installe la bonne version
# | sh = pipe vers sh = exécute le script téléchargé

# 4. Installation du plugin Docker Compose
apt install -y docker-compose-plugin
# docker-compose-plugin = la version moderne de Docker Compose (intégrée à docker)
# Permet d'utiliser : docker compose (sans tiret)
# L'ancienne version était docker-compose (avec tiret) — on évite

# 5. Activer Docker au démarrage du serveur
systemctl enable docker
# systemctl = gestionnaire de services Linux (systemd)
# enable = dit au système de démarrer Docker automatiquement au boot
# Sans ça, Docker s'arrête si le VPS redémarre et l'app ne repart pas

# 6. Démarrer Docker maintenant
systemctl start docker
# start = démarre le service immédiatement (sans attendre le prochain reboot)
```

### Vérification
```bash
docker --version
# → Docker version 29.7.2

docker compose version
# → Docker Compose version v5.5.0
```

---

## PARTIE 4 — Créer un utilisateur de déploiement

### Concept
Travailler en `root` est dangereux — une erreur ou une faille de sécurité et l'attaquant a accès à tout le serveur. On crée un utilisateur `deployer` avec uniquement les droits nécessaires :
- Accès à Docker (pour lancer les conteneurs)
- Accès au dossier `/opt/codearena` (pour le code)
- Pas de droits root

### Commandes exécutées

```bash
# 1. Créer l'utilisateur deployer
useradd -m -s /bin/bash deployer
# useradd = créer un utilisateur
# -m = créer son dossier home (/home/deployer)
# -s /bin/bash = définir bash comme shell par défaut

# 2. Ajouter deployer au groupe docker
usermod -aG docker deployer
# usermod = modifier un utilisateur existant
# -aG = append to Group (ajouter à un groupe sans retirer des autres)
# docker = le groupe qui donne accès à Docker
# Sans ça, deployer ne peut pas lancer docker compose

# 3. Créer le dossier de l'application
mkdir -p /opt/codearena
# mkdir = make directory (créer un dossier)
# -p = parents (crée les dossiers parents si nécessaire)
# /opt = convention Linux pour les applications tierces

# 4. Donner la propriété du dossier à deployer
chown deployer:deployer /opt/codearena
# chown = change owner (changer le propriétaire)
# deployer:deployer = propriétaire:groupe
# Sans ça, deployer ne peut pas écrire dans /opt/codearena

# Vérification
id deployer
# → uid=1000(deployer) gid=1000(deployer) groups=1000(deployer),988(docker)
```

---

## PARTIE 5 — Générer les clés SSH pour GitHub Actions

### Concept
GitHub Actions doit se connecter au VPS pour déployer. On ne peut pas mettre un mot de passe dans le code — ce serait une faille de sécurité. On utilise une **paire de clés SSH** :

- **Clé privée** → gardée secrète dans GitHub Secrets (jamais dans le code)
- **Clé publique** → mise sur le VPS dans `authorized_keys`

Quand GitHub Actions se connecte, il prouve son identité avec la clé privée. Le VPS vérifie avec la clé publique. Pas de mot de passe échangé sur le réseau.

### Commandes exécutées

```bash
# Passer sur l'utilisateur deployer
su - deployer
# su = switch user
# - = charge l'environnement complet de deployer (variables, dossier home, etc.)

# Générer la paire de clés
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/deploy_key -N ""
# ssh-keygen = générateur de clés SSH
# -t ed25519 = algorithme Ed25519 (moderne, rapide, sécurisé — meilleur que RSA)
# -C "github-actions-deploy" = commentaire pour identifier la clé
# -f ~/.ssh/deploy_key = nom du fichier (crée deploy_key et deploy_key.pub)
# -N "" = passphrase vide (obligatoire pour l'automatisation — GitHub Actions ne peut pas taper un mot de passe)

# Résultat : deux fichiers créés
# ~/.ssh/deploy_key     → clé PRIVÉE (à mettre dans GitHub Secrets)
# ~/.ssh/deploy_key.pub → clé PUBLIQUE (à autoriser sur le VPS)

# Autoriser la clé publique sur le VPS
cat ~/.ssh/deploy_key.pub >> ~/.ssh/authorized_keys
# cat = afficher le contenu du fichier
# >> = append (ajouter à la fin du fichier, sans écraser)
# authorized_keys = fichier que SSH consulte pour autoriser les connexions

# Sécuriser les permissions
chmod 700 ~/.ssh
# chmod = change mode (changer les permissions)
# 700 = rwx------ = seul le propriétaire peut lire/écrire/exécuter le dossier

chmod 600 ~/.ssh/authorized_keys
# 600 = rw------- = seul le propriétaire peut lire/écrire le fichier
# SSH refuse de fonctionner si les permissions sont trop ouvertes (sécurité)

# Afficher la clé privée (pour la copier dans GitHub Secrets)
cat ~/.ssh/deploy_key
# Copier TOUT le contenu : de -----BEGIN OPENSSH PRIVATE KEY----- 
# jusqu'à -----END OPENSSH PRIVATE KEY-----
```

---

## PARTIE 6 — Générer les secrets de production

### Concept
En production, les mots de passe doivent être :
- **Aléatoires** : pas de "password123"
- **Longs** : minimum 32 caractères
- **Uniques** : différents de l'environnement de dev

`openssl rand` génère des données aléatoires cryptographiquement sûres.

### Commandes exécutées

```bash
# Générer un mot de passe pour PostgreSQL (32 bytes = ~44 chars en base64)
openssl rand -base64 32
# openssl = outil cryptographique
# rand = générer des données aléatoires
# -base64 = encoder en base64 (lettres + chiffres + /+= — utilisable partout)
# 32 = 32 bytes de données aléatoires

# Générer le secret JWT (64 bytes = ~88 chars en base64)
openssl rand -base64 64
# Plus long pour le JWT car il signe tous les tokens d'authentification
# Si ce secret est compromis, n'importe qui peut créer de faux tokens
```

**Ces deux valeurs sont allées dans :**
1. Le fichier `.env` sur le VPS (`POSTGRES_PASSWORD` et `JWT_SECRET`)
2. Les secrets GitHub Actions si besoin

---

## PARTIE 7 — Installer Certbot et obtenir le certificat SSL

### Concept
**SSL/TLS** (le "S" dans HTTPS) chiffre les communications entre le navigateur et le serveur. Sans SSL :
- Les mots de passe transitent en clair sur le réseau
- Les navigateurs affichent "Site non sécurisé"
- Google déclasse le site dans les résultats

**Let's Encrypt** est une autorité de certification gratuite et automatisée. **Certbot** est l'outil officiel pour obtenir et renouveler ces certificats.

### Pourquoi AVANT Docker
Certbot doit prouver qu'on contrôle le domaine en démarrant un serveur web temporaire sur le port 80. Si Docker tournait déjà, nginx prendrait le port 80 et Certbot ne pourrait pas démarrer.

### Commandes exécutées

```bash
# Installer Certbot
apt install -y certbot
# certbot = outil officiel Let's Encrypt

# Obtenir le certificat SSL
certbot certonly --standalone \
  -d codearena.bissaye.online \
  --non-interactive \
  --agree-tos \
  -m ton@email.com
# certonly = obtenir le certificat seulement (sans configurer nginx automatiquement)
# --standalone = Certbot démarre son propre serveur web temporaire sur le port 80
# -d codearena.bissaye.online = le domaine pour lequel on veut le certificat
# --non-interactive = pas de questions interactives
# --agree-tos = accepter les conditions d'utilisation
# -m = email pour les alertes d'expiration

# Résultat : 4 fichiers dans /etc/letsencrypt/live/codearena.bissaye.online/
# cert.pem      = le certificat du domaine
# chain.pem     = la chaîne de certification intermédiaire
# fullchain.pem = cert.pem + chain.pem (c'est ce qu'on donne à nginx)
# privkey.pem   = la clé privée du certificat (garder secrète)

# Configurer le renouvellement automatique
(crontab -l 2>/dev/null; echo "0 3 * * * certbot renew --quiet && docker compose -f /opt/codearena/docker-compose.prod.yml restart frontend") | crontab -
# crontab = planificateur de tâches Linux
# 0 3 * * * = tous les jours à 3h00 du matin
# certbot renew = renouvelle les certificats qui expirent dans moins de 30 jours
# --quiet = sans output (pour ne pas spammer les logs)
# && = si le renouvellement réussit, alors recharger nginx
# Les certificats Let's Encrypt expirent après 90 jours — le cron les renouvelle automatiquement
```

---

## PARTIE 8 — Cloner le repo sur le VPS

### Concept
Le code doit être présent sur le VPS pour que Docker puisse construire les images. On clone le repo GitHub une première fois manuellement. Ensuite GitHub Actions fera `git pull` à chaque déploiement.

### Commandes exécutées

```bash
# Passer sur deployer
su - deployer
cd /opt/codearena

# Cloner avec le Personal Access Token dans l'URL
git clone https://TON_TOKEN@github.com/bissaye/CodeArena.git .
# git clone = copier un repo distant en local
# https://TOKEN@github.com = authentification par token dans l'URL
# Le . à la fin = cloner dans le dossier courant (pas créer un sous-dossier)

# Configurer SSH pour GitHub (pour les git pull suivants)
cat > ~/.ssh/config << 'EOF'
Host github.com
  IdentityFile ~/.ssh/deploy_key
  StrictHostKeyChecking no
EOF
# Ce fichier dit à SSH : "quand tu te connectes à github.com, utilise la clé deploy_key"
# StrictHostKeyChecking no = ne pas demander confirmation à la première connexion

chmod 600 ~/.ssh/config
# SSH refuse d'utiliser le fichier config s'il est trop permissif

# Changer l'URL du remote de HTTPS vers SSH
git remote set-url origin git@github.com:bissaye/CodeArena.git
# git remote = gestion des repos distants
# set-url = changer l'URL du remote "origin"
# git@github.com = connexion SSH à GitHub (utilise la clé configurée dans ~/.ssh/config)
# Avantage : plus besoin de token dans l'URL, la clé SSH fait l'authentification

# Tester
git pull origin main
# → Already up to date. = succès
```

---

## PARTIE 9 — Configurer les variables d'environnement

### Concept
Le fichier `.env` contient les secrets et la configuration spécifique à l'environnement (dev vs prod). Il n'est **jamais** commité dans Git (il est dans `.gitignore`) — chaque environnement a le sien.

Docker Compose lit automatiquement ce fichier et injecte les variables dans les conteneurs.

### Ce qu'on a créé

```bash
nano /opt/codearena/.env
# nano = éditeur de texte en terminal
# Ctrl+X → Y → Enter pour sauvegarder et quitter
```

Contenu du `.env` de production :
```env
POSTGRES_DB=codearena
# Nom de la base de données PostgreSQL

POSTGRES_USER=codearena_user
# Utilisateur PostgreSQL (pas root)

POSTGRES_PASSWORD=<valeur openssl rand -base64 32>
# Mot de passe fort généré aléatoirement

JWT_SECRET=<valeur openssl rand -base64 64>
# Secret pour signer les tokens JWT d'authentification
# Si compromis → tous les tokens existants sont invalides et recréables

JWT_EXPIRY_HOURS=24
# Durée de validité d'un token JWT (24h = l'utilisateur reste connecté 24h)

FRONTEND_URL=https://codearena.bissaye.online
# URL du frontend — utilisée par l'API pour configurer CORS

UPLOADS_PATH=/app/uploads
# Chemin interne au conteneur API où stocker les fichiers uploadés
# (avatars, fichiers de soumission)

REDIS_CONNECTION=redis:6379
# Connexion au conteneur Redis (host:port interne Docker)
# Jamais exposé publiquement — réseau interne uniquement

APP_URL=https://codearena.bissaye.online
# URL de l'app — utilisée pour générer des liens dans les emails

# Email SMTP (Brevo)
SMTP_HOST=smtp-relay.brevo.com
SMTP_PORT=587
SMTP_USER=<login_brevo@smtp-brevo.com>
SMTP_PASSWORD=<clé_smtp_brevo>
SMTP_FROM=CodeArena <noreply@bissaye.online>
```

---

## PARTIE 10 — Installer et configurer nginx sur le VPS

### Concept
On a **deux nginx** dans notre architecture — c'est source de confusion, expliquons :

1. **nginx dans le conteneur Frontend** : sert les fichiers Angular (HTML, JS, CSS) sur le port 80 interne du conteneur
2. **nginx sur le VPS (hôte)** : le **reverse proxy** — il reçoit les connexions HTTPS depuis internet et les redirige vers les bons conteneurs

Le nginx du VPS est le "portier" de l'application :
- Gère le SSL (certificats Let's Encrypt)
- Redirige HTTP → HTTPS
- Route `/api/` vers le conteneur API (port 5000)
- Route `/` vers le conteneur Frontend (port 4200)

### Commandes exécutées

```bash
# Installer nginx
apt install -y nginx
# nginx = serveur web / reverse proxy très performant

# Créer la config pour CodeArena
nano /etc/nginx/sites-available/codearena
# sites-available = dossier où on met les configs des sites (actifs ou non)
# sites-enabled = dossier avec des liens symboliques vers les configs actives
# Cette séparation permet d'activer/désactiver un site sans supprimer sa config

# Activer la config (créer un lien symbolique)
ln -s /etc/nginx/sites-available/codearena /etc/nginx/sites-enabled/
# ln -s = créer un lien symbolique (comme un raccourci)
# sites-enabled/codearena pointe vers sites-available/codearena

# Désactiver la page par défaut nginx (qui prenait la priorité)
rm /etc/nginx/sites-enabled/default
# Le site "default" affichait "Welcome to nginx!" au lieu de notre app

# Tester la config nginx (syntaxe)
nginx -t
# → nginx: configuration file /etc/nginx/nginx.conf syntax is ok
# → nginx: configuration file /etc/nginx/nginx.conf test is successful
# TOUJOURS tester avant de recharger — une erreur de syntaxe crasherait nginx

# Activer nginx au démarrage et le démarrer
systemctl enable nginx && systemctl start nginx

# Recharger nginx après modification de config
systemctl reload nginx
# reload = recharge la config sans couper les connexions actives (graceful)
# restart = arrête et redémarre (coupe les connexions) — à éviter en prod
```

### Contenu de la config nginx expliqué

```nginx
# Bloc 1 : Redirection HTTP → HTTPS
server {
    listen 80;                              # Écoute sur le port 80 (HTTP)
    server_name codearena.bissaye.online;   # Pour ce domaine uniquement
    return 301 https://$host$request_uri;  # Redirection permanente vers HTTPS
    # 301 = redirection permanente (les navigateurs et Google mémorisent)
}

# Bloc 2 : Serveur HTTPS principal
server {
    listen 443 ssl;                          # Écoute sur le port 443 (HTTPS)
    server_name codearena.bissaye.online;

    # Certificats Let's Encrypt
    ssl_certificate /etc/letsencrypt/live/codearena.bissaye.online/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/codearena.bissaye.online/privkey.pem;

    # Versions SSL acceptées (TLS 1.2 et 1.3 uniquement — les anciennes sont vulnérables)
    ssl_protocols TLSv1.2 TLSv1.3;

    # Headers de sécurité
    add_header X-Frame-Options "SAMEORIGIN";         # Empêche l'intégration dans une iframe (clickjacking)
    add_header X-Content-Type-Options "nosniff";     # Empêche le navigateur de "deviner" le type de fichier
    add_header Referrer-Policy "strict-origin-when-cross-origin"; # Contrôle les infos envoyées au site suivant

    # Route vers le frontend Angular
    location / {
        proxy_pass http://localhost:4200;  # Redirige vers le conteneur frontend
        proxy_set_header Host $host;       # Transmet le nom de domaine au conteneur
        proxy_set_header X-Real-IP $remote_addr;  # Transmet l'IP réelle du client
        proxy_set_header X-Forwarded-Proto $scheme; # Indique que la connexion originale était HTTPS
    }

    # Route vers l'API
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        client_max_body_size 12M;  # Limite la taille des uploads (fichiers de soumission)
    }

    # Route vers les fichiers uploadés (avatars)
    location /uploads/ {
        proxy_pass http://localhost:5000/uploads/;
        proxy_cache_valid 200 7d;
    }

    # Route SignalR WebSocket — OBLIGATOIRE pour les notifications temps réel
    # Sans ces headers, nginx traite la connexion comme HTTP et coupe le WebSocket
    location /hubs/ {
        proxy_pass http://localhost:5000/hubs/;
        proxy_http_version 1.1;                    # WebSocket nécessite HTTP/1.1
        proxy_set_header Upgrade $http_upgrade;    # Header de passage en mode WebSocket
        proxy_set_header Connection "upgrade";     # Indique le changement de protocole
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;                  # 24h — connexion SignalR persistante
    }

    # Swagger (optionnel — à désactiver en prod si API publique)
    location /swagger/ {
        proxy_pass http://localhost:5000/swagger/;
    }

    # Dashboard Hangfire (Admin uniquement, protégé par JWT côté API)
    location /hangfire/ {
        proxy_pass http://localhost:5000/hangfire/;
    }
}
```

---

## PARTIE 11 — Lancer les conteneurs Docker

### Concept
On utilise **deux fichiers docker-compose** :
- `docker-compose.yml` : config de base (commune dev et prod)
- `docker-compose.prod.yml` : surcharges pour la production (ports fermés, etc.)

Docker Compose fusionne les deux fichiers — les valeurs de `prod` écrasent celles de base.

### Commandes exécutées

```bash
# Premier lancement (en tant que deployer)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
# docker compose = outil de gestion multi-conteneurs
# -f docker-compose.yml = fichier de base
# -f docker-compose.prod.yml = surcharges prod (appliquées par-dessus)
# up = créer et démarrer les conteneurs
# -d = detached (en arrière-plan — le terminal reste disponible)
# --build = reconstruire les images Docker avant de démarrer
#   (obligatoire si le code a changé depuis le dernier build)

# Voir l'état des conteneurs
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
# ps = process status = liste les conteneurs avec leur état et ports

# Voir les logs d'un conteneur
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs api --tail=50
# logs = afficher les logs
# api = nom du service (dans docker-compose.yml)
# --tail=50 = seulement les 50 dernières lignes

# Redémarrer un seul service
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart api

# Arrêter tout
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
# down = arrête et supprime les conteneurs (les volumes persistent)
# down -v = arrête ET supprime les volumes (DANGER : perd les données PostgreSQL)
```

### Pourquoi PostgreSQL n'expose plus son port

**Avant** (dans docker-compose.yml original) :
```yaml
postgres:
  ports:
    - "5432:5432"  # 0.0.0.0:5432 = accessible depuis internet
```

**Après** (port supprimé) :
```yaml
postgres:
  # plus de ports: → accessible uniquement en interne Docker
```

Les conteneurs Docker sur le même réseau (`codearena_default`) se voient via leurs noms de service. L'API se connecte à `postgres:5432` — ça passe par le réseau interne Docker, pas par internet. PostgreSQL n'est plus accessible de l'extérieur.

---

## PARTIE 12 — GitHub Actions : CI/CD automatisé

### Concept
**CI/CD** = Continuous Integration / Continuous Deployment

- **CI** (Intégration Continue) : à chaque push, le code est automatiquement compilé et testé. Si ça casse, on le sait immédiatement.
- **CD** (Déploiement Continu) : si les tests passent ET qu'on est sur `main`, le code est automatiquement déployé en production.

### Comment ça fonctionne

```
git push origin main
    ↓
GitHub Actions démarre un runner (machine virtuelle Ubuntu gratuite)
    ↓
Job 1 : build-and-test
  - Installe .NET 10
  - dotnet restore → télécharge les dépendances NuGet
  - dotnet build → compile le backend
  - Installe Node 24
  - npm ci → installe les dépendances npm
  - npm run build → compile Angular en production
    ↓ (si tout passe)
Job 2 : deploy (uniquement sur push main)
  - Se connecte au VPS via SSH (clé privée dans secrets)
  - cd /opt/codearena
  - git pull origin main → récupère le nouveau code
  - docker compose up --build → rebuild et redémarre les conteneurs
  - curl health check → vérifie que l'API répond
```

### Les secrets GitHub (Settings → Secrets and variables → Actions)

| Secret | Valeur | Pourquoi |
|---|---|---|
| `VPS_HOST` | `195.35.3.89` | IP du VPS pour la connexion SSH |
| `VPS_USER` | `deployer` | Utilisateur SSH (pas root — sécurité) |
| `VPS_SSH_KEY` | contenu de `~/.ssh/deploy_key` | Clé privée pour s'authentifier sans mot de passe |
| `VPS_PORT` | `22` | Port SSH standard |

Ces valeurs sont chiffrées par GitHub et injectées dans le workflow au moment de l'exécution. Elles n'apparaissent jamais dans les logs.

### La Deploy Key GitHub

Sur GitHub → Settings → Deploy keys :
- Clé publique (`deploy_key.pub`) ajoutée en lecture seule
- Permet à l'action SSH de faire `git pull` sur un repo privé depuis le VPS

### Le fichier `.github/workflows/deploy.yml` expliqué

```yaml
on:
  push:
    branches: [ main ]      # Déclencher sur push vers main
  pull_request:
    branches: [ main ]      # ET sur les PR vers main (pour le job build seulement)

jobs:
  build-and-test:           # Job 1 : compile et teste
    runs-on: ubuntu-latest  # Machine virtuelle Ubuntu fournie gratuitement par GitHub

  deploy:
    needs: build-and-test   # Ne démarre que si build-and-test réussit
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    # Déploie UNIQUEMENT si c'est un push sur main (pas une PR)
```

---

## PARTIE 13 — Sécurité PostgreSQL (fermer le port 5432)

### Ce qu'on a changé
Dans `docker-compose.yml`, on a supprimé :
```yaml
ports:
  - "5432:5432"
```

### Résultat visible
```
Avant : 0.0.0.0:5432->5432/tcp  (accessible depuis internet)
Après : 5432/tcp                 (interne Docker uniquement)
```

### Pourquoi ça marche quand même
Docker crée un réseau virtuel `codearena_default` entre tous les services du `docker-compose.yml`. Les conteneurs se parlent via les noms de service :

```
codearena-api → postgres:5432  ✅ (réseau interne Docker)
Internet      → 195.35.3.89:5432  ❌ (port fermé)
```

---

## Récapitulatif : architecture finale

```
Internet
   ↓ HTTPS:443 / WSS (WebSocket)
nginx (VPS hôte)
   ├── /api/*      → localhost:5000 → conteneur API (.NET)
   ├── /hubs/*     → localhost:5000 → NotificationHub (SignalR WebSocket)
   ├── /uploads/*  → localhost:5000 → fichiers statiques (avatars)
   ├── /hangfire/* → localhost:5000 → dashboard Hangfire (Admin)
   └── /*          → localhost:4200 → conteneur Frontend (Angular)

Réseau interne Docker :
   API ──────────────────→ postgres:5432 (PostgreSQL — données app + jobs Hangfire)
   API ──────────────────→ redis:6379    (IDistributedCache leaderboard 30s)
   API ──────────────────→ redis:6379    (pub/sub → RedisNotificationRelay → SignalR)
   Worker (Hangfire) ────→ postgres:5432 (déqueue et exécute les jobs)
   Worker (Hangfire) ────→ redis:6379    (publie notifications push)
```

---

## Commandes de maintenance courantes

```bash
# Se connecter au VPS (depuis réseau non-entreprise)
ssh root@195.35.3.89

# Ou via le terminal web Hostinger
# hpanel.hostinger.com → VPS → Terminal

# Voir l'état de tous les conteneurs
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

# Logs en temps réel (tous les services)
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f

# Logs par service
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs api -f
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs hangfire -f

# Redémarrer l'app complète
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart

# Mettre à jour manuellement (sans attendre GitHub Actions)
su - deployer
cd /opt/codearena
git pull origin main
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Sauvegarder la base de données
docker exec codearena-db pg_dump -U codearena_user codearena > backup_$(date +%Y%m%d).sql

# Vérifier que Redis répond
docker exec codearena-redis redis-cli ping
# → PONG

# Vérifier les clés Redis (cache leaderboard)
docker exec codearena-redis redis-cli keys "leaderboard*"

# Accéder au dashboard Hangfire (Admin uniquement)
# → https://codearena.bissaye.online/hangfire  (se connecter en admin d'abord)

# Vérifier les jobs Hangfire en base
docker exec codearena-db psql -U codearena_user -d codearena \
  -c "SELECT key FROM hangfire.hash WHERE key LIKE 'recurring-job:%';"

# Vérifier le certificat SSL
certbot certificates
# Affiche la date d'expiration — renouvellement automatique via cron à 3h du matin

# Vérifier nginx
systemctl status nginx
nginx -t  # tester la config avant reload
systemctl reload nginx
```

---

## Si tu dois tout refaire sur un nouveau VPS

Suis ces étapes dans l'ordre :

```
1.  DNS         → Enregistrement A dans le panel du domaine
2.  Firewall    → Ouvrir ports 22, 80, 443 uniquement
3.  SSH         → Se connecter en root
4.  Docker      → curl -fsSL https://get.docker.com | sh
5.  Utilisateur → useradd deployer + groupe docker + /opt/codearena
6.  Clés SSH    → ssh-keygen deploy_key + authorized_keys
7.  Secrets     → openssl rand pour POSTGRES_PASSWORD et JWT_SECRET
8.  Certbot     → apt install certbot + certbot certonly --standalone
9.  Cron SSL    → crontab renouvellement automatique
10. Clone       → git clone dans /opt/codearena
11. .env        → cp .env.example .env + remplir les valeurs prod
12. nginx       → apt install + config sites-available + désactiver default
13. Docker up   → docker compose -f ... -f ... up -d --build
14. GitHub      → Deploy Key + Secrets Actions + workflow deploy.yml
15. Test        → push un commit → vérifier GitHub Actions → vérifier https://domaine
```
