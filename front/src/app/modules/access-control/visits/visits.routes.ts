import { Routes } from '@angular/router';
import { VisitsComponent } from './pages/visits/visits.component';
import { RegisterVisitComponent } from './pages/register-visit/register-visit.component';
import { roleGuard } from '../../../core/guards/role.guard';
import { Role } from '../../../core/models/user.model';

const VISITS_ROUTES: Routes = [
  {
    path: '',
    component: VisitsComponent,
  },
  {
    path: 'register',
    component: RegisterVisitComponent,
    canActivate: [roleGuard],
    data: { requiredRoles: [Role.Admin, Role.Guard] },
  },
];

export default VISITS_ROUTES;
