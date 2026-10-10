import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // token ausente o vencido: se limpia la sesión y se recuerda a dónde iba
  authService.logout();
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
