import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-card__header">
          <h1 class="auth-card__title">{{ 'auth.forgot_password.title' | translate }}</h1>
          <p class="auth-card__subtitle">{{ 'auth.forgot_password.subtitle' | translate }}</p>
        </div>

        @if (!success) {
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-card__form">
            <div class="form-group">
              <label class="form-label" for="email">{{ 'auth.forgot_password.email_label' | translate }}</label>
              <input
                id="email"
                type="email"
                class="form-input"
                formControlName="email"
                [placeholder]="'auth.forgot_password.email_placeholder' | translate"
                autocomplete="email"
              />
            </div>

            @if (error) {
              <div class="alert alert--error">{{ error | translate }}</div>
            }

            <button type="submit" class="btn btn--primary btn--full" [disabled]="isLoading">
              @if (isLoading) { <span class="spinner"></span> }
              {{ 'auth.forgot_password.submit' | translate }}
            </button>
          </form>
        } @else {
          <div class="auth-card__success">
            <div class="success-icon">✉️</div>
            <p>{{ 'auth.forgot_password.success' | translate }}</p>
          </div>
        }

        <div class="auth-card__footer">
          <a [routerLink]="['/login']">{{ 'auth.forgot_password.back_to_login' | translate }}</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--color-background);
      padding: var(--space-4);
    }
    .auth-card {
      background: var(--color-surface);
      border-radius: var(--radius-lg);
      padding: var(--space-8);
      width: 100%;
      max-width: 420px;
      box-shadow: var(--shadow-card);
    }
    .auth-card__header { margin-bottom: var(--space-6); text-align: center; }
    .auth-card__title { font-size: var(--text-title); font-weight: 700; color: var(--color-text); margin: 0 0 var(--space-2); }
    .auth-card__subtitle { color: var(--color-text-secondary); font-size: var(--text-label); margin: 0; }
    .auth-card__form { display: flex; flex-direction: column; gap: var(--space-4); }
    .auth-card__footer { margin-top: var(--space-6); text-align: center; font-size: var(--text-label); color: var(--color-text-secondary); }
    .auth-card__footer a { color: var(--color-primary); text-decoration: none; }
    .auth-card__success { text-align: center; padding: var(--space-4) 0; color: var(--color-text-secondary); }
    .success-icon { font-size: 3rem; margin-bottom: var(--space-4); }
    .spinner { display: inline-block; width: 14px; height: 14px; border: 2px solid rgba(255,255,255,0.4); border-top-color: white; border-radius: 50%; animation: spin 0.7s linear infinite; margin-right: var(--space-2); }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = false;
  error: string | null = null;
  success = false;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  onSubmit(): void {
    if (this.form.invalid || this.isLoading) return;

    this.isLoading = true;
    this.error = null;

    this.authService.forgotPassword(this.form.getRawValue().email).subscribe({
      next: () => {
        this.isLoading = false;
        this.success = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'auth.forgot_password.error_generic';
        this.cdr.markForCheck();
      },
    });
  }
}
