import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        @if (isLoading) {
          <div class="auth-card__loading">
            <div class="loading-spinner"></div>
            <p>{{ 'auth.verify_email.loading' | translate }}</p>
          </div>
        } @else if (success) {
          <div class="auth-card__success">
            <div class="status-icon">✅</div>
            <h2>{{ 'auth.verify_email.success_title' | translate }}</h2>
            <p>{{ 'auth.verify_email.success_body' | translate }}</p>
            <a [routerLink]="['/login']" class="btn btn--primary" style="margin-top:1.5rem;display:inline-block;">
              {{ 'auth.verify_email.go_to_login' | translate }}
            </a>
          </div>
        } @else {
          <div class="auth-card__error">
            <div class="status-icon">❌</div>
            <h2>{{ 'auth.verify_email.error_title' | translate }}</h2>
            <p>{{ 'auth.verify_email.error_body' | translate }}</p>
            <a [routerLink]="['/login']" class="btn btn--outline" style="margin-top:1.5rem;display:inline-block;">
              {{ 'auth.verify_email.back_to_login' | translate }}
            </a>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .auth-page { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--color-background); padding: var(--space-4); }
    .auth-card { background: var(--color-surface); border-radius: var(--radius-lg); padding: var(--space-8); width: 100%; max-width: 420px; box-shadow: var(--shadow-card); text-align: center; }
    .auth-card__loading, .auth-card__success, .auth-card__error { display: flex; flex-direction: column; align-items: center; gap: var(--space-4); color: var(--color-text-secondary); }
    .status-icon { font-size: 3rem; }
    h2 { font-size: var(--text-title); font-weight: 700; color: var(--color-text); margin: 0; }
    p { margin: 0; }
    .loading-spinner { width: 48px; height: 48px; border: 4px solid var(--color-border); border-top-color: var(--color-primary); border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
})
export class VerifyEmailComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  success = false;

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.isLoading = false;
      this.cdr.markForCheck();
      return;
    }

    this.authService.verifyEmail(token).subscribe({
      next: () => {
        this.isLoading = false;
        this.success = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.success = false;
        this.cdr.markForCheck();
      },
    });
  }
}
