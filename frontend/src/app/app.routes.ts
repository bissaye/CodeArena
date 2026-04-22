import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { moderatorGuard } from './core/guards/moderator.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
  },
  {
    path: 'verify-email',
    loadComponent: () => import('./features/auth/verify-email/verify-email.component').then(m => m.VerifyEmailComponent),
  },
  // Competition routes
  {
    path: 'competitions',
    loadComponent: () => import('./features/competition/competitions-list/competitions-list.component').then(m => m.CompetitionsListComponent),
  },
  {
    path: 'competitions/new',
    canActivate: [authGuard, moderatorGuard],
    loadComponent: () => import('./features/admin/competition-form/competition-form.component').then(m => m.CompetitionFormComponent),
  },
  {
    path: 'competitions/:id/edit',
    canActivate: [authGuard, moderatorGuard],
    loadComponent: () => import('./features/admin/competition-form/competition-form.component').then(m => m.CompetitionFormComponent),
  },
  {
    path: 'competitions/:competitionId/problems/new',
    canActivate: [authGuard, moderatorGuard],
    loadComponent: () => import('./features/admin/problem-form/problem-form.component').then(m => m.ProblemFormComponent),
  },
  {
    path: 'competitions/:id',
    loadComponent: () => import('./features/competition/competition-detail/competition-detail.component').then(m => m.CompetitionDetailComponent),
  },
  // Problem routes
  {
    path: 'problems/:id/edit',
    canActivate: [authGuard, moderatorGuard],
    loadComponent: () => import('./features/admin/problem-form/problem-form.component').then(m => m.ProblemFormComponent),
  },
  {
    path: 'problems/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/problem/problem-detail/problem-detail.component').then(m => m.ProblemDetailComponent),
  },
  {
    path: 'notifications',
    canActivate: [authGuard],
    loadComponent: () => import('./features/notifications/notifications.component').then(m => m.NotificationsComponent),
  },
  {
    path: 'leaderboard',
    loadComponent: () => import('./features/leaderboard/leaderboard.component').then(m => m.LeaderboardComponent),
  },
  {
    path: 'u/:username',
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./features/admin/admin.component').then(m => m.AdminComponent),
  },
  {
    path: '404',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent),
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/forbidden/forbidden.component').then(m => m.ForbiddenComponent),
  },
  {
    path: '**',
    redirectTo: '404',
  },
];
