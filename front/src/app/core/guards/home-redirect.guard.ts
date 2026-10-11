import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { MenuService } from '../services/menu.service';
import { FORBIDDEN_ROUTE } from './permission.guard';

/**
 * Entrada a /access-control sin pantalla concreta: lleva a la primera opción del menú del
 * usuario, que depende de sus permisos. Sin ninguna opción, a "Sin acceso".
 */
export const homeRedirectGuard: CanActivateFn = () => {
  const router = inject(Router);

  return inject(MenuService)
    .getMenuForCurrentUser()
    .pipe(map((items) => router.parseUrl(items[0]?.path ?? FORBIDDEN_ROUTE)));
};
