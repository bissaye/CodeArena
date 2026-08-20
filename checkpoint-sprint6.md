# Checkpoint Sprint 6 — Administration & Polish

**Date :** 2026-08-19  
**Statut :** ✅ Terminé + Post-Sprint Corrections & Ajouts (Session 2 — 2026-08-19/20)  
**Sprints complétés :** 0, 1, 2, 3, 4, 5, 6

---

## Résumé

Sprint 6 clôture l'application : interface admin pour gérer les modérateurs, polish complet (toasts, pages d'erreur, intercepteur HTTP, responsive mobile, switch FR/EN) et README de déploiement.

Après la livraison du Sprint 6, deux sessions de corrections et d'ajouts ont été menées :  
- **Post-Sprint 6 (corrections)** : bugs critiques détectés au smoke test (JWT claims, zoneless CD, CSS)  
- **Post-Sprint 6 Session 2** : nouvelles fonctionnalités (page `/competitions`, home adaptative, datalists région/école)

---

## Endpoints livrés (Sprint 6)

| Endpoint | Auth | Code | Description |
|---|---|---|---|
| `GET /api/admin/moderators` | AdminOnly | 200 | Liste des modérateurs |
| `POST /api/admin/moderators` | AdminOnly | 201 / 404 / 409 | Promouvoir un utilisateur en modérateur |
| `DELETE /api/admin/moderators/{userId}` | AdminOnly | 200 / 400 / 404 | Rétrograder un modérateur (interdit de se retirer soi-même) |

---

## Endpoints livrés (Post-Sprint 6 Session 2)

| Endpoint | Auth | Code | Description |
|---|---|---|---|
| `GET /api/users/regions` | Non | 200 | Régions distinctes non-nulles (ordre alphabétique) |
| `GET /api/users/schools` | Non | 200 | Établissements distincts non-nuls (ordre alphabétique) |

Implémentation : `IUserService` / `UserService` → `UsersController` — LINQ `Where + Select + Distinct + OrderBy + ToListAsync`.

---

## Composants / services Frontend livrés (Sprint 6)

| Élément | Localisation | Description |
|---|---|---|
| `ToastService` | `core/services/toast.service.ts` | Service global, 3 types (success/error/info), auto-dismiss |
| `ToastComponent` | `shared/components/toast/` | Composant fixe bas-droite, animation entrée |
| `AdminGuard` | `core/guards/admin.guard.ts` | CanActivateFn — redirige `/` si non Admin |
| `AdminService` | `core/services/admin.service.ts` | HTTP vers /api/admin/moderators |
| `AdminComponent` | `features/admin/` | Liste + form ajout + modal confirmation retrait |
| `ErrorInterceptor` | `core/interceptors/error.interceptor.ts` | 401→login, 403→/forbidden, 500→toast |
| `NotFoundComponent` | `features/not-found/` | Page 404 personnalisée |
| `ForbiddenComponent` | `features/forbidden/` | Page 403 personnalisée |
| Lang switcher `[FR\|EN]` | Header | Via `translate.onLangChange`, `TranslateService.use()` |
| Responsive mobile | `styles.scss` + composants | Header 375px, leaderboard colonnes masquées, problem-title-row |
| `README.md` | Racine | Prérequis, 3 commandes install, déploiement VPS complet |

---

## Composants / services Frontend livrés (Post-Sprint 6 Session 2)

| Élément | Localisation | Description |
|---|---|---|
| `CompetitionsListComponent` | `features/competition/competitions-list/` | Liste paginée (10/page), recherche live, tri Ongoing→Upcoming→Finished, indicateurs visuels |
| Bouton "+ Créer une compétition" | `competitions-list.component.html` | Conditionnel `@if (isModerator)` → `/competitions/new` |
| `UserService.getRegions()` | `core/services/user.service.ts` | Appelle `GET /api/users/regions` |
| `UserService.getSchools()` | `core/services/user.service.ts` | Appelle `GET /api/users/schools` |
| `CAMEROON_REGIONS` | `core/models/regions.ts` | Constante des 10 régions du Cameroun |
| Datalist région/école | `register`, `profile`, `leaderboard` | `<input list> + <datalist>` sur champs région et école |
| Home layout adaptatif | `features/home/` | Hero card (1 en cours), grille 2col (2-3), auto-fill (4+) |
| Lien "Compétitions" header | `shared/components/header/` | Route `/competitions` dans la navbar principale |
| Lien "Voir toutes →" home | `features/home/home.component.html` | Section Terminées → `/competitions` |

---

## Visuels `/competitions`

- **Ongoing** : point jaune clignotant (`@keyframes pulse`) + ligne en gras + badge `badge--live` (fond accent)
- **Upcoming** : point bleu fixe + badge `badge--upcoming` (fond info)
- **Finished** : neutre + badge `badge--finished` (fond gris)
- Classes badge définies dans `competitions-list.component.scss` (pas globales)
- `comp-link { font-weight: inherit }` pour propager le gras de la ligne

---

## Décisions techniques Sprint 6

### User.PromotedAt
- `DateTime?` nullable ajoutée à l'entité User
- Migration `AddUserPromotedAt` appliquée automatiquement au démarrage
- Valeur null pour les modérateurs existants dans le seed → affichage `—` côté frontend

### AdminService — règles de sécurité
- `GET/POST/DELETE /api/admin/moderators` : `[Authorize(Policy = "AdminOnly")]` strict
- `RemoveModeratorAsync` compare `userId == requestingUserId` → `BadRequestException` (400)
- Un Admin ne peut pas être promu/rétrogradé via cette interface (`ConflictException` 409)
- Le rôle Admin reste uniquement attribuable via seed/migration — jamais via l'API

### ErrorInterceptor
- Placé après `jwtInterceptor` dans `withInterceptors([jwtInterceptor, errorInterceptor])`
- 401 → `auth.logout()` + navigate to `/login` (token expiré ou invalide)
- 403 → navigate to `/forbidden` (rôle insuffisant)
- 500+ → `toast.error('ERRORS.SERVER')` (erreur serveur interne)
- Les erreurs 404 et 400 sont laissées aux composants pour un traitement contextuel

### Language switch FR/EN
- `translate.currentLang` est un `Signal<string | null>` dans ngx-translate 18 → ne pas lire directement
- Utiliser `translate.onLangChange` (Observable) pour suivre les changements
- Valeur initiale 'fr' codée en dur (correspond à `lang: 'fr'` dans `provideTranslateService`)

---

## Corrections post-Sprint 6 (bugs critiques)

### Bug 1 : JWT claim name incorrect
- **Cause** : `ClaimTypes.Role` écrit l'URL longue `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` dans le JWT → Angular lisait `payload.role` = `undefined`
- **Fix** : `new Claim("role", user.Role.ToString())` dans `JwtService.cs`
- **Règle** : Ne jamais utiliser `ClaimTypes.*` pour écrire des claims JWT — toujours les noms courts

### Bug 2 : Angular 21 Zoneless Change Detection
- **Cause** : Angular 21 sans `zone.js` → callbacks HTTP ne déclenchaient pas de re-rendu
- **Fix** : `provideZonelessChangeDetection()` dans `app.config.ts` + `ChangeDetectorRef.markForCheck()` dans tous les callbacks async de tous les composants
- **Règle** : Tout changement d'état dans un callback async doit être suivi de `markForCheck()`

### Bug 3 : `.leaderboard-row` global
- **Cause** : Classe définie dans `styles.scss` pour `div` s'appliquait aux `<tr>` du `LeaderboardComponent`
- **Fix** : Renommage des `<tr>` en classe `lb-row` dans `leaderboard.component.html/scss`

### Bug 4 : Mini-leaderboard pseudos tronqués
- **Cause** : Colonne pays (`auto` ≈ 80px) volait l'espace au pseudo dans la sidebar (320px)
- **Fix** : Suppression de la colonne pays, grille passée de 6 à 5 colonnes

### Bug 5 : Variables CSS `--text-*` manquantes
- **Fix** : Ajout dans `:root` de `--text-display`, `--text-title`, `--text-subtitle`, `--text-body`, `--text-label`, `--text-small`

---

## Clés i18n ajoutées (Post-Sprint 6 Session 2)

| Clé | FR | EN |
|---|---|---|
| `NAV.COMPETITIONS` | Compétitions | Competitions |
| `competitions.*` | Namespace complet (titre, recherche, colonnes, pagination, vide, erreur) | ✅ |
| `home.hero_competition.*` | Badge Live + CTA hero card | ✅ |
| `home.section.seeAll` | Voir toutes → | See all → |
| `common.prev_page` | Page précédente | Previous page |
| `common.next_page` | Page suivante | Next page |
| `leaderboard.filter.school_placeholder` | Rechercher un établissement… | Search an institution… |
| `profile.region_placeholder` | Région | Region |
| `profile.school_placeholder` | École / Université | School / University |
| `leaderboard.filter.region_placeholder` | Région | Region |

---

## Smoke tests Sprint 6

| Test | Résultat |
|---|---|
| GET /api/admin/moderators (admin) → 200 | ✅ |
| GET /api/admin/moderators (sans auth) → 401 | ✅ |
| POST /api/admin/moderators (admin → ajouter alice) → 201 | ✅ |
| POST /api/admin/moderators (alice déjà mod) → 409 | ✅ |
| GET /api/admin/moderators (participant) → 403 | ✅ |
| DELETE /api/admin/moderators/{aliceId} → 200 | ✅ |
| DELETE self (admin tente de se retirer) → 400 | ✅ |
| Build Angular production (ng build) | ✅ |
| Build .NET (dotnet build) | ✅ |
| Containers podman-compose up --build | ✅ |

## Smoke tests Post-Sprint 6 Session 2

| Test | Résultat |
|---|---|
| GET /api/users/regions → régions distinctes | ✅ |
| GET /api/users/schools → établissements distincts | ✅ |
| GET /api/competitions → 3 compétitions (sans createdAt) | ✅ |
| Page /competitions — visuel, recherche, pagination | ✅ |
| Home hero card (1 compétition en cours) | ✅ |
| Datalist région dans leaderboard | ✅ |
| Datalist école chargé depuis l'API | ✅ |
| Bouton "+ Créer" visible pour moderateur1, invisible pour alice | ✅ |

---

## État des services au checkpoint

```
podman-compose ps  →  tous UP (codearena-db healthy, codearena-api, codearena-frontend)
API Health         →  {"status":"healthy"}
Frontend           →  HTTP 200 sur toutes les routes
```

---

## Application prête pour démo — toutes les fonctionnalités

- ✅ Auth (inscription / connexion / JWT)
- ✅ Profil utilisateur + avatar + changement mot de passe
- ✅ Datalist région/école sur inscription, profil, filtres classement
- ✅ Page d'accueil adaptative (hero card / grille 2col / auto-fill selon nb compétitions en cours)
- ✅ Page `/competitions` — liste paginée, visuels statut (dot + badge + gras), recherche live
- ✅ Bouton "Créer une compétition" sur `/competitions` (Modérateur/Admin uniquement)
- ✅ Compte à rebours temps réel
- ✅ Exercices avec soumission + jugement automatique
- ✅ Classement global filtré + paginé (7 filtres dont région/école avec datalist)
- ✅ Back-office modérateur (création compétitions/exercices, Markdown, aperçu live)
- ✅ Administration (gestion modérateurs avec modal confirmation)
- ✅ Toast notifications globales (succès/erreur/info)
- ✅ Pages 404/403 personnalisées
- ✅ Intercepteur HTTP global (401/403/500)
- ✅ Switch FR/EN fonctionnel sur toutes les pages
- ✅ Responsive mobile 375px
- ✅ README complet avec déploiement VPS

---

## Backlog restant (non encore implémenté)

- 🔲 Carousel / grille avec scroll horizontal pour 4+ compétitions en cours simultanément
- 🔲 Filtre par année sur la page `/competitions` (archives par saison)
