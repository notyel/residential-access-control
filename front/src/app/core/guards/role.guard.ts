import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth/auth.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const requiredRole = route.data['requiredRole'] as string;

  if (!requiredRole) {
    console.error('Role guard requires a role to be specified');
    return false;
  }

  return authService.currentUser$.pipe(
    take(1),
    map(user => {
      const hasRole = user?.roles.includes(requiredRole) ?? false;
      
      if (!hasRole) {
        router.navigate(['/dashboard']);
        return false;
      }
      
      return true;
    })
  );
};