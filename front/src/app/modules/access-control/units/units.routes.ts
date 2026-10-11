import { Routes } from '@angular/router';
import { P } from '../../../core/security/permissions';
import { UnitDetailComponent } from './pages/unit-detail/unit-detail.component';
import { UnitFormComponent } from './pages/unit-form/unit-form.component';
import { UnitsComponent } from './pages/units/units.component';

const UNITS_URL = '/access-control/units';
const PARENT = { label: 'Unidades', url: UNITS_URL };

const UNITS_ROUTES: Routes = [
  {
    path: '',
    component: UnitsComponent,
    title: 'Unidades inmobiliarias',
    data: { permission: P.unitsRead, subtitle: 'Casas, apartamentos y locales' },
  },
  {
    path: 'new',
    component: UnitFormComponent,
    title: 'Nueva unidad',
    data: { permission: P.unitsWrite, backUrl: UNITS_URL, parent: PARENT },
  },
  {
    path: 'edit/:id',
    component: UnitFormComponent,
    title: 'Editar unidad',
    data: { permission: P.unitsWrite, backUrl: UNITS_URL, parent: PARENT },
  },
  {
    path: ':id',
    component: UnitDetailComponent,
    title: 'Ficha de unidad',
    data: { permission: P.unitsRead, backUrl: UNITS_URL, parent: PARENT },
  },
];

export default UNITS_ROUTES;
