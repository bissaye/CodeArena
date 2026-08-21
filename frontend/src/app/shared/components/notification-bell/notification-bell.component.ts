import {
  Component, OnInit, OnDestroy, HostListener,
  ChangeDetectionStrategy, ChangeDetectorRef, inject
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDto, NotificationsPage } from '../../../core/models/notification.models';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, TranslatePipe],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss',
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private readonly notifService = inject(NotificationService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  isOpen = false;
  isLoading = false;
  isMarkingAll = false;
  error: string | null = null;
  page: NotificationsPage | null = null;

  private refreshSub?: Subscription;

  get unreadCount(): number {
    return this.page?.unreadCount ?? 0;
  }

  get notifications(): NotificationDto[] {
    return this.page?.items ?? [];
  }

  ngOnInit(): void {
    this.loadNotifications();

    // Start SignalR connection if authenticated — replaces polling
    if (this.authService.isAuthenticated()) {
      this.notifService.startConnection();
    }

    // refresh$ fires on every SignalR ReceiveNotification event (and on triggerRefresh() calls)
    this.refreshSub = this.notifService.refresh$.subscribe(() => {
      this.loadNotifications();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
    // Do not stop the connection here — service is a singleton and other components may use it
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notif-bell')) {
      this.isOpen = false;
      this.cdr.markForCheck();
    }
  }

  toggleOpen(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.page === null) {
      this.loadNotifications();
    }
    this.cdr.markForCheck();
  }

  loadNotifications(): void {
    this.notifService.getNotifications(false, 1).subscribe({
      next: (data) => {
        this.page = data;
        this.error = null;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'notifications.error_load';
        this.cdr.markForCheck();
      }
    });
  }

  markAsRead(notif: NotificationDto, event: Event): void {
    event.stopPropagation();
    if (notif.isRead) return;
    notif.isRead = true;
    notif.readAt = new Date().toISOString();
    if (this.page) this.page.unreadCount = Math.max(0, this.page.unreadCount - 1);
    this.cdr.markForCheck();
    this.notifService.markAsRead(notif.id).subscribe();
  }

  markAllAsRead(event: Event): void {
    event.stopPropagation();
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

  getTypeClass(type: NotificationDto['type']): string {
    switch (type) {
      case 'SubmissionAccepted':   return 'notif-item--accepted';
      case 'SubmissionWrong':      return 'notif-item--wrong';
      case 'CompetitionStarting':  return 'notif-item--starting';
      case 'CompetitionStarted':   return 'notif-item--started';
      case 'BadgeEarned':          return 'notif-item--badge';
      default: return '';
    }
  }

  getTypeIcon(type: NotificationDto['type']): string {
    switch (type) {
      case 'SubmissionAccepted':   return '✓';
      case 'SubmissionWrong':      return '✗';
      case 'CompetitionStarting':  return '⏱';
      case 'CompetitionStarted':   return '🏁';
      case 'BadgeEarned':          return '🏅';
      default: return '●';
    }
  }
}
