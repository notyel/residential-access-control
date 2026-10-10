import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { P } from '../../../core/security/permissions';

const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    title: 'Panel de control',
    data: { permission: P.dashboardRead },
  },
];

export default DASHBOARD_ROUTES;
