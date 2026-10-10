import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Role } from '../models/user.model';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const requiredRoles = route.data['requiredRoles'] as Role[] | undefined;

  if (!requiredRoles?.length) {
    console.error('Role guard requires at least one role to be specified');
    return false;
  }

  const user = authService.getUser();

  if (!user || !requiredRoles.includes(user.role)) {
    return router.createUrlTree(['/access-control/dashboard']);
  }

  return true;
};
