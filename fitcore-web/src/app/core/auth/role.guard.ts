import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Role } from '../../shared/models/api.models';
import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const allowedRoles = (route.data['roles'] ?? []) as Role[];

  return allowedRoles.length === 0 || auth.hasAnyRole(allowedRoles)
    ? true
    : router.createUrlTree(['/dashboard']);
};
