/** Iniciales para un avatar a partir de nombre y apellido. */
export function initials(firstName?: string, lastName?: string): string {
  const first = firstName?.trim().charAt(0) ?? '';
  const last = lastName?.trim().charAt(0) ?? '';
  return `${first}${last}`.toUpperCase() || '?';
}
