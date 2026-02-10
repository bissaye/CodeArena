import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CompetitionService } from '../../../core/services/competition.service';

@Component({
  selector: 'app-competition-form',
  standalone: true,
  imports: [RouterLink, TranslatePipe, ReactiveFormsModule],
  templateUrl: './competition-form.component.html',
  styleUrl: './competition-form.component.scss',
})
export class CompetitionFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly competitionService = inject(CompetitionService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  isEditMode = false;
  competitionId: string | null = null;
  isLoading = false;
  isSaving = false;
  error: string | null = null;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    startDate: ['', Validators.required],
    startTime: ['00:00', Validators.required],
    durationHours: [1, [Validators.required, Validators.min(0)]],
    durationMinutes: [0, [Validators.required, Validators.min(0), Validators.max(59)]],
    publish: [false],
  });

  ngOnInit(): void {
    this.competitionId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.competitionId;

    if (this.isEditMode && this.competitionId) {
      this.isLoading = true;
      this.competitionService.getById(this.competitionId).subscribe({
        next: (comp) => {
          const start = new Date(comp.startDate);
          const pad = (n: number) => String(n).padStart(2, '0');
          this.form.patchValue({
            name: comp.name,
            startDate: start.toISOString().substring(0, 10),
            startTime: `${pad(start.getHours())}:${pad(start.getMinutes())}`,
            durationHours: this.getTotalMinutes(comp.startDate, comp.endDate) >= 60
              ? Math.floor(this.getTotalMinutes(comp.startDate, comp.endDate) / 60)
              : 0,
            durationMinutes: this.getTotalMinutes(comp.startDate, comp.endDate) % 60,
            publish: comp.status !== 'Draft',
          });
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.error = 'admin.competition.error.load';
          this.isLoading = false;
          this.cdr.markForCheck();
        }
      });
    }
  }

  private getTotalMinutes(startDate: string, endDate: string): number {
    return Math.round((new Date(endDate).getTime() - new Date(startDate).getTime()) / 60000);
  }

  save(): void {
    if (this.form.invalid) return;
    this.isSaving = true;
    this.error = null;

    const v = this.form.value;
    const startDate = new Date(`${v['startDate']}T${v['startTime']}:00`).toISOString();
    const request = {
      name: v['name']!,
      startDate,
      durationHours: v['durationHours'] ?? 0,
      durationMinutes: v['durationMinutes'] ?? 0,
      publish: v['publish'] ?? false,
    };

    const action$ = this.isEditMode && this.competitionId
      ? this.competitionService.update(this.competitionId, request)
      : this.competitionService.create(request);

    action$.subscribe({
      next: (res) => {
        this.isSaving = false;
        const id = 'id' in res ? res.id : this.competitionId!;
        this.router.navigate(['/competitions', id]);
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving = false;
        this.error = err.error?.message ?? 'admin.competition.error.save';
        this.cdr.markForCheck();
      },
    });
  }
}
