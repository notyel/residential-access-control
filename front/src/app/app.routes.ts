import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './core/layouts/auth-layout/auth-layout.component';
import { AccessControlLayoutComponent } from './modules/access-control/layouts/access-control-layout/access-control-layout.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: AuthLayoutComponent,
    loadChildren: () =>
      import('./modules/login/login.routes').then((m) => m.default),
  },
  {
    path: 'access-control',
    component: AccessControlLayoutComponent,
    canActivate: [authGuard],
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
