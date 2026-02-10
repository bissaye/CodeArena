import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ProblemDetail, SubmitSolutionResult, SubmissionRecord,
  CreateProblemRequest, UpdateProblemRequest, ProblemEditFiles, ProblemMutationResponse
} from '../models/problem.models';

@Injectable({ providedIn: 'root' })
export class ProblemService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/problems';

  getById(id: string): Observable<ProblemDetail> {
    return this.http.get<ProblemDetail>(`${this.base}/${id}`);
  }

  getInputUrl(id: string): string {
    return `${this.base}/${id}/input`;
  }

  getEditFiles(id: string): Observable<ProblemEditFiles> {
    return this.http.get<ProblemEditFiles>(`${this.base}/${id}/input-edit`);
  }

  createProblem(competitionId: string, request: CreateProblemRequest, inputFile: File, outputFile: File): Observable<ProblemMutationResponse> {
    const formData = new FormData();
    formData.append('title', request.title);
    formData.append('body', request.body);
    formData.append('points', String(request.points));
    formData.append('inputFile', inputFile, inputFile.name);
    formData.append('outputFile', outputFile, outputFile.name);
    return this.http.post<ProblemMutationResponse>(`/api/competitions/${competitionId}/problems`, formData);
  }

  updateProblem(id: string, request: UpdateProblemRequest, inputFile?: File | null, outputFile?: File | null): Observable<{ message: string }> {
    const formData = new FormData();
    formData.append('title', request.title);
    formData.append('body', request.body);
    formData.append('points', String(request.points));
    formData.append('replaceInputFile', String(request.replaceInputFile));
    formData.append('replaceOutputFile', String(request.replaceOutputFile));
    if (inputFile) formData.append('inputFile', inputFile, inputFile.name);
    if (outputFile) formData.append('outputFile', outputFile, outputFile.name);
    return this.http.put<{ message: string }>(`${this.base}/${id}`, formData);
  }

  submit(id: string, resultFile: File, sourceFile: File | null): Observable<SubmitSolutionResult> {
    const formData = new FormData();
    formData.append('resultFile', resultFile, resultFile.name);
    if (sourceFile) {
      formData.append('sourceFile', sourceFile, sourceFile.name);
    }
    return this.http.post<SubmitSolutionResult>(`${this.base}/${id}/submit`, formData);
  }

  getMySubmissions(id: string): Observable<SubmissionRecord[]> {
    return this.http.get<SubmissionRecord[]>(`${this.base}/${id}/submissions/me`);
  }
}
