export interface RoleRef {
  id: string;
  name: string;
  /** Clave del rol de sistema (admin, guard, owner, resident, auditor); null en roles propios. */
  systemKey?: string | null;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  apartmentNumber: string;
  isActive?: boolean;
  isSuperAdmin?: boolean;
  complexId?: string | null;
  complexName?: string | null;
  roles: RoleRef[];
}

/** Usuario de la sesión: incluye sus permisos efectivos. */
export interface SessionUser extends User {
  permissions: string[];
}
