import { Component, OnInit, inject, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { TranslatePipe, TranslateService, LangChangeEvent } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, AsyncPipe, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly currentUser$ = this.auth.currentUser$;
  dropdownOpen = false;
  currentLang = 'fr';

  ngOnInit(): void {
    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.currentLang = event.lang;
    });
  }

  switchLang(lang: 'fr' | 'en'): void {
    this.translate.use(lang);
  }

  toggleDropdown(): void {
    this.dropdownOpen = !this.dropdownOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.header__user')) {
      this.dropdownOpen = false;
    }
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  getInitials(username: string): string {
    return username.slice(0, 2).toUpperCase();
  }
}
