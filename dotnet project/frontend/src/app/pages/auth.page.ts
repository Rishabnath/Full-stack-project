import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { ToastService } from '../core/toast.service';
import { passwordMatchValidator } from '../core/validation';
import { IconComponent } from '../shared/icon.component';

@Component({
  selector: 'fs-auth-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, IconComponent],
  template: `
    <main class="auth-layout" [class.auth-layout--register]="isRegister">
      <section class="auth-visual" aria-label="FoodSupply dining experience">
        <div class="auth-visual__image"></div>
        <div class="auth-visual__shade"></div>
        <a routerLink="/login" class="brand brand--light" aria-label="FoodSupply home">
          <span class="brand__mark"><fs-icon name="sparkles" /></span>
          <span class="brand__name">Food<span>Supply</span><small>GOOD FOOD, GATHERED WELL</small></span>
        </a>
        <div class="auth-story">
          <span class="eyebrow eyebrow--light"><span class="eyebrow__line"></span> THE NEIGHBOURHOOD TABLE</span>
          <h1>Good food.<br><em>Better evenings.</em></h1>
          <p>Discover the kitchens your city loves, and bring a little more delight to the table.</p>
          <div class="auth-quote"><span class="auth-quote__stars">★★★★★</span><span>“Our Friday night ritual just found its new favourite.”</span><small>— A note from the neighbourhood</small></div>
        </div>
        <div class="auth-visual__foot"><span>Thoughtfully sourced, beautifully served.</span><a href="https://unsplash.com/photos/a-white-bowl-filled-with-rice-and-meat-ysmeQt1dzcw" target="_blank" rel="noreferrer">Food photography: Unsplash</a></div>
      </section>

      <section class="auth-panel">
        <div class="auth-panel__top"><span class="auth-overline">A seat at the table</span><a class="auth-switch" [routerLink]="isRegister ? '/login' : '/register'">{{ isRegister ? 'Already a member?' : 'New to FoodSupply?' }} <strong>{{ isRegister ? 'Sign in' : 'Create account' }}</strong></a></div>
        <div class="auth-form-wrap">
          <div class="auth-heading">
            <span class="auth-heading__icon"><fs-icon [name]="isRegister ? 'users' : 'sparkles'" /></span>
            <p class="eyebrow">{{ isRegister ? 'YOUR TABLE IS WAITING' : 'WELCOME BACK' }}</p>
            <h2>{{ isRegister ? 'Join the table.' : 'Come on in.' }}</h2>
            <p>{{ isRegister ? 'Create your customer account to discover local favourites and order something lovely.' : 'Sign in to pick up right where your appetite left off.' }}</p>
          </div>

          @if (errorMessage) { <div class="form-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span></div> }

          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            @if (isRegister) {
              <div class="form-row">
                <label class="field"><span>First name</span><input formControlName="firstName" autocomplete="given-name" placeholder="Rishab" [class.field__input--invalid]="showError('firstName')"><small class="field__error">{{ errorFor('firstName') }}</small></label>
                <label class="field"><span>Last name</span><input formControlName="lastName" autocomplete="family-name" placeholder="Nath" [class.field__input--invalid]="showError('lastName')"><small class="field__error">{{ errorFor('lastName') }}</small></label>
              </div>
              <label class="field"><span>Phone number <small>(optional)</small></span><input formControlName="phoneNumber" type="tel" autocomplete="tel" placeholder="+91 98765 43210" [class.field__input--invalid]="showError('phoneNumber')"><small class="field__error">{{ errorFor('phoneNumber') }}</small></label>
            }
            <label class="field"><span>Email address</span><input formControlName="email" type="email" autocomplete="email" placeholder="you@example.com" [class.field__input--invalid]="showError('email')"><small class="field__error">{{ errorFor('email') }}</small></label>
            <label class="field"><span>Password</span><div class="input-with-icon"><input formControlName="password" [type]="showPassword ? 'text' : 'password'" [autocomplete]="isRegister ? 'new-password' : 'current-password'" placeholder="{{ isRegister ? 'At least 8 characters' : 'Enter your password' }}" [class.field__input--invalid]="showError('password')"><button type="button" class="password-toggle" (click)="showPassword = !showPassword" [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'">{{ showPassword ? 'HIDE' : 'SHOW' }}</button></div><small class="field__error">{{ errorFor('password') }}</small></label>
            @if (isRegister) {
              <label class="field"><span>Confirm password</span><input formControlName="confirmPassword" type="password" autocomplete="new-password" placeholder="Type your password again" [class.field__input--invalid]="showError('confirmPassword')"><small class="field__error">{{ errorFor('confirmPassword') }}</small></label>
              <div class="role-note"><span class="role-note__dot"></span><span>Every new account starts as a <strong>Customer</strong>. You can apply to become a restaurant owner or delivery partner after signing in.</span></div>
            }
            @if (!isRegister) { <div class="form-options"><span class="secure-note"><fs-icon name="sessions" /> Your session is protected and expires automatically.</span></div> }
            <button class="button button--primary button--full auth-submit" type="submit" [disabled]="busy">
              @if (busy) { <span class="spinner"></span><span>{{ isRegister ? 'Creating your account…' : 'Signing you in…' }}</span> }
              @else { <span>{{ isRegister ? 'Create my account' : 'Sign in to FoodSupply' }}</span><fs-icon name="arrow" /> }
            </button>
          </form>
          <p class="auth-terms">By continuing, you agree to our <a href="#" (click)="$event.preventDefault()">Terms</a> and <a href="#" (click)="$event.preventDefault()">Privacy Policy</a>.</p>
        </div>
        <div class="auth-panel__bottom"><span>© {{ year }} FoodSupply</span><span><i></i> Made for good company</span></div>
      </section>
    </main>
  `
})
export class AuthPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly year = new Date().getFullYear();
  readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.maxLength(30), Validators.pattern(/^$|^[+()\d .-]{7,30}$/)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    confirmPassword: ['', [Validators.required, Validators.maxLength(128)]],
  }, { validators: passwordMatchValidator });
  isRegister = this.route.snapshot.data['mode'] === 'register';
  busy = false;
  submitted = false;
  showPassword = false;
  errorMessage = '';

  constructor() {
    this.route.data.subscribe(data => { this.configureMode(data['mode'] === 'register'); this.errorMessage = ''; });
  }

  showError(name: string): boolean {
    const control = this.form.get(name);
    return !!control && (control.invalid || (name === 'confirmPassword' && this.form.hasError('passwordMismatch'))) && (control.touched || this.submitted);
  }

  errorFor(name: string): string {
    const control = this.form.get(name);
    if (!control || !this.showError(name)) return '';
    if (name === 'confirmPassword' && this.form.hasError('passwordMismatch')) return 'Those passwords do not match.';
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('email')) return 'Enter a valid email address.';
    if (control.hasError('minlength')) return 'Use at least 8 characters.';
    if (control.hasError('maxlength')) return 'This value is too long.';
    if (control.hasError('pattern')) return 'Enter a valid phone number.';
    return 'Please check this field.';
  }

  submit(): void {
    this.submitted = true;
    this.errorMessage = '';
    if (this.isRegister) {
      this.form.get('firstName')?.markAsTouched();
      this.form.get('lastName')?.markAsTouched();
      this.form.get('confirmPassword')?.markAsTouched();
    }
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.busy = true;
    const value = this.form.getRawValue();
    const request = this.isRegister
      ? this.auth.register({ firstName: value.firstName!.trim(), lastName: value.lastName!.trim(), email: value.email!.trim(), phoneNumber: value.phoneNumber?.trim() || null, password: value.password, confirmPassword: value.confirmPassword })
      : this.auth.login(value.email!.trim(), value.password!);
    request.pipe(finalize(() => this.busy = false)).subscribe({
      next: () => {
        this.toast.show(this.isRegister ? 'Welcome to FoodSupply. Your account is ready.' : 'Welcome back. Your table is waiting.', 'success');
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        void this.router.navigateByUrl(returnUrl?.startsWith('/app') ? returnUrl : '/app/dashboard');
      },
      error: error => this.errorMessage = getErrorMessage(error, this.isRegister ? 'We could not create your account.' : 'We could not sign you in.')
    });
  }

  private configureMode(register: boolean): void {
    this.isRegister = register;
    const firstName = this.form.controls.firstName;
    const lastName = this.form.controls.lastName;
    const phoneNumber = this.form.controls.phoneNumber;
    const confirmPassword = this.form.controls.confirmPassword;
    if (register) {
      firstName.setValidators([Validators.required, Validators.maxLength(100)]);
      lastName.setValidators([Validators.required, Validators.maxLength(100)]);
      phoneNumber.setValidators([Validators.maxLength(30), Validators.pattern(/^$|^[+()\d .-]{7,30}$/)]);
      confirmPassword.setValidators([Validators.required, Validators.maxLength(128)]);
    } else {
      firstName.clearValidators();
      lastName.clearValidators();
      phoneNumber.clearValidators();
      confirmPassword.clearValidators();
    }
    firstName.updateValueAndValidity();
    lastName.updateValueAndValidity();
    phoneNumber.updateValueAndValidity();
    confirmPassword.updateValueAndValidity();
    this.form.updateValueAndValidity();
  }
}
