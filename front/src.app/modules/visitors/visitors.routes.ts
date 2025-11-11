import { Routes } from '@angular/router';
import { VisitorsComponent } from './visitors.component';
import { VisitorFormComponent } from './visitor-form.component';

const routes: Routes = [
  {
    path: '',
    component: VisitorsComponent
  },
  {
    path: 'add',
    component: VisitorFormComponent
  },
  {
    path: 'edit/:id',
    component: VisitorFormComponent
  }
];

export default routes;
