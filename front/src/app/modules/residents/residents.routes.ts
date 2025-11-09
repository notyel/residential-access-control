import { Routes } from '@angular/router';
import { ResidentsComponent } from './pages/residents/residents.component';
import { EditResidentComponent } from './pages/edit-resident/edit-resident.component';

const routes: Routes = [
  {
    path: '',
    component: ResidentsComponent,
  },
  {
    path: 'edit/:id',
    component: EditResidentComponent,
  },
];

export default routes;
