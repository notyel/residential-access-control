import { Routes } from '@angular/router';
import { P } from '../../../core/security/permissions';
import { UserFormComponent } from './pages/user-form/user-form.component';
import { UsersComponent } from './pages/users/users.component';

const USERS_URL = '/access-control/users';
const PARENT = { label: 'Usuarios', url: USERS_URL };

const USERS_ROUTES: Routes = [
  {
    path: '',
    component: UsersComponent,
    title: 'Usuarios',
    data: { permission: P.usersRead, subtitle: 'Quién entra a la plataforma' },
  },
  {
    path: 'new',
    component: UserFormComponent,
    title: 'Nuevo usuario',
    data: { permission: P.usersManage, backUrl: USERS_URL, parent: PARENT },
  },
  {
    path: 'edit/:id',
    component: UserFormComponent,
    title: 'Editar usuario',
    data: { permission: P.usersManage, backUrl: USERS_URL, parent: PARENT },
  },
];

export default USERS_ROUTES;
