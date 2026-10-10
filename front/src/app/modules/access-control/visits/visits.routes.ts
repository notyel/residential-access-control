import { Routes } from '@angular/router';
import { VisitsComponent } from './pages/visits/visits.component';
import { RegisterVisitComponent } from './pages/register-visit/register-visit.component';
import { P } from '../../../core/security/permissions';

const VISITS_ROUTES: Routes = [
  {
    path: '',
    component: VisitsComponent,
    title: 'Visitas',
    data: {
      permission: [P.visitsRead, P.visitsReadOwn],
      subtitle: 'Registro y seguimiento de ingresos',
    },
  },
  {
    path: 'register',
    component: RegisterVisitComponent,
    title: 'Registrar visita',
    data: {
      permission: P.visitsRegister,
      backUrl: '/access-control/visits',
      parent: { label: 'Visitas', url: '/access-control/visits' },
    },
  },
];

export default VISITS_ROUTES;
