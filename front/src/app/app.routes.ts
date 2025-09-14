import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./modules/dashboard/dashboard.routes').then(
        (m) => m.DASHBOARD_ROUTES
      ),
  },
  {
    path: 'login',
    loadChildren: () =>
      import('./modules/login/login.routes').then(
        (m) => m.LOGIN_ROUTES
      ),
  },
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full',
  },
];
