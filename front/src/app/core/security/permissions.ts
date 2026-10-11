/**
 * Claves de permiso, iguales a las del catálogo del servidor
 * (backend/AccessControl.Application/Security/Permissions.cs). Aquí solo sirven para decidir
 * qué se muestra; quien autoriza cada operación es el servidor.
 */
export const P = {
  dashboardRead: 'dashboard.read',

  visitsRead: 'visits.read',
  visitsReadOwn: 'visits.read_own',

  accessRegister: 'access.register',
  accessException: 'access.exception',
  accessVoid: 'access.void',
  accessRead: 'access.read',
  accessReadOwn: 'access.read_own',
  incidentsWrite: 'incidents.write',

  reportsRead: 'reports.read',
  reportsExport: 'reports.export',

  authorizationsRead: 'authorizations.read',
  authorizationsWrite: 'authorizations.write',
  authorizationsWriteOwn: 'authorizations.write_own',

  vehiclesRead: 'vehicles.read',
  vehiclesWrite: 'vehicles.write',
  vehiclesWriteOwn: 'vehicles.write_own',

  parkingRead: 'parking.read',
  parkingReadOwn: 'parking.read_own',
  parkingWrite: 'parking.write',
  parkingAssign: 'parking.assign',
  parkingReserve: 'parking.reserve',

  peopleRead: 'people.read',
  peopleWrite: 'people.write',
  peopleViewContact: 'people.view_contact',
  peopleViewIdentity: 'people.view_identity',

  unitsRead: 'units.read',
  unitsReadOwn: 'units.read_own',
  unitsWrite: 'units.write',
  ownershipWrite: 'ownership.write',
  occupancyWrite: 'occupancy.write',
  occupancyWriteOwn: 'occupancy.write_own',

  usersRead: 'users.read',
  usersManage: 'users.manage',

  rolesRead: 'roles.read',
  rolesManage: 'roles.manage',

  auditRead: 'audit.read',

  settingsRead: 'settings.read',
  settingsManage: 'settings.manage',
} as const;

export type PermissionKey = (typeof P)[keyof typeof P];

/** Un permiso o varios; con varios basta tener uno. */
export type PermissionRule = string | readonly string[];

const OWN_SUFFIX = '_own';

/** Misma regla del servidor: quien puede sobre todo el conjunto puede también sobre lo propio. */
export function grants(owned: ReadonlySet<string>, permission: string): boolean {
  return (
    owned.has(permission) ||
    (permission.endsWith(OWN_SUFFIX) &&
      owned.has(permission.slice(0, -OWN_SUFFIX.length)))
  );
}
