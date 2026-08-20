import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="page-container error-page">
      <div class="error-page__code">403</div>
      <h1 class="error-page__title">{{ 'errors.forbidden.title' | translate }}</h1>
      <p class="error-page__message">{{ 'errors.forbidden.message' | translate }}</p>
      <a routerLink="/" class="btn btn-primary">{{ 'common.backHome' | translate }}</a>
    </div>
  `,
  styles: [`
    .error-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 60vh;
      text-align: center;
      gap: var(--space-4);

      &__code {
        font-family: var(--font-display);
        font-size: 120px;
        font-weight: 900;
        color: var(--color-error);
        line-height: 1;
        opacity: 0.15;
      }

      &__title {
        font-family: var(--font-display);
        font-size: var(--text-2xl);
        color: var(--color-text-primary);
        margin-top: calc(-1 * var(--space-8));
      }

      &__message {
        color: var(--color-text-secondary);
        font-size: var(--text-base);
        max-width: 400px;
      }
    }
  `]
})
export class ForbiddenComponent {}
