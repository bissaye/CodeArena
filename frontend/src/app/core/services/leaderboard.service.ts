import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaderboardEntry, LeaderboardFilters, LeaderboardPage } from '../models/leaderboard.models';

@Injectable({ providedIn: 'root' })
export class LeaderboardService {
  private readonly http = inject(HttpClient);

  getMini(top = 20): Observable<LeaderboardEntry[]> {
    return this.http.get<LeaderboardEntry[]>('/api/leaderboard/mini', { params: { top } });
  }

  getFiltered(filters: LeaderboardFilters = {}): Observable<LeaderboardPage> {
    let params = new HttpParams();
    if (filters.country) params = params.set('country', filters.country);
    if (filters.region) params = params.set('region', filters.region);
    if (filters.school) params = params.set('school', filters.school);
    if (filters.competitionId) params = params.set('competitionId', filters.competitionId);
    if (filters.scoreMin != null) params = params.set('scoreMin', filters.scoreMin);
    if (filters.scoreMax != null) params = params.set('scoreMax', filters.scoreMax);
    if (filters.competitionOnly) params = params.set('competitionOnly', 'true');
    if (filters.search) params = params.set('search', filters.search);
    if (filters.offset != null) params = params.set('offset', filters.offset);
    if (filters.limit != null) params = params.set('limit', filters.limit);
    return this.http.get<LeaderboardPage>('/api/leaderboard', { params });
  }
}
