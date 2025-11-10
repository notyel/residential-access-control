import { Routes } from '@angular/router';
import { VisitsComponent } from './pages/visits/visits.component';
import { RegisterVisitComponent } from './pages/register-visit/register-visit.component';

const VISITS_ROUTES: Routes = [
  {
    path: '',
    component: VisitsComponent,
  },
  {
    path: 'register',
    component: RegisterVisitComponent,
  },
];

export default VISITS_ROUTES;
