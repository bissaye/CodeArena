import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { ProblemService } from '../../../core/services/problem.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { BadgeService } from '../../../core/services/badge.service';
import { ProblemDetail, SubmitSolutionResult, SubmissionRecord } from '../../../core/models/problem.models';
import { MarkdownPipe } from '../../../shared/pipes/markdown.pipe';
import { BadgeDto } from '../../../core/models/badge.models';

@Component({
  selector: 'app-problem-detail',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DatePipe, MarkdownPipe],
  templateUrl: './problem-detail.component.html',
  styleUrl: './problem-detail.component.scss',
})
export class ProblemDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly problemService = inject(ProblemService);
  private readonly auth = inject(AuthService);
  private readonly notifService = inject(NotificationService);
  private readonly badgeService = inject(BadgeService);
  private readonly cdr = inject(ChangeDetectorRef);

  // Badge toast
  badgeToasts: BadgeDto[] = [];
  private knownBadgeIds = new Set<string>();
  private badgeSub?: Subscription;

  // Page state
  isLoading = true;
  error: string | null = null;
  problem: ProblemDetail | null = null;

  // Submission state
  resultFile: File | null = null;
  sourceFile: File | null = null;
  isSubmitting = false;
  submitError: string | null = null;
  submitResult: SubmitSolutionResult | null = null;

  // History state
  submissions: SubmissionRecord[] = [];
  historyLoading = true;

  private problemId = '';

  ngOnInit(): void {
    this.problemId = this.route.snapshot.paramMap.get('id')!;
    this.loadProblem();
    this.loadHistory();
    this.preloadUserBadges();
    this.badgeSub = this.notifService.badgeEarned$.subscribe(() => this.checkNewBadges());
  }

  ngOnDestroy(): void {
    this.badgeSub?.unsubscribe();
  }

  private loadProblem(): void {
    this.problemService.getById(this.problemId).subscribe({
      next: (p) => {
        this.problem = p;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.error = err.status === 404 ? 'problem.error.notFound' : 'problem.error.load';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private loadHistory(): void {
    this.problemService.getMySubmissions(this.problemId).subscribe({
      next: (list) => {
        this.submissions = list;
        this.historyLoading = false;
        this.cdr.markForCheck();
      },
      error: () => { this.historyLoading = false; this.cdr.markForCheck(); }
    });
  }

  get isModerator(): boolean {
    return this.auth.hasRole('Moderator');
  }

  get canSubmit(): boolean {
    return this.problem?.competitionStatus === 'Ongoing'
      && this.problem?.solvedByCurrentUser !== true
      && !this.isSubmitting
      && this.submitResult?.status !== 'Accepted';
  }

  get isFormDisabled(): boolean {
    return !this.canSubmit
      || this.isSubmitting
      || this.submitResult?.status === 'Accepted'
      || this.problem?.solvedByCurrentUser === true;
  }

  onResultFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.resultFile = input.files?.[0] ?? null;
  }

  onSourceFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.sourceFile = input.files?.[0] ?? null;
  }

  onSubmit(): void {
    if (!this.resultFile || this.isSubmitting) return;

    this.isSubmitting = true;
    this.submitError = null;

    this.problemService.submit(this.problemId, this.resultFile, this.sourceFile).subscribe({
      next: (result) => {
        this.submitResult = result;
        this.isSubmitting = false;
        if (result.status === 'Accepted' && this.problem) {
          this.problem = { ...this.problem, solvedByCurrentUser: true };
        }
        this.loadHistory();
        this.notifService.triggerRefresh();
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting = false;
        if (err.status === 409) {
          this.submitError = 'problem.submit.errorAlreadySolved';
        } else if (err.status === 400) {
          this.submitError = err.error?.message ?? 'problem.submit.errorInvalid';
        } else {
          this.submitError = 'problem.submit.errorGeneric';
        }
        this.cdr.markForCheck();
      }
    });
  }

  getInputUrl(): string {
    return this.problemService.getInputUrl(this.problemId);
  }

  dismissBadgeToast(index: number): void {
    this.badgeToasts.splice(index, 1);
    this.cdr.markForCheck();
  }

  private preloadUserBadges(): void {
    const username = this.auth.currentUser?.username;
    if (!username) return;
    this.badgeService.getUserBadges(username).subscribe({
      next: (badges) => {
        this.knownBadgeIds = new Set(badges.map(b => b.id));
      },
      error: () => {}
    });
  }

  private checkNewBadges(): void {
    const username = this.auth.currentUser?.username;
    if (!username) return;
    this.badgeService.getUserBadges(username).subscribe({
      next: (badges) => {
        const newBadges = badges.filter(b => !this.knownBadgeIds.has(b.id));
        newBadges.forEach(b => this.knownBadgeIds.add(b.id));
        if (newBadges.length > 0) {
          this.badgeToasts.push(...newBadges);
          this.cdr.markForCheck();
          setTimeout(() => {
            this.badgeToasts = this.badgeToasts.filter(t => !newBadges.includes(t));
            this.cdr.markForCheck();
          }, 6000);
        }
      },
      error: () => {}
    });
  }
}
