import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { NotificationDto, NotificationsPage } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = '/api/notifications';

  private readonly refreshSubject = new Subject<void>();
  readonly refresh$ = this.refreshSubject.asObservable();

  triggerRefresh(): void {
    this.refreshSubject.next();
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
