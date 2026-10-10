import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';

const LOGIN_ROUTES: Routes = [
  {
    path: '',
    component: LoginComponent,
    title: 'Iniciar sesión',
  },
];
export default LOGIN_ROUTES;
