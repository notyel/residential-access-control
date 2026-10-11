export interface RoleRef {
  id: string;
  name: string;
  /** Clave del rol de sistema (admin, guard, owner, resident, auditor); null en roles propios. */
  systemKey?: string | null;
}

/** Invited: aún no define su contraseña. Disabled: no puede iniciar sesión. */
export type UserStatus = 'Invited' | 'Active' | 'Disabled';

export const USER_STATUS_LABELS: Record<UserStatus, string> = {
  Active: 'Activos',
  Invited: 'Invitados',
  Disabled: 'Desactivados',
};

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  apartmentNumber: string;
  isActive?: boolean;
  status: UserStatus;
  isSuperAdmin?: boolean;
  complexId?: string | null;
  complexName?: string | null;
  roles: RoleRef[];
}

/** Usuario de la sesión: incluye sus permisos efectivos. */
export interface SessionUser extends User {
  permissions: string[];
}
