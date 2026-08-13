import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DatePipe, LowerCasePipe } from '@angular/common';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { CompetitionService } from '../../core/services/competition.service';
import { UserService } from '../../core/services/user.service';
import { NotificationService } from '../../core/services/notification.service';
import { LeaderboardEntry, LeaderboardPage, LeaderboardFilters } from '../../core/models/leaderboard.models';
import { CompetitionSummary } from '../../core/models/competition.models';
import { COUNTRIES } from '../../core/constants/countries';
import { CAMEROON_REGIONS } from '../../core/models/regions';
import { CountryFlagPipe } from '../../shared/pipes/country-flag.pipe';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DatePipe, LowerCasePipe, ReactiveFormsModule, CountryFlagPipe],
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.scss',
})
export class LeaderboardComponent implements OnInit, OnDestroy {
  private readonly leaderboardService = inject(LeaderboardService);
  private readonly competitionService = inject(CompetitionService);
  private readonly userService = inject(UserService);
  private readonly notifService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  error: string | null = null;
  page: LeaderboardPage | null = null;
  competitions: CompetitionSummary[] = [];
  countries = COUNTRIES;
  readonly cameroonRegions = CAMEROON_REGIONS;
  schoolSuggestions: string[] = [];
  highlightedUsername = '';

  private lbSub?: Subscription;
  private highlightTimeout?: ReturnType<typeof setTimeout>;

  get isLive(): boolean {
    return this.competitions.some(c => c.status === 'Ongoing');
  }

  readonly LIMIT = 50;
  currentOffset = 0;

  filtersForm = this.fb.group({
    country: [''],
    region: [''],
    school: [''],
    competitionId: [''],
    scoreMin: [null as number | null],
    scoreMax: [null as number | null],
    competitionOnly: [false],
    search: [''],
  });

  ngOnInit(): void {
    // Pre-fill from query params (e.g. from competition page "Voir classement complet")
    const params = this.route.snapshot.queryParamMap;
    if (params.get('competitionId')) {
      this.filtersForm.patchValue({ competitionId: params.get('competitionId') });
    }

    this.loadCompetitions();
    this.loadSchools();
    this.loadLeaderboard();

    this.lbSub = this.notifService.leaderboardUpdate$.subscribe(evt => {
      const competitionFilter = this.filtersForm.value.competitionId;
      const relevant = !competitionFilter || competitionFilter === evt.competitionId;
      if (!relevant) return;

      this.highlightedUsername = evt.username;
      clearTimeout(this.highlightTimeout);
      this.highlightTimeout = setTimeout(() => {
        this.highlightedUsername = '';
        this.cdr.markForCheck();
      }, 2000);
      this.loadLeaderboard(this.currentOffset);
    });
  }

  ngOnDestroy(): void {
    this.lbSub?.unsubscribe();
    clearTimeout(this.highlightTimeout);
  }

  loadSchools(): void {
    this.userService.getSchools().subscribe({
      next: (schools) => { this.schoolSuggestions = schools; this.cdr.markForCheck(); },
      error: () => { /* non-bloquant */ },
    });
  }

  loadCompetitions(): void {
    this.competitionService.getAll().subscribe({
      next: (comps) => { this.competitions = comps; this.cdr.markForCheck(); },
      error: () => { /* non-bloquant */ this.cdr.markForCheck(); },
    });
  }

  loadLeaderboard(offset = 0): void {
    this.isLoading = true;
    this.error = null;
    this.currentOffset = offset;

    const filters = this.buildFilters(offset);
    this.leaderboardService.getFiltered(filters).subscribe({
      next: (p) => {
        this.page = p;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'leaderboard.error.generic';
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  applyFilters(): void {
    this.loadLeaderboard(0);
  }

  resetFilters(): void {
    this.filtersForm.reset({ country: '', region: '', school: '', competitionId: '', scoreMin: null, scoreMax: null, competitionOnly: false, search: '' });
    this.loadLeaderboard(0);
  }

  goToPage(offset: number): void {
    if (offset < 0) return;
    if (this.page && offset >= this.page.total) return;
    this.loadLeaderboard(offset);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  get currentPage(): number {
    return Math.floor(this.currentOffset / this.LIMIT) + 1;
  }

  get totalPages(): number {
    if (!this.page) return 1;
    return Math.ceil(this.page.total / this.LIMIT);
  }

  get pages(): number[] {
    const pages: number[] = [];
    const total = this.totalPages;
    const current = this.currentPage;
    const delta = 2;
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      pages.push(i);
    }
    return pages;
  }

  getRankClass(rank: number): string {
    if (rank === 1) return 'rank-gold';
    if (rank === 2) return 'rank-silver';
    if (rank === 3) return 'rank-bronze';
    if (this.page && rank <= Math.ceil(this.page.total * 0.2)) return 'rank-green';
    return 'rank-default';
  }

  getInitials(username: string): string {
    return username.slice(0, 2).toUpperCase();
  }

  private buildFilters(offset: number): LeaderboardFilters {
    const v = this.filtersForm.value;
    return {
      country: v.country || undefined,
      region: v.region || undefined,
      school: v.school || undefined,
      competitionId: v.competitionId || undefined,
      scoreMin: v.scoreMin ?? undefined,
      scoreMax: v.scoreMax ?? undefined,
      competitionOnly: v.competitionOnly ?? false,
      search: v.search || undefined,
      offset,
      limit: this.LIMIT,
    };
  }
}
