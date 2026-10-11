import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './core/layouts/auth-layout/auth-layout.component';
import { AccessControlLayoutComponent } from './core/layouts/access-control-layout/access-control-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: AuthLayoutComponent,
    loadChildren: () =>
      import('./modules/login/login.routes').then((m) => m.default),
  },
  // Páginas públicas de la cuenta; los correos enlazan a /invitation y /reset-password con ?token=
  ...(['forgot', 'reset', 'invitation'] as const).map((mode) => ({
    path: { forgot: 'forgot-password', reset: 'reset-password', invitation: 'invitation' }[mode],
    component: AuthLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./modules/login/pages/account/account.component').then(
            (m) => m.AccountComponent
          ),
        title: { forgot: 'Recuperar contraseña', reset: 'Nueva contraseña', invitation: 'Activar cuenta' }[mode],
        data: { mode },
      },
    ],
  })),
  {
    path: 'access-control',
    component: AccessControlLayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [permissionGuard],
    loadChildren: () =>
      import('./modules/access-control/access-control.routes').then(
        (m) => m.default
      ),
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
];
