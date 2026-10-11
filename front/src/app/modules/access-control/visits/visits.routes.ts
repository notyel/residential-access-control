import { Routes } from '@angular/router';
import { VisitsComponent } from './pages/visits/visits.component';
import { P } from '../../../core/security/permissions';

const VISITS_ROUTES: Routes = [
  {
    path: '',
    component: VisitsComponent,
    title: 'Visitantes',
    data: {
      permission: [P.visitsRead, P.visitsReadOwn],
      subtitle: 'Esperados, presentes e historial',
    },
  },
  // el registro se hace ahora desde la validación de portería
  { path: 'register', redirectTo: '/access-control/gate' },
];

export default VISITS_ROUTES;
