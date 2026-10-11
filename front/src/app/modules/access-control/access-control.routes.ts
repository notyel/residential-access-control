import { Routes } from '@angular/router';
import { P } from '../../core/security/permissions';
import { homeRedirectGuard } from '../../core/guards/home-redirect.guard';

export const accessControlRoutes: Routes = [
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./dashboard/dashboard.routes').then((m) => m.default),
  },
  {
    path: 'visits',
    loadChildren: () => import('./visits/visits.routes').then((m) => m.default),
  },
  {
    path: 'gate',
    loadComponent: () =>
      import('./gate/gate.component').then((m) => m.GateComponent),
    title: 'Validar visitantes',
    data: {
      permission: P.accessRegister,
      subtitle: 'Busque por documento, nombre o unidad',
    },
  },
  {
    path: 'gate/vehicles',
    loadComponent: () =>
      import('./gate/vehicle-check.component').then(
        (m) => m.VehicleCheckComponent
      ),
    title: 'Verificar vehículos',
    data: {
      permission: P.accessRegister,
      subtitle: 'Escriba la placa para saber si puede ingresar',
    },
  },
  {
    path: 'vehicles',
    loadComponent: () =>
      import('./vehicles/vehicles.component').then((m) => m.VehiclesComponent),
    title: 'Vehículos',
    data: {
      permission: [P.vehiclesRead, P.vehiclesWrite],
      subtitle: 'Inventario de vehículos autorizados',
    },
  },
  {
    path: 'my/vehicles',
    loadComponent: () =>
      import('./my/my-vehicles.component').then((m) => m.MyVehiclesComponent),
    title: 'Mis vehículos',
    data: { permission: P.vehiclesWriteOwn },
  },
  {
    path: 'parking',
    loadComponent: () =>
      import('./parking/parking.component').then((m) => m.ParkingComponent),
    title: 'Parqueaderos',
    data: {
      permission: [P.parkingRead, P.parkingWrite],
      subtitle: 'Disponibilidad calculada en este momento',
    },
  },
  {
    path: 'my/parking',
    loadComponent: () =>
      import('./my/my-parking.component').then((m) => m.MyParkingComponent),
    title: 'Mis parqueaderos',
    data: { permission: P.parkingReadOwn },
  },
  {
    path: 'access-history',
    loadComponent: () =>
      import('./reports/access-history.component').then(
        (m) => m.AccessHistoryComponent
      ),
    title: 'Historial de accesos',
    data: {
      permission: [P.accessRead, P.accessReadOwn],
      subtitle: 'Entradas, salidas, rechazos y anulaciones',
    },
  },
  {
    path: 'reports',
    loadComponent: () =>
      import('./reports/reports.component').then((m) => m.ReportsComponent),
    title: 'Reportes',
    data: { permission: P.reportsRead, subtitle: 'Consultas del conjunto, exportables a CSV' },
  },
  {
    path: 'incidents',
    loadComponent: () =>
      import('./reports/incidents.component').then((m) => m.IncidentsComponent),
    title: 'Novedades',
    data: {
      permission: [P.incidentsWrite, P.accessRead],
      subtitle: 'Novedades del turno de portería',
    },
  },
  {
    path: 'authorizations',
    loadComponent: () =>
      import('./authorizations/authorizations.component').then(
        (m) => m.AuthorizationsComponent
      ),
    title: 'Autorizaciones',
    data: {
      permission: [P.authorizationsRead, P.authorizationsWrite],
      mode: 'admin',
      subtitle: 'Visitantes autorizados por las unidades',
    },
  },
  {
    path: 'my/authorizations',
    loadComponent: () =>
      import('./authorizations/authorizations.component').then(
        (m) => m.AuthorizationsComponent
      ),
    title: 'Autorizar visitantes',
    data: {
      permission: P.authorizationsWriteOwn,
      mode: 'portal',
      subtitle: 'Avise a portería quién viene a visitarle',
    },
  },
  {
    path: 'complexes',
    loadChildren: () =>
      import('./complexes/complexes.routes').then((m) => m.default),
  },
  {
    path: 'units',
    loadChildren: () => import('./units/units.routes').then((m) => m.default),
  },
  {
    path: 'people',
    loadChildren: () => import('./people/people.routes').then((m) => m.default),
  },
  {
    path: 'my/units',
    loadComponent: () =>
      import('./my/my-units.component').then((m) => m.MyUnitsComponent),
    title: 'Mis propiedades',
    data: {
      permission: P.occupancyWriteOwn,
      mode: 'owner',
      subtitle: 'Sus unidades y quienes viven en ellas',
    },
  },
  {
    path: 'my/home',
    loadComponent: () =>
      import('./my/my-units.component').then((m) => m.MyUnitsComponent),
    title: 'Mi residencia',
    data: { permission: P.unitsReadOwn, mode: 'resident' },
  },
  {
    path: 'users',
    loadChildren: () => import('./users/users.routes').then((m) => m.default),
  },
  {
    path: 'roles',
    loadChildren: () => import('./roles/roles.routes').then((m) => m.default),
  },
  {
    path: 'audit',
    loadComponent: () =>
      import('./audit/audit.component').then((m) => m.AuditComponent),
    title: 'Bitácora',
    data: { permission: P.auditRead, subtitle: 'Quién cambió qué y cuándo' },
  },
  {
    path: 'settings/policies',
    loadComponent: () =>
      import('./settings/policies.component').then((m) => m.PoliciesComponent),
    title: 'Políticas',
    data: {
      permission: P.settingsRead,
      subtitle: 'Reglas de operación del conjunto',
    },
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./forbidden/forbidden.component').then(
        (m) => m.ForbiddenComponent
      ),
    title: 'Sin acceso',
    data: { public: true },
  },
  {
    // sin pantalla propia: el guard siempre redirige a la primera opción del menú
    path: '',
    pathMatch: 'full',
    canActivate: [homeRedirectGuard],
    children: [],
    data: { public: true },
  },
];

export default accessControlRoutes;
