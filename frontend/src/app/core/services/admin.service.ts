import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ModeratorEntry } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  getModerators(): Observable<ModeratorEntry[]> {
    return this.http.get<ModeratorEntry[]>('/api/admin/moderators');
  }

  addModerator(username: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/admin/moderators', { username });
  }

  removeModerator(userId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`/api/admin/moderators/${userId}`);
  }
}
