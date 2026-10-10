import { Routes } from '@angular/router';
import { ResidentsComponent } from './pages/residents/residents.component';
import { EditResidentComponent } from './pages/edit-resident/edit-resident.component';
import { NewResidentComponent } from './pages/new-resident/new-resident.component';

const RESIDENTS_URL = '/access-control/residents';
const RESIDENTS_PARENT = { label: 'Residentes', url: RESIDENTS_URL };

const RESIDENTS_ROUTES: Routes = [
  {
    path: '',
    component: ResidentsComponent,
    title: 'Residentes',
    data: { subtitle: 'Gestión de residentes registrados' },
  },
  {
    path: 'new',
    component: NewResidentComponent,
    title: 'Nuevo residente',
    data: { backUrl: RESIDENTS_URL, parent: RESIDENTS_PARENT },
  },
  {
    path: 'edit/:id',
    component: EditResidentComponent,
    title: 'Editar residente',
    data: { backUrl: RESIDENTS_URL, parent: RESIDENTS_PARENT },
  },
];

export default RESIDENTS_ROUTES;
