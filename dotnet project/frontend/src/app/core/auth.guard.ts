import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UserRole } from './models';

export const authGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.session) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: router.url } });
  }
  const roles = route.data['roles'] as UserRole[] | undefined;
  if (roles?.length && !roles.includes(auth.role!)) {
    return router.createUrlTree(['/app/dashboard']);
  }
  return true;
};
