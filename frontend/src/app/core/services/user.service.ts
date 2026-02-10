import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserProfile, UpdateUserRequest, ChangePasswordRequest } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getProfile(username: string): Observable<UserProfile> {
    return this.http.get<UserProfile>(`/api/users/${username}`);
  }

  updateProfile(username: string, request: UpdateUserRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`/api/users/${username}`, request);
  }

  uploadAvatar(username: string, file: File): Observable<{ avatarUrl: string }> {
    const formData = new FormData();
    formData.append('avatarFile', file);
    return this.http.put<{ avatarUrl: string }>(`/api/users/${username}/avatar`, formData);
  }

  changePassword(request: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/auth/change-password', request);
  }

  getRegions(): Observable<string[]> {
    return this.http.get<string[]>('/api/users/regions');
  }

  getSchools(): Observable<string[]> {
    return this.http.get<string[]>('/api/users/schools');
  }
}
