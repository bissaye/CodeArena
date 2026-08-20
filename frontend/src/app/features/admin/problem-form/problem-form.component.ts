import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ProblemService } from '../../../core/services/problem.service';
import { MarkdownPipe } from '../../../shared/pipes/markdown.pipe';
import { ProblemEditFiles } from '../../../core/models/problem.models';

@Component({
  selector: 'app-problem-form',
  standalone: true,
  imports: [RouterLink, TranslatePipe, ReactiveFormsModule, MarkdownPipe],
  templateUrl: './problem-form.component.html',
  styleUrl: './problem-form.component.scss',
})
export class ProblemFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly problemService = inject(ProblemService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  isEditMode = false;
  problemId: string | null = null;
  competitionId: string | null = null;

  isLoading = false;
  isSaving = false;
  error: string | null = null;
  showPreview = false;

  existingFiles: ProblemEditFiles | null = null;
  inputFile: File | null = null;
  outputFile: File | null = null;

  form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    body: ['', Validators.required],
    points: [100, [Validators.required, Validators.min(1)]],
    replaceInputFile: [false],
    replaceOutputFile: [false],
  });

  get bodyValue(): string {
    return this.form.value['body'] ?? '';
  }

  ngOnInit(): void {
    this.problemId = this.route.snapshot.paramMap.get('id');
    this.competitionId = this.route.snapshot.paramMap.get('competitionId')
      ?? this.route.snapshot.queryParamMap.get('competitionId');
    this.isEditMode = !!this.problemId;

    if (this.isEditMode && this.problemId) {
      this.isLoading = true;
      this.problemService.getById(this.problemId).subscribe({
        next: (p) => {
          this.competitionId = p.competitionId;
          this.form.patchValue({ title: p.title, body: p.body, points: p.points });
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.error = 'admin.problem.error.load';
          this.isLoading = false;
          this.cdr.markForCheck();
        }
      });

      this.problemService.getEditFiles(this.problemId).subscribe({
        next: (files) => { this.existingFiles = files; this.cdr.markForCheck(); },
        error: () => { this.cdr.markForCheck(); }
      });
    }
  }

  onInputFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.inputFile = input.files?.[0] ?? null;
    if (this.inputFile) this.form.patchValue({ replaceInputFile: true });
  }

  onOutputFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.outputFile = input.files?.[0] ?? null;
    if (this.outputFile) this.form.patchValue({ replaceOutputFile: true });
  }

  save(): void {
    if (this.form.invalid) return;
    if (!this.isEditMode && (!this.inputFile || !this.outputFile)) {
      this.error = 'admin.problem.error.files_required';
      return;
    }

    this.isSaving = true;
    this.error = null;

    const v = this.form.value;

    if (this.isEditMode && this.problemId) {
      const request = {
        title: v['title']!,
        body: v['body']!,
        points: v['points']!,
        replaceInputFile: v['replaceInputFile'] ?? false,
        replaceOutputFile: v['replaceOutputFile'] ?? false,
      };
      this.problemService.updateProblem(this.problemId, request, this.inputFile, this.outputFile).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/problems', this.problemId]);
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving = false;
          this.error = err.error?.message ?? 'admin.problem.error.save';
          this.cdr.markForCheck();
        }
      });
    } else if (this.competitionId && this.inputFile && this.outputFile) {
      const request = { title: v['title']!, body: v['body']!, points: v['points']! };
      this.problemService.createProblem(this.competitionId, request, this.inputFile, this.outputFile).subscribe({
        next: (res) => {
          this.isSaving = false;
          this.router.navigate(['/competitions', this.competitionId]);
          this.cdr.markForCheck();
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving = false;
          this.error = err.error?.message ?? 'admin.problem.error.save';
          this.cdr.markForCheck();
        }
      });
    }
  }

  getFileDownloadUrl(relativeUrl: string | null | undefined): string {
    if (!relativeUrl) return '#';
    return '/' + relativeUrl;
  }
}
