import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { CountdownTimerComponent } from '../countdown-timer/countdown-timer.component';
import { CompetitionSummary } from '../../../core/models/competition.models';

@Component({
  selector: 'app-competition-card',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe, CountdownTimerComponent],
  templateUrl: './competition-card.component.html',
  styleUrl: './competition-card.component.scss',
})
export class CompetitionCardComponent {
  @Input({ required: true }) competition!: CompetitionSummary;

  get isLive(): boolean {
    return this.competition.status === 'Ongoing';
  }

  get isUpcoming(): boolean {
    return this.competition.status === 'Upcoming';
  }

  get isFinished(): boolean {
    return this.competition.status === 'Finished';
  }

  formatDuration(): string {
    const start = new Date(this.competition.startDate).getTime();
    const end = new Date(this.competition.endDate).getTime();
    const diffMs = end - start;
    const hours = Math.floor(diffMs / 3600000);
    const minutes = Math.floor((diffMs % 3600000) / 60000);
    if (hours === 0) return `${minutes}min`;
    if (minutes === 0) return `${hours}h`;
    return `${hours}h ${minutes}min`;
  }
}
