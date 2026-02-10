import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AdminService } from '../../core/services/admin.service';
import { ToastService } from '../../core/services/toast.service';
import { ModeratorEntry } from '../../core/models/user.models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DatePipe, TranslatePipe],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  error: string | null = null;
  moderators: ModeratorEntry[] = [];

  isSaving = false;
  addError: string | null = null;

  confirmTarget: ModeratorEntry | null = null;
  isRemoving = false;

  readonly addForm = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(30)]],
  });

  ngOnInit(): void {
    this.loadModerators();
  }

  private loadModerators(): void {
    this.isLoading = true;
    this.adminService.getModerators().subscribe({
      next: (list) => { this.moderators = list; this.isLoading = false; this.cdr.markForCheck(); },
      error: () => { this.error = 'admin.moderators.error.load'; this.isLoading = false; this.cdr.markForCheck(); },
    });
  }

  onAdd(): void {
    if (this.addForm.invalid || this.isSaving) return;
    this.isSaving = true;
    this.addError = null;
    const username = this.addForm.value.username!.trim();

    this.adminService.addModerator(username).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.addForm.reset();
        this.isSaving = false;
        this.loadModerators();
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving = false;
        if (err.status === 404) {
          this.addError = 'admin.moderators.error.user_not_found';
        } else if (err.status === 409) {
          this.addError = 'admin.moderators.error.already_moderator';
        } else {
          this.addError = 'admin.moderators.error.save';
        }
        this.cdr.markForCheck();
      },
    });
  }

  openConfirm(moderator: ModeratorEntry): void {
    this.confirmTarget = moderator;
  }

  cancelConfirm(): void {
    this.confirmTarget = null;
  }

  confirmRemove(): void {
    if (!this.confirmTarget || this.isRemoving) return;
    this.isRemoving = true;
    const target = this.confirmTarget;

    this.adminService.removeModerator(target.userId).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.confirmTarget = null;
        this.isRemoving = false;
        this.loadModerators();
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isRemoving = false;
        this.confirmTarget = null;
        if (err.status === 400) {
          this.toast.error(err.error?.message ?? 'admin.moderators.error.self_remove');
        } else {
          this.toast.error('admin.moderators.error.remove');
        }
        this.cdr.markForCheck();
      },
    });
  }
}
