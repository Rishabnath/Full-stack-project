import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value as string | undefined;
  const confirmPassword = control.get('confirmPassword')?.value as string | undefined;
  return password && confirmPassword && password !== confirmPassword ? { passwordMismatch: true } : null;
};

export const httpUrlPattern = /^https?:\/\/.+/i;
