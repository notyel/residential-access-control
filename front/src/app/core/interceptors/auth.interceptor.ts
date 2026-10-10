import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ComplexContextService } from '../services/complex-context.service';

/**
 * Añade el token a cada petición y trata la sesión vencida: ante un 401 cierra la sesión y
 * lleva al login recordando la ruta actual. Un 403 no cierra la sesión; lo muestra quien llamó.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();
  const isLogin = request.url.endsWith('/auth/login');

  if (token && !isLogin) {
    const headers: Record<string, string> = { Authorization: `Bearer ${token}` };
    // Solo el superadministrador elige conjunto; el servidor ignora el encabezado en los demás
    const complex = inject(ComplexContextService).selected();
    if (complex && authService.isSuperAdmin()) {
      headers['X-Complex-Id'] = complex.id;
    }
    request = request.clone({ setHeaders: headers });
  }

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isLogin
      ) {
        authService.logout();
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url },
        });
      }
      return throwError(() => error);
    })
  );
};
