import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BadgeDto } from '../models/badge.models';

@Injectable({ providedIn: 'root' })
export class BadgeService {
  private readonly http = inject(HttpClient);

  getAllBadges(): Observable<BadgeDto[]> {
    return this.http.get<BadgeDto[]>('/api/badges');
  }

  getUserBadges(username: string): Observable<BadgeDto[]> {
    return this.http.get<BadgeDto[]>(`/api/users/${username}/badges`);
  }
}
