import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    title: 'Panel de control',
  },
];

export default DASHBOARD_ROUTES;
