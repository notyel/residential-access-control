import { Routes } from '@angular/router';
import { ResidentsComponent } from './pages/residents/residents.component';
import { EditResidentComponent } from './pages/edit-resident/edit-resident.component';
import { NewResidentComponent } from './pages/new-resident/new-resident.component';

const RESIDENTS_ROUTES: Routes = [
  {
    path: '',
    component: ResidentsComponent,
  },
  {
    path: 'new',
    component: NewResidentComponent,
  },
  {
    path: 'edit/:id',
    component: EditResidentComponent,
  },
];

export default RESIDENTS_ROUTES;
