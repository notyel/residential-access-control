import { Routes } from '@angular/router';
import { ComplexFormComponent } from './pages/complex-form/complex-form.component';
import { ComplexesComponent } from './pages/complexes/complexes.component';

const COMPLEXES_URL = '/access-control/complexes';
const PARENT = { label: 'Conjuntos', url: COMPLEXES_URL };

const COMPLEXES_ROUTES: Routes = [
  {
    path: '',
    component: ComplexesComponent,
    title: 'Conjuntos',
    data: { superAdminOnly: true, subtitle: 'Clientes de la plataforma' },
  },
  {
    path: 'new',
    component: ComplexFormComponent,
    title: 'Nuevo conjunto',
    data: { superAdminOnly: true, backUrl: COMPLEXES_URL, parent: PARENT },
  },
  {
    path: 'edit/:id',
    component: ComplexFormComponent,
    title: 'Editar conjunto',
    data: { superAdminOnly: true, backUrl: COMPLEXES_URL, parent: PARENT },
  },
];

export default COMPLEXES_ROUTES;
