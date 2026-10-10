import { Routes } from '@angular/router';
import { P } from '../../../core/security/permissions';
import { RoleFormComponent } from './pages/role-form/role-form.component';
import { RolesComponent } from './pages/roles/roles.component';

const ROLES_URL = '/access-control/roles';
const PARENT = { label: 'Roles', url: ROLES_URL };

const ROLES_ROUTES: Routes = [
  {
    path: '',
    component: RolesComponent,
    title: 'Roles',
    data: { permission: P.rolesRead, subtitle: 'Qué puede hacer cada tipo de usuario' },
  },
  {
    path: 'new',
    component: RoleFormComponent,
    title: 'Nuevo rol',
    data: { permission: P.rolesManage, backUrl: ROLES_URL, parent: PARENT },
  },
  {
    path: 'edit/:id',
    component: RoleFormComponent,
    title: 'Editar rol',
    // con solo lectura se abre para consultar los permisos; el formulario queda deshabilitado
    data: { permission: P.rolesRead, backUrl: ROLES_URL, parent: PARENT },
  },
];

export default ROLES_ROUTES;
