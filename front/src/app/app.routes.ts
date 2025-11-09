import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './core/layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './core/layouts/dashboard-layout/dashboard-layout.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    component: AuthLayoutComponent,
    loadChildren: () =>
      import('./modules/auth/auth.module').then((m) => m.AuthModule),
  },
  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    loadChildren: () =>
      import('./modules/dashboard/dashboard.module').then(
        (m) => m.DashboardModule
      ),
  },
  {
    path: 'visits',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    loadChildren: () =>
      import('./modules/visits/visits.module').then(
        (m) => m.VisitsModule
      ),
  },
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full',
  },
];
