import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { CompetitionService } from '../../core/services/competition.service';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { NotificationService } from '../../core/services/notification.service';
import { CompetitionSummary } from '../../core/models/competition.models';
import { LeaderboardEntry } from '../../core/models/leaderboard.models';
import { CompetitionCardComponent } from '../../shared/components/competition-card/competition-card.component';
import { LeaderboardMiniComponent } from '../../shared/components/leaderboard-mini/leaderboard-mini.component';
import { CountdownTimerComponent } from '../../shared/components/countdown-timer/countdown-timer.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, TranslatePipe, CompetitionCardComponent, LeaderboardMiniComponent, CountdownTimerComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly competitionService = inject(CompetitionService);
  private readonly leaderboardService = inject(LeaderboardService);
  private readonly notifService = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  error: string | null = null;

  ongoing: CompetitionSummary[] = [];
  upcoming: CompetitionSummary[] = [];
  finished: CompetitionSummary[] = [];

  leaderboardEntries: LeaderboardEntry[] = [];
  leaderboardLoading = true;
  highlightedUsername = '';

  private lbSub?: Subscription;
  private highlightTimeout?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.competitionService.getAll().subscribe({
      next: (competitions) => {
        this.ongoing = competitions.filter(c => c.status === 'Ongoing');
        this.upcoming = competitions.filter(c => c.status === 'Upcoming');
        this.finished = competitions.filter(c => c.status === 'Finished').slice(0, 5);
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'home.error.loadCompetitions';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });

    this.leaderboardService.getMini(20).subscribe({
      next: (entries) => {
        this.leaderboardEntries = entries;
        this.leaderboardLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.leaderboardLoading = false;
        this.cdr.markForCheck();
      }
    });

    this.lbSub = this.notifService.leaderboardUpdate$.subscribe(evt => {
      this.highlightedUsername = evt.username;
      clearTimeout(this.highlightTimeout);
      this.highlightTimeout = setTimeout(() => {
        this.highlightedUsername = '';
        this.cdr.markForCheck();
      }, 2000);
      this.leaderboardService.getMini(20).subscribe({
        next: (entries) => {
          this.leaderboardEntries = entries;
          this.cdr.markForCheck();
        },
        error: () => {}
      });
    });
  }

  ngOnDestroy(): void {
    this.lbSub?.unsubscribe();
    clearTimeout(this.highlightTimeout);
  }
}
