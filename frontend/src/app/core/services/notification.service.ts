import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { NotificationDto, NotificationsPage } from '../models/notification.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly apiBase = '/api/notifications';

  private hubConnection?: signalR.HubConnection;

  private readonly refreshSubject = new Subject<void>();
  readonly refresh$ = this.refreshSubject.asObservable();

  private readonly badgeEarnedSubject = new Subject<void>();
  readonly badgeEarned$ = this.badgeEarnedSubject.asObservable();

  startConnection(): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      // Trigger a list refresh so UI shows the new notification
      this.refreshSubject.next();
      // Announce badge events for the problem page toast
      if (notification.type === 'BadgeEarned') {
        this.badgeEarnedSubject.next();
      }
    });

    this.hubConnection.onreconnecting(() => {
      console.debug('[SignalR] Reconnecting…');
    });

    this.hubConnection.start().catch(err =>
      console.error('[SignalR] Connection failed:', err)
    );
  }

  stopConnection(): void {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  // Called from outside (e.g. after a submission) to force an immediate refresh
  triggerRefresh(): void {
    this.refreshSubject.next();
  }

  // Legacy method kept for backward-compat — now auto-called by SignalR event
  announceBadgeEarned(): void {
    this.badgeEarnedSubject.next();
  }

  getNotifications(unreadOnly = false, page = 1): Observable<NotificationsPage> {
    return this.http.get<NotificationsPage>(this.apiBase, {
      params: { unreadOnly: String(unreadOnly), page: String(page) }
    });
  }

  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/${id}/read`, {});
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/read-all`, {});
  }
}
