import { Routes } from '@angular/router';
import { LoginFormComponent } from './components/login-form/login-form.component';
import { LoginLayoutComponent } from './login-layout.component';

export const LOGIN_ROUTES: Routes = [
  {
    path: '',
    component: LoginLayoutComponent,
    children: [
      {
        path: '',
        component: LoginFormComponent,
      },
    ],
  },
];