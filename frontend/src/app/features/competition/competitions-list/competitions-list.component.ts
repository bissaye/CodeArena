import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { CompetitionService } from '../../../core/services/competition.service';
import { AuthService } from '../../../core/services/auth.service';
import { CompetitionSummary } from '../../../core/models/competition.models';

const STATUS_ORDER: Record<string, number> = {
  Ongoing: 0,
  Upcoming: 1,
  Finished: 2,
  Draft: 3,
};

@Component({
  selector: 'app-competitions-list',
  standalone: true,
  imports: [RouterLink, DatePipe, ReactiveFormsModule, TranslatePipe],
  templateUrl: './competitions-list.component.html',
  styleUrl: './competitions-list.component.scss',
})
export class CompetitionsListComponent implements OnInit {
  private readonly competitionService = inject(CompetitionService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  get isModerator(): boolean {
    return this.authService.hasRole('Moderator');
  }

  isLoading = true;
  error: string | null = null;

  private allCompetitions: CompetitionSummary[] = [];
  filteredCompetitions: CompetitionSummary[] = [];
  pagedCompetitions: CompetitionSummary[] = [];

  searchControl = new FormControl('');

  readonly PAGE_SIZE = 10;
  currentPage = 1;
  totalPages = 1;

  get pageStart(): number {
    return (this.currentPage - 1) * this.PAGE_SIZE + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.PAGE_SIZE, this.filteredCompetitions.length);
  }

  ngOnInit(): void {
    this.competitionService.getAll().subscribe({
      next: (competitions) => {
        this.allCompetitions = this.sortCompetitions(competitions);
        this.applyFilter();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'competitions.error.load';
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });

    this.searchControl.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.applyFilter();
      this.cdr.markForCheck();
    });
  }

  private sortCompetitions(competitions: CompetitionSummary[]): CompetitionSummary[] {
    return [...competitions].sort((a, b) => {
      const orderDiff = (STATUS_ORDER[a.status] ?? 99) - (STATUS_ORDER[b.status] ?? 99);
      if (orderDiff !== 0) return orderDiff;
      // Finished : tri par endDate décroissant (plus récent en premier)
      if (a.status === 'Finished') {
        return new Date(b.endDate).getTime() - new Date(a.endDate).getTime();
      }
      return 0;
    });
  }

  private applyFilter(): void {
    const search = (this.searchControl.value ?? '').toLowerCase().trim();
    this.filteredCompetitions = search
      ? this.allCompetitions.filter((c) => c.name.toLowerCase().includes(search))
      : [...this.allCompetitions];

    this.totalPages = Math.max(1, Math.ceil(this.filteredCompetitions.length / this.PAGE_SIZE));
    this.currentPage = Math.min(this.currentPage, this.totalPages);
    this.updatePage();
  }

  private updatePage(): void {
    const start = (this.currentPage - 1) * this.PAGE_SIZE;
    this.pagedCompetitions = this.filteredCompetitions.slice(start, start + this.PAGE_SIZE);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.updatePage();
    this.cdr.markForCheck();
  }

  getPages(): (number | '...')[] {
    const pages: (number | '...')[] = [];
    const total = this.totalPages;
    const current = this.currentPage;

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push('...');
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push('...');
      pages.push(total);
    }
    return pages;
  }

  getBadgeClass(status: CompetitionSummary['status']): string {
    switch (status) {
      case 'Ongoing':  return 'badge badge--live';
      case 'Upcoming': return 'badge badge--upcoming';
      case 'Finished': return 'badge badge--finished';
      default:         return 'badge';
    }
  }

  getStatusKey(status: CompetitionSummary['status']): string {
    switch (status) {
      case 'Ongoing':  return 'competition.status.ongoing';
      case 'Upcoming': return 'competition.status.upcoming';
      case 'Finished': return 'competition.status.finished';
      default:         return 'competition.status.draft';
    }
  }
}
