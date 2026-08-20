import { Component, Input, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';

interface TimeLeft {
  days: number;
  hours: number;
  minutes: number;
  seconds: number;
  expired: boolean;
}

@Component({
  selector: 'app-countdown-timer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (timeLeft.expired) {
      <span class="countdown countdown--expired">Terminé</span>
    } @else {
      <span class="countdown">
        @if (timeLeft.days > 0) {
          <span class="countdown__segment">{{ timeLeft.days }}j</span>
        }
        <span class="countdown__segment">{{ pad(timeLeft.hours) }}h</span>
        <span class="countdown__segment">{{ pad(timeLeft.minutes) }}m</span>
        <span class="countdown__segment">{{ pad(timeLeft.seconds) }}s</span>
      </span>
    }
  `,
  styles: [`
    .countdown {
      font-family: var(--font-mono);
      font-size: var(--text-title);
      color: var(--color-accent);
      display: inline-flex;
      gap: var(--space-1);
    }
    .countdown--expired {
      color: var(--color-text-muted);
      font-size: var(--text-label);
    }
    .countdown__segment {
      min-width: 2ch;
    }
  `]
})
export class CountdownTimerComponent implements OnInit, OnDestroy {
  @Input({ required: true }) endsAt!: string;

  private readonly cdr = inject(ChangeDetectorRef);
  private intervalId?: ReturnType<typeof setInterval>;

  timeLeft: TimeLeft = { days: 0, hours: 0, minutes: 0, seconds: 0, expired: false };

  ngOnInit(): void {
    this.tick();
    this.intervalId = setInterval(() => this.tick(), 1000);
  }

  ngOnDestroy(): void {
    if (this.intervalId) clearInterval(this.intervalId);
  }

  pad(n: number): string {
    return n.toString().padStart(2, '0');
  }

  private tick(): void {
    const diff = new Date(this.endsAt).getTime() - Date.now();
    if (diff <= 0) {
      this.timeLeft = { days: 0, hours: 0, minutes: 0, seconds: 0, expired: true };
    } else {
      const totalSeconds = Math.floor(diff / 1000);
      this.timeLeft = {
        days: Math.floor(totalSeconds / 86400),
        hours: Math.floor((totalSeconds % 86400) / 3600),
        minutes: Math.floor((totalSeconds % 3600) / 60),
        seconds: totalSeconds % 60,
        expired: false,
      };
    }
    this.cdr.markForCheck();
  }
}
