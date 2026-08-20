import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';
import { COUNTRIES } from '../../../core/constants/countries';
import { CAMEROON_REGIONS } from '../../../core/models/regions';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const pw = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return pw && confirm && pw !== confirm ? { passwordsMismatch: true } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly countries = COUNTRIES;
  readonly cameroonRegions = CAMEROON_REGIONS;
  schoolSuggestions: string[] = [];

  isLoading = false;
  error: string | null = null;
  success = false;

  form = this.fb.nonNullable.group(
    {
      username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(30), Validators.pattern(/^[a-zA-Z0-9\-]+$/)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      country: ['', [Validators.required]],
      email: ['', [Validators.email]],
      phoneNumber: [''],
      region: [''],
      school: [''],
      acceptCgu: [false, [Validators.requiredTrue]],
    },
    { validators: passwordsMatchValidator }
  );

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    this.userService.getSchools().subscribe({
      next: (schools) => { this.schoolSuggestions = schools; this.cdr.markForCheck(); },
      error: () => { /* non-bloquant — liste vide si l'endpoint est indisponible */ },
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.isLoading) return;

    this.isLoading = true;
    this.error = null;

    const { confirmPassword, acceptCgu, email, phoneNumber, region, school, ...request } = this.form.getRawValue();

    this.authService
      .register({
        ...request,
        email: email || undefined,
        phoneNumber: phoneNumber || undefined,
        region: region || undefined,
        school: school || undefined,
      })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.success = true;
          setTimeout(() => this.router.navigate(['/login']), 2000);
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 409) {
            const msg: string = err.error?.message ?? '';
            this.error = msg.toLowerCase().includes('email')
              ? 'AUTH.REGISTER.ERROR_EMAIL_TAKEN'
              : 'AUTH.REGISTER.ERROR_USERNAME_TAKEN';
          } else {
            this.error = 'AUTH.REGISTER.ERROR_GENERIC';
          }
          this.cdr.markForCheck();
        },
      });
  }
}
