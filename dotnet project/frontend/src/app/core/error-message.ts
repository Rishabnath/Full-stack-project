import { HttpErrorResponse } from '@angular/common/http';

export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  const body = error.error as { detail?: string; title?: string; message?: string; errors?: Record<string, string[]> } | null;
  const validation = body?.errors ? Object.values(body.errors).flat()[0] : undefined;
  return validation ?? body?.detail ?? body?.message ?? body?.title ?? (error.status === 0
    ? 'Cannot reach the API. Start the FoodSupply backend and check that it is running on port 5215.'
    : fallback);
}
