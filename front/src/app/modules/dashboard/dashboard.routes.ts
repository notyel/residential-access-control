import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { ReportsComponent } from './pages/reports/reports.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: HomeComponent,
  },
  {
    path: 'reports',
    component: ReportsComponent,
  },
];
