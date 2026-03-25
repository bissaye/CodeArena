import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordsMismatch: true } : null;
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-card__header">
          <h1 class="auth-card__title">{{ 'auth.reset_password.title' | translate }}</h1>
        </div>

        @if (!token) {
          <div class="alert alert--error">{{ 'auth.reset_password.error_no_token' | translate }}</div>
        } @else if (!success) {
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-card__form">
            <div class="form-group">
              <label class="form-label" for="newPassword">{{ 'auth.reset_password.new_password_label' | translate }}</label>
              <input
                id="newPassword"
                type="password"
                class="form-input"
                formControlName="newPassword"
                autocomplete="new-password"
              />
            </div>
            <div class="form-group">
              <label class="form-label" for="confirmPassword">{{ 'auth.reset_password.confirm_password_label' | translate }}</label>
              <input
                id="confirmPassword"
                type="password"
                class="form-input"
                formControlName="confirmPassword"
                autocomplete="new-password"
              />
              @if (form.hasError('passwordsMismatch') && form.get('confirmPassword')?.touched) {
                <span class="form-error">{{ 'auth.reset_password.error_mismatch' | translate }}</span>
              }
            </div>

            @if (error) {
              <div class="alert alert--error">{{ error | translate }}</div>
            }

            <button type="submit" class="btn btn--primary btn--full" [disabled]="isLoading">
              @if (isLoading) { <span class="spinner"></span> }
              {{ 'auth.reset_password.submit' | translate }}
            </button>
          </form>
        } @else {
          <div class="auth-card__success">
            <div class="success-icon">✅</div>
            <p>{{ 'auth.reset_password.success' | translate }}</p>
            <a [routerLink]="['/login']" class="btn btn--primary" style="margin-top:1rem;display:inline-block;">
              {{ 'auth.reset_password.go_to_login' | translate }}
            </a>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .auth-page { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--color-background); padding: var(--space-4); }
    .auth-card { background: var(--color-surface); border-radius: var(--radius-lg); padding: var(--space-8); width: 100%; max-width: 420px; box-shadow: var(--shadow-card); }
    .auth-card__header { margin-bottom: var(--space-6); text-align: center; }
    .auth-card__title { font-size: var(--text-title); font-weight: 700; color: var(--color-text); margin: 0; }
    .auth-card__form { display: flex; flex-direction: column; gap: var(--space-4); }
    .auth-card__success { text-align: center; padding: var(--space-4) 0; color: var(--color-text-secondary); }
    .success-icon { font-size: 3rem; margin-bottom: var(--space-4); }
    .form-error { color: var(--color-danger); font-size: var(--text-small); margin-top: var(--space-1); display: block; }
    .spinner { display: inline-block; width: 14px; height: 14px; border: 2px solid rgba(255,255,255,0.4); border-top-color: white; border-radius: 50%; animation: spin 0.7s linear infinite; margin-right: var(--space-2); }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = false;
  error: string | null = null;
  success = false;
  token: string | null = null;

  form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  }, { validators: passwordsMatch });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
  }

  onSubmit(): void {
    if (this.form.invalid || this.isLoading || !this.token) return;

    this.isLoading = true;
    this.error = null;

    this.authService.resetPassword(this.token, this.form.getRawValue().newPassword).subscribe({
      next: () => {
        this.isLoading = false;
        this.success = true;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.error = err.status === 400
          ? 'auth.reset_password.error_invalid_token'
          : 'auth.reset_password.error_generic';
        this.cdr.markForCheck();
      },
    });
  }
}
