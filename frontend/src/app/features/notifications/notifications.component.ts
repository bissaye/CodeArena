import {
  Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, inject
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationDto, NotificationsPage } from '../../core/models/notification.models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, TranslatePipe],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent implements OnInit {
  private readonly notifService = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = false;
  isMarkingAll = false;
  error: string | null = null;
  page: NotificationsPage | null = null;
  currentPage = 1;
  unreadOnly = false;

  get notifications(): NotificationDto[] {
    return this.page?.items ?? [];
  }

  get totalPages(): number {
    return this.page?.totalPages ?? 1;
  }

  get unreadCount(): number {
    return this.page?.unreadCount ?? 0;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.error = null;
    this.notifService.getNotifications(this.unreadOnly, this.currentPage).subscribe({
      next: (data) => {
        this.page = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'notifications.error_load';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  toggleUnreadOnly(): void {
    this.unreadOnly = !this.unreadOnly;
    this.currentPage = 1;
    this.load();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.load();
  }

  markAsRead(notif: NotificationDto): void {
    if (notif.isRead) return;
    notif.isRead = true;
    notif.readAt = new Date().toISOString();
    if (this.page) this.page.unreadCount = Math.max(0, this.page.unreadCount - 1);
    this.cdr.markForCheck();
    this.notifService.markAsRead(notif.id).subscribe();
  }

  markAllAsRead(): void {
    if (this.isMarkingAll || this.unreadCount === 0) return;
    this.isMarkingAll = true;
    this.notifService.markAllAsRead().subscribe({
      next: () => {
        if (this.page) {
          this.page.items.forEach(n => { n.isRead = true; n.readAt = new Date().toISOString(); });
          this.page.unreadCount = 0;
        }
        this.isMarkingAll = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isMarkingAll = false;
        this.cdr.markForCheck();
      }
    });
  }

  getTypeIcon(type: NotificationDto['type']): string {
    switch (type) {
      case 'SubmissionAccepted':   return '✓';
      case 'SubmissionWrong':      return '✗';
      case 'CompetitionStarting':  return '⏱';
      case 'CompetitionStarted':   return '🏁';
      default: return '●';
    }
  }

  getTypeClass(type: NotificationDto['type']): string {
    switch (type) {
      case 'SubmissionAccepted':   return 'notif--accepted';
      case 'SubmissionWrong':      return 'notif--wrong';
      case 'CompetitionStarting':  return 'notif--starting';
      case 'CompetitionStarted':   return 'notif--started';
      default: return '';
    }
  }

  pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }
}
