import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './core/layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './core/layouts/dashboard-layout/dashboard-layout.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    component: AuthLayoutComponent,
    loadChildren: () => import('./modules/auth/auth.routes'),
  },
  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    loadComponent: () =>
      import('./modules/dashboard/pages/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent
      ),
  },
  {
    path: 'visits',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    loadChildren: () => import('./modules/visits/visits.routes'),
  },
  {
    path: 'residents',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    loadChildren: () => import('./modules/residents/residents.routes'),
  },
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full',
  },
];
