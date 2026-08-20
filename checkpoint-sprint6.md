# Checkpoint Sprint 6 — Administration & Polish

**Date :** 2026-08-19  
**Statut :** ✅ Terminé  
**Sprints complétés :** 0, 1, 2, 3, 4, 5, 6

---

## Résumé

Sprint 6 clôture l'application : interface admin pour gérer les modérateurs, polish complet (toasts, pages d'erreur, intercepteur HTTP, responsive mobile, switch FR/EN) et README de déploiement.

---

## Endpoints livrés

| Endpoint | Auth | Code | Description |
|---|---|---|---|
| `GET /api/admin/moderators` | AdminOnly | 200 | Liste des modérateurs |
| `POST /api/admin/moderators` | AdminOnly | 201 / 404 / 409 | Promouvoir un utilisateur en modérateur |
| `DELETE /api/admin/moderators/{userId}` | AdminOnly | 200 / 400 / 404 | Rétrograder un modérateur (interdit de se retirer soi-même) |

---

## Composants / services Frontend livrés

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
| Lang switcher `[FR|EN]` | Header | Via `translate.onLangChange`, `TranslateService.use()` |
| Responsive mobile | `styles.scss` + composants | Header 375px, leaderboard colonnes masquées, problem-title-row |
| `README.md` | Racine | Prérequis, 3 commandes install, déploiement VPS complet |

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

---

## État des services au checkpoint

```
podman-compose ps  →  tous UP
API Health         →  {"status":"healthy"}
Frontend           →  HTTP 200 sur toutes les routes
```

---

## Application prête pour démo — toutes les fonctionnalités

- ✅ Auth (inscription / connexion / JWT)
- ✅ Profil utilisateur + avatar + changement mot de passe  
- ✅ Compétitions (Live / À venir / Terminées) + compte à rebours
- ✅ Exercices avec soumission + jugement automatique
- ✅ Classement global filtré + paginé
- ✅ Back-office modérateur (création compétitions/exercices, Markdown)
- ✅ Administration (gestion modérateurs avec modal confirmation)
- ✅ Toast notifications globales (succès/erreur/info)
- ✅ Pages 404/403 personnalisées
- ✅ Intercepteur HTTP global (401/403/500)
- ✅ Switch FR/EN fonctionnel sur toutes les pages
- ✅ Responsive mobile 375px
- ✅ README complet avec déploiement VPS
