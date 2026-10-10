import { inject } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivateChildFn,
  Router,
} from '@angular/router';
import { PermissionRule } from '../security/permissions';
import { AuthService } from '../services/auth.service';

/** Página que explica que la ruta existe pero el usuario no tiene permiso para abrirla. */
export const FORBIDDEN_ROUTE = '/access-control/forbidden';

/**
 * Se aplica con canActivateChild a todo /access-control. Cada ruta declara en data.permission
 * el permiso que la habilita (o superAdminOnly, o public); una ruta que no declara nada queda
 * bloqueada. Evita abrir por URL lo que el menú no muestra; el servidor protege los datos.
 */
export const permissionGuard: CanActivateChildFn = (
  route: ActivatedRouteSnapshot
) => {
  // las rutas contenedoras (lazy, redirecciones) no declaran nada: decide la ruta hoja
  if (route.firstChild || !route.routeConfig || route.routeConfig.redirectTo) {
    return true;
  }

  const authService = inject(AuthService);
  const forbidden = inject(Router).createUrlTree([FORBIDDEN_ROUTE]);

  if (route.data['public']) {
    return true;
  }
  if (route.data['superAdminOnly']) {
    return authService.isSuperAdmin() ? true : forbidden;
  }

  const permission = route.data['permission'] as PermissionRule | undefined;
  if (!permission?.length) {
    console.error(
      `La ruta "${route.routeConfig.path}" no declara data.permission`
    );
    return forbidden;
  }

  return authService.hasPermission(permission) ? true : forbidden;
};
