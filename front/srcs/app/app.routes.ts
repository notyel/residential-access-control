import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './core/layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './core/layouts/dashboard-layout/dashboard-layout.component';
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
    component: DashboardLayoutComponent,
    // canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./modules/access-control/dashboard/dashboard.routes').then(
            (m) => m.default
          ),
      },
      {
        path: 'visits',
        loadChildren: () =>
          import('./modules/access-control/visits/visits.routes').then(
            (m) => m.default
          ),
      },
      {
        path: 'residents',
        loadChildren: () =>
          import('./modules/access-control/residents/residents.routes').then(
            (m) => m.default
          ),
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
    ],
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
];
