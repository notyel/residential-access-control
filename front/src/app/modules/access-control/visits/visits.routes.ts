import { Routes } from '@angular/router';
import { VisitsComponent } from './pages/visits/visits.component';
import { RegisterVisitComponent } from './pages/register-visit/register-visit.component';
import { roleGuard } from '../../../core/guards/role.guard';
import { Role } from '../../../core/models/user.model';

const VISITS_ROUTES: Routes = [
  {
    path: '',
    component: VisitsComponent,
    title: 'Visitas',
    data: { subtitle: 'Registro y seguimiento de ingresos' },
  },
  {
    path: 'register',
    component: RegisterVisitComponent,
    canActivate: [roleGuard],
    title: 'Registrar visita',
    data: {
      requiredRoles: [Role.Admin, Role.Guard],
      backUrl: '/access-control/visits',
      parent: { label: 'Visitas', url: '/access-control/visits' },
    },
  },
];

export default VISITS_ROUTES;
