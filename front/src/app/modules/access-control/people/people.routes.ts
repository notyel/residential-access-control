import { Routes } from '@angular/router';
import { P } from '../../../core/security/permissions';
import { PeopleComponent } from './people.component';
import { PersonComponent } from './person.component';

const PEOPLE_URL = '/access-control/people';
const PARENT = { label: 'Propietarios y residentes', url: PEOPLE_URL };

const PEOPLE_ROUTES: Routes = [
  {
    path: '',
    component: PeopleComponent,
    title: 'Propietarios y residentes',
    data: { permission: P.peopleRead, subtitle: 'Directorio del conjunto' },
  },
  {
    path: 'new',
    component: PersonComponent,
    title: 'Nueva persona',
    data: { permission: P.peopleWrite, backUrl: PEOPLE_URL, parent: PARENT },
  },
  {
    path: ':id',
    component: PersonComponent,
    title: 'Ficha de persona',
    data: { permission: P.peopleRead, backUrl: PEOPLE_URL, parent: PARENT },
  },
];

export default PEOPLE_ROUTES;
