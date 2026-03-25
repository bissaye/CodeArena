import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { UserService } from '../../core/services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { UserProfile } from '../../core/models/user.models';
import { COUNTRIES } from '../../core/constants/countries';
import { CAMEROON_REGIONS } from '../../core/models/regions';
import { CountryFlagPipe } from '../../shared/pipes/country-flag.pipe';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DatePipe, ReactiveFormsModule, CountryFlagPipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly userService = inject(UserService);
  readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly toast = inject(ToastService);

  // Page state
  isLoading = true;
  error: string | null = null;
  profile: UserProfile | null = null;
  username = '';

  // Edit mode
  isEditing = false;
  isSaving = false;
  saveError: string | null = null;
  saveSuccess = false;

  // Avatar
  avatarPreview: string | null = null;
  avatarFile: File | null = null;
  isUploadingAvatar = false;
  avatarError: string | null = null;

  // Email verification
  isResendingVerification = false;

  // Change password mode
  isChangingPassword = false;
  isSavingPassword = false;
  passwordError: string | null = null;
  passwordSuccess = false;

  countries = COUNTRIES;
  readonly cameroonRegions = CAMEROON_REGIONS;
  schoolSuggestions: string[] = [];

  profileForm = this.fb.group({
    country: ['', Validators.required],
    region: [''],
    school: [''],
  });

  passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  });

  get isOwner(): boolean {
    return this.authService.currentUser?.username === this.username;
  }

  get isAdmin(): boolean {
    return this.authService.hasRole('Admin');
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.username = params.get('username') ?? '';
      this.loadProfile();
    });

    this.userService.getSchools().subscribe({
      next: (schools) => { this.schoolSuggestions = schools; this.cdr.markForCheck(); },
      error: () => { /* non-bloquant — liste vide si l'endpoint est indisponible */ },
    });
  }

  loadProfile(): void {
    this.isLoading = true;
    this.error = null;
    this.userService.getProfile(this.username).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isLoading = false;
        this.profileForm.patchValue({
          country: profile.country,
          region: profile.region ?? '',
          school: profile.school ?? '',
        });
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.error = err.status === 404
          ? 'profile.error.not_found'
          : 'profile.error.generic';
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  openEditMode(): void {
    this.isEditing = true;
    this.saveError = null;
    this.saveSuccess = false;
  }

  resendVerification(): void {
    if (this.isResendingVerification) return;
    this.isResendingVerification = true;
    this.authService.resendVerification().subscribe({
      next: () => {
        this.isResendingVerification = false;
        this.toast.success('profile.verification_sent');
        this.cdr.markForCheck();
      },
      error: () => {
        this.isResendingVerification = false;
        this.toast.error('profile.verification_error');
        this.cdr.markForCheck();
      },
    });
  }

  cancelEdit(): void {
    this.isEditing = false;
    if (this.profile) {
      this.profileForm.patchValue({
        country: this.profile.country,
        region: this.profile.region ?? '',
        school: this.profile.school ?? '',
      });
    }
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.isSaving = true;
    this.saveError = null;
    this.saveSuccess = false;

    const { country, region, school } = this.profileForm.value;
    this.userService.updateProfile(this.username, {
      country: country!,
      region: region || undefined,
      school: school || undefined,
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.saveSuccess = true;
        this.isEditing = false;
        this.loadProfile();
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving = false;
        this.saveError = err.error?.message ?? 'profile.error.save_failed';
        this.cdr.markForCheck();
      },
    });
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.avatarFile = file;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.avatarPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);
  }

  uploadAvatar(): void {
    if (!this.avatarFile) return;
    this.isUploadingAvatar = true;
    this.avatarError = null;

    this.userService.uploadAvatar(this.username, this.avatarFile).subscribe({
      next: (result) => {
        this.isUploadingAvatar = false;
        this.avatarFile = null;
        if (this.profile) {
          this.profile = { ...this.profile, avatarUrl: result.avatarUrl };
        }
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isUploadingAvatar = false;
        this.avatarError = err.error?.message ?? 'profile.error.avatar_failed';
        this.cdr.markForCheck();
      },
    });
  }

  cancelAvatar(): void {
    this.avatarFile = null;
    this.avatarPreview = null;
    this.avatarError = null;
  }

  openPasswordChange(): void {
    this.isChangingPassword = true;
    this.passwordError = null;
    this.passwordSuccess = false;
    this.passwordForm.reset();
  }

  cancelPasswordChange(): void {
    this.isChangingPassword = false;
    this.passwordForm.reset();
  }

  savePassword(): void {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;
    if (newPassword !== confirmPassword) {
      this.passwordError = 'profile.error.passwords_mismatch';
      return;
    }

    this.isSavingPassword = true;
    this.passwordError = null;

    this.userService.changePassword({
      currentPassword: currentPassword!,
      newPassword: newPassword!,
      confirmPassword: confirmPassword!,
    }).subscribe({
      next: () => {
        this.isSavingPassword = false;
        this.passwordSuccess = true;
        this.isChangingPassword = false;
        this.passwordForm.reset();
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.isSavingPassword = false;
        this.passwordError = err.error?.message ?? 'profile.error.password_failed';
        this.cdr.markForCheck();
      },
    });
  }

  getAvatarSrc(): string {
    if (this.avatarPreview) return this.avatarPreview;
    if (this.profile?.avatarUrl) return `/${this.profile.avatarUrl}`;
    return '';
  }

  getInitials(): string {
    return (this.profile?.username ?? '?').slice(0, 2).toUpperCase();
  }

  getStatusClass(status: string): string {
    if (status === 'Accepted') return 'submission-badge--accepted';
    if (status === 'Wrong') return 'submission-badge--wrong';
    return 'submission-badge--pending';
  }

  getStatusLabel(status: string): string {
    if (status === 'Accepted') return '✓ AC';
    if (status === 'Wrong') return '✗ WA';
    return '⏳';
  }
}
