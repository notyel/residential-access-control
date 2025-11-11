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
    path: 'visitors',
    loadChildren: () => import('../visitors/visitors.routes').then((m) => m.default),
  },
  {
    path: 'residents',
    loadChildren: () =>
      import('./residents/residents.routes').then((m) => m.default),
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
];

export default accessControlRoutes;
