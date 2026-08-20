import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CompetitionSummary, CompetitionDetail,
  CreateCompetitionRequest, UpdateCompetitionRequest, CompetitionMutationResponse
} from '../models/competition.models';
import { LeaderboardEntry } from '../models/leaderboard.models';

@Injectable({ providedIn: 'root' })
export class CompetitionService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/competitions';

  getAll(): Observable<CompetitionSummary[]> {
    return this.http.get<CompetitionSummary[]>(this.base);
  }

  getById(id: string): Observable<CompetitionDetail> {
    return this.http.get<CompetitionDetail>(`${this.base}/${id}`);
  }

  getLeaderboard(id: string, top = 10): Observable<LeaderboardEntry[]> {
    return this.http.get<LeaderboardEntry[]>(`${this.base}/${id}/leaderboard`, { params: { top } });
  }

  create(request: CreateCompetitionRequest): Observable<CompetitionMutationResponse> {
    return this.http.post<CompetitionMutationResponse>(this.base, request);
  }

  update(id: string, request: UpdateCompetitionRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.base}/${id}`, request);
  }
}
