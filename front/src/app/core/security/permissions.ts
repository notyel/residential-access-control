/**
 * Claves de permiso, iguales a las del catálogo del servidor
 * (backend/AccessControl.Application/Security/Permissions.cs). Aquí solo sirven para decidir
 * qué se muestra; quien autoriza cada operación es el servidor.
 */
export const P = {
  dashboardRead: 'dashboard.read',

  visitsRead: 'visits.read',
  visitsReadOwn: 'visits.read_own',
  visitsRegister: 'visits.register',
  visitsUpdate: 'visits.update',
  visitsCheckout: 'visits.checkout',

  peopleRead: 'people.read',
  peopleWrite: 'people.write',

  residencesRead: 'residences.read',

  usersRead: 'users.read',
  usersManage: 'users.manage',

  rolesRead: 'roles.read',
  rolesManage: 'roles.manage',
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
