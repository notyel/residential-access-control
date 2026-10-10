import { Routes } from '@angular/router';

export const accessControlRoutes: Routes = [
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./dashboard/dashboard.routes').then((m) => m.default),
  },
  {
    path: 'visits',
    loadChildren: () => import('./visits/visits.routes').then((m) => m.default),
  },
  {
    path: 'residents',
    loadChildren: () =>
      import('./residents/residents.routes').then((m) => m.default),
  },
  {
    path: 'complexes',
    loadChildren: () =>
      import('./complexes/complexes.routes').then((m) => m.default),
  },
  {
    path: 'roles',
    loadChildren: () => import('./roles/roles.routes').then((m) => m.default),
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./forbidden/forbidden.component').then(
        (m) => m.ForbiddenComponent
      ),
    title: 'Sin acceso',
    data: { public: true },
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
];

export default accessControlRoutes;
