# CodeArena Cameroun — Guide de démo client

> Application complète — Sprints 0–6 + V2-1 (emails) + V2-2 (notifications) + V2-3 (badges) + Sprint INFRA (Redis/Hangfire/SignalR).  
> Build validé le **2026-08-21** — tous les smoke tests passent.

---

## 1. Lancer l'application (3 commandes)

```bash
git clone <url-du-repo> codearena
cd codearena
cp .env.example .env
podman-compose up --build
```

L'application démarre en ~60 secondes. Attendre que les logs indiquent :

```
codearena-api       | Database migrated and seeded successfully
codearena-frontend  | 2026/08/19 ... "GET / HTTP/1.1" 200
```

---

## 2. URLs clés

| Service | URL | Description |
|---|---|---|
| **Application** | http://localhost:4200 | Interface Angular (prod build) |
| **API Swagger** | http://localhost:5000/swagger | Documentation interactive |
| **API Health** | http://localhost:5000/api/health | `{"status":"healthy"}` |
| **Hangfire Dashboard** | http://localhost:5000/hangfire | Jobs (connecté en Admin) |
| **Hangfire Worker** | http://localhost:5001/hangfire | Worker interne (sans auth) |
| **PgAdmin** | http://localhost:5050 | `podman-compose --profile tools up` |

---

## 3. Comptes de démo

| Compte | Mot de passe | Rôle | Usage démo |
|---|---|---|---|
| `admin` | `Admin123!` | Admin | Gérer les modérateurs, voir le lien Administration |
| `moderateur1` | `Test123!` | Modérateur | Créer des compétitions et exercices |
| `alice_yaounde` | `Test123!` | Participant | Soumettre des solutions, voir le classement |
| `bob_douala` | `Test123!` | Participant | Compte alternatif |

---

## 4. Scénario de démo guidé (5 minutes)

### Étape 1 — Accueil & Navigation (45s)

1. Ouvrir http://localhost:4200
2. Montrer la **hero card** de la compétition en cours (pleine largeur, compte à rebours, bouton CTA)
3. Montrer la section À venir et Terminées avec le lien **Voir toutes →**
4. Montrer le mini-classement dans la sidebar
5. Basculer **FR → EN** avec le switch dans le header → toute l'interface change de langue
6. Cliquer **Compétitions** dans la navbar → page `/competitions` :
   - Point jaune clignotant + ligne en gras pour les compétitions **En cours**
   - Point bleu pour les compétitions **À venir**
   - Recherche live par nom, pagination 10/page
7. Naviguer vers http://localhost:4200/leaderboard → classement global filtré par pays/région/école
   - Filtre région : datalist avec les 10 régions du Cameroun
   - Filtre école : datalist avec les établissements enregistrés en base

---

### Étape 2 — Connexion Admin & Administration (60s)

1. Cliquer **Se connecter** → saisir `admin` / `Admin123!`
2. Le header affiche **admin** avec le menu déroulant
3. Cliquer sur le pseudo `admin` → lien **Administration** visible (invisible pour un participant)
4. Aller sur `/admin` → interface de gestion des modérateurs
5. Ajouter `bob_douala` comme modérateur → toast vert de confirmation
6. Le supprimer → modal de confirmation → toast info

---

### Étape 3 — Création de contenu (Modérateur) (90s)

1. Se déconnecter → se connecter en `moderateur1` / `Test123!`
2. Aller sur la compétition **CodeArena Open 2026** (Terminée) → bouton **Modifier la compétition**
3. Montrer le formulaire d'édition : titre, dates, description Markdown
4. Aller sur un exercice → bouton **Modifier l'exercice**
5. Montrer l'aperçu Markdown live (panneau droit se met à jour en temps réel)
6. Naviguer vers **Compétitions** (navbar) → bouton **+ Créer une compétition** visible en haut à droite (invisible pour un participant)
7. Créer une nouvelle compétition → remplir le formulaire → publier
8. Ajouter un exercice avec upload de `input.txt` et `output.txt`

---

### Étape 4 — Soumission Participant + Notifications temps réel (90s)

1. Se déconnecter → se connecter en `alice_yaounde` / `Test123!`
2. Observer la **cloche de notification** dans le header (icône en haut à droite)
3. Aller sur la compétition **CodeArena Challenge Sprint 3 — En cours** (statut Live)
4. Cliquer sur l'exercice **Fibonacci** → lire l'énoncé Markdown rendu
5. Cliquer **Télécharger l'entrée** → récupérer `input.txt`
6. Cliquer **Soumettre** → uploader le fichier résultat `.txt` + fichier source
7. Résultat **Accepted ✓ — 200 points ajoutés** affiché immédiatement
8. **Sans recharger la page** — la cloche se met à jour via SignalR (badge rouge +1)
9. Si premier Accepted : toast **"Badge débloqué : Premier pas"** apparaît
10. Cliquer sur la cloche → dropdown des 5 dernières notifications
11. Naviguer vers `/notifications` → liste complète paginée
12. Naviguer vers le classement → `alice_yaounde` monte dans le classement

---

### Étape 5 — Profil, Badges & Niveaux (45s)

1. Cliquer sur le pseudo `alice_yaounde` → page profil publique
2. Section **Niveau** : indicateur coloré (Débutant/Intermédiaire/Avancé/Expert) + barre de progression
3. Section **Badges** : grille des badges obtenus (icône + date d'obtention)
4. Les badges non obtenus apparaissent grisés avec leur condition

---

### Étape 6 — Profil & Erreurs (30s)

1. Modifier le profil → champ **Région** : datalist avec les 10 régions du Cameroun (saisie libre aussi possible)
3. Champ **École** : datalist avec les établissements déjà enregistrés en base
4. **Enregistrer** → confirmation succès
5. Uploader un avatar (JPG/PNG < 2 Mo) → redimensionné 200×200 automatiquement
6. Naviguer vers http://localhost:4200/url-inexistante → **page 404** personnalisée
7. Tester http://localhost:4200/admin (en tant que participant) → **page 403** personnalisée

---

## 5. Récapitulatif des fonctionnalités

| Fonctionnalité | Sprint | Statut |
|---|---|---|
| Inscription / Connexion JWT | S1 | ✅ |
| Profil utilisateur + avatar (resize 200×200) | S4 | ✅ |
| Changement de mot de passe | S4 | ✅ |
| Page d'accueil — layout adaptatif (hero card / 2col / grille) | S2 | ✅ |
| Compte à rebours temps réel | S2 | ✅ |
| Page `/competitions` — liste paginée, visuels statut, recherche live | S2 | ✅ |
| Bouton "Créer une compétition" sur `/competitions` (Modérateur/Admin) | S5 | ✅ |
| Classement global filtré + paginé (7 filtres) | S4 | ✅ |
| Datalist région/école (inscription, profil, filtres classement) | S4 | ✅ |
| Exercices avec rendu Markdown | S3 | ✅ |
| Soumission + jugement automatique (comparaison fichiers) | S3 | ✅ |
| Score transactionnel (pas de double soumission) | S3 | ✅ |
| Back-office Modérateur (créer/modifier compétitions & exercices) | S5 | ✅ |
| Sanitisation Markdown XSS (Markdig côté serveur) | S5 | ✅ |
| Administration (gestion modérateurs) | S6 | ✅ |
| Toast notifications globales | S6 | ✅ |
| Pages 404 / 403 personnalisées | S6 | ✅ |
| Intercepteur HTTP (401→login, 403→/forbidden, 500→toast) | S6 | ✅ |
| Switch FR / EN complet | S6 | ✅ |
| Responsive mobile 375px | S6 | ✅ |
| Emails transactionnels (vérification, reset mdp) via Brevo SMTP | V2-1 | ✅ |
| Récupération de compte (forgot/reset password) | V2-1 | ✅ |
| Notifications in-app — cloche header, dropdown, page `/notifications` | V2-2 | ✅ |
| Notifications push temps réel via SignalR (sans polling) | INFRA | ✅ |
| 7 badges débloquables (first-ac, speed-solver, week-streak, top-10…) | V2-3 | ✅ |
| Niveaux joueur (Débutant/Intermédiaire/Avancé/Expert sur TotalScore) | V2-3 | ✅ |
| Toast "Badge débloqué" animé après Accepted | V2-3 | ✅ |
| Cache leaderboard Redis (IDistributedCache, 30s TTL) | INFRA | ✅ |
| Jobs Hangfire persistants avec retry (emails, badges, notifications) | INFRA | ✅ |

---

## 6. Données seed disponibles au démarrage

- **3 compétitions** : 1 Upcoming (Test Publié), 1 Ongoing (CodeArena Challenge Sprint 3), 1 Finished (CodeArena Open 2026)
- **4 exercices** répartis sur ces compétitions, avec fichiers input/output sur disque
- **6 utilisateurs** : 1 Admin, 1 Modérateur, 4 Participants (Cameroun, régions différentes)
- **7 badges** seedés avec IDs fixes (first-ac, speed-solver, week-streak, top-10, top-3-national, centurion, mentor)

---

## 7. Résultats des smoke tests build final (2026-08-21)

| Test | Résultat |
|---|---|
| Redis PING → PONG | ✅ |
| `GET /api/health` → `{"status":"healthy"}` | ✅ |
| `GET /api/leaderboard` → HTTP 200 | ✅ |
| Redis keys `leaderboard*` présentes en cache | ✅ |
| Hangfire Worker → `Server codearena-worker` annoncé | ✅ |
| Hangfire dashboard `:5001/hangfire` → HTTP 200 | ✅ |
| Job récurrent `competition-status-update` présent en DB Hangfire | ✅ |
| SignalR hub `/hubs/notifications` → 401 (endpoint protégé) | ✅ |
| `POST /api/auth/login` admin → JWT role Admin | ✅ |
| `GET /api/admin/moderators` avec token admin → 200 | ✅ |
| `GET /api/competitions` → 3 compétitions | ✅ |
| `GET /api/leaderboard/mini` → classement 5 entrées | ✅ |
| `POST /api/problems/{id}/submit` réponse correcte → Accepted + 100 pts | ✅ |
| `POST /api/problems/{id}/submit` double soumission → 409 Conflict | ✅ |
| Frontend http://localhost:4200 → HTTP 200 | ✅ |
| Swagger http://localhost:5000/swagger → HTTP 200 | ✅ |
