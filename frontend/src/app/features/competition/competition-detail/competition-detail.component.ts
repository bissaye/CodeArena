import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CompetitionService } from '../../../core/services/competition.service';
import { CompetitionDetail, ProblemSummary } from '../../../core/models/competition.models';
import { LeaderboardEntry } from '../../../core/models/leaderboard.models';
import { AuthService } from '../../../core/services/auth.service';
import { CountdownTimerComponent } from '../../../shared/components/countdown-timer/countdown-timer.component';
import { LeaderboardMiniComponent } from '../../../shared/components/leaderboard-mini/leaderboard-mini.component';

@Component({
  selector: 'app-competition-detail',
  standalone: true,
  imports: [RouterLink, TranslatePipe, CountdownTimerComponent, LeaderboardMiniComponent],
  templateUrl: './competition-detail.component.html',
  styleUrl: './competition-detail.component.scss',
})
export class CompetitionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly competitionService = inject(CompetitionService);
  readonly auth = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  error: string | null = null;

  competition: CompetitionDetail | null = null;
  leaderboard: LeaderboardEntry[] = [];
  leaderboardLoading = true;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;

    this.competitionService.getById(id).subscribe({
      next: (comp) => {
        this.competition = comp;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'competition.error.load';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });

    this.competitionService.getLeaderboard(id, 10).subscribe({
      next: (entries) => {
        this.leaderboard = entries;
        this.leaderboardLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.leaderboardLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  get isOngoing(): boolean {
    return this.competition?.status === 'Ongoing';
  }

  get isFinished(): boolean {
    return this.competition?.status === 'Finished';
  }

  get isModerator(): boolean {
    return this.auth.hasRole('Moderator');
  }

  acceptanceRate(p: ProblemSummary): number {
    if (p.totalSubmissions === 0) return 0;
    return Math.round((p.acceptedSubmissions / p.totalSubmissions) * 100);
  }
}
