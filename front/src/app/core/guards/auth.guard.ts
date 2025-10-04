import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
        return true;
      } else {
        router.navigate(['/login']);
        return false;
      }

  // Redirigir al login si no está autenticado
  return router.createUrlTree(['/login']);
};