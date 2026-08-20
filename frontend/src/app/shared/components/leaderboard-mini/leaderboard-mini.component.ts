import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LeaderboardEntry } from '../../../core/models/leaderboard.models';

@Component({
  selector: 'app-leaderboard-mini',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './leaderboard-mini.component.html',
  styleUrl: './leaderboard-mini.component.scss',
})
export class LeaderboardMiniComponent {
  @Input({ required: true }) entries!: LeaderboardEntry[];
  @Input() title = '';

  getRankClass(rank: number): string {
    if (rank === 1) return 'rank-gold';
    if (rank === 2) return 'rank-silver';
    if (rank === 3) return 'rank-bronze';
    if (rank <= Math.ceil(this.entries.length * 0.2)) return 'rank-green';
    return 'rank-default';
  }

  getInitials(username: string): string {
    return username.slice(0, 2).toUpperCase();
  }
}
