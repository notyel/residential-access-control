import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { HomeComponent } from './pages/home/home.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: HomeComponent,
    canActivate: [authGuard],
  },
];
