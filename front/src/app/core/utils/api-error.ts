import { HttpErrorResponse } from '@angular/common/http';
import { ResponseModel } from '../types/response.model';

/**
 * Mensaje para mostrar al usuario a partir de un error HTTP. Usa el texto que envía el API
 * (errores de validación y de negocio); si no hay, el mensaje de respaldo.
 */
export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }
  if (error.status === 0) {
    return 'No hay conexión con el servidor. Inténtalo de nuevo.';
  }
  if (error.status === 403) {
    return 'No tiene permiso para esta acción.';
  }

  const body = error.error as ResponseModel<unknown> | null;
  if (body?.errors?.length) {
    return body.errors.map(stripFieldName).join(' ');
  }
  // un 5xx trae un texto genérico; el de respaldo dice qué se estaba haciendo
  return error.status < 500 && body?.message ? body.message : fallback;
}

/** "Email: El correo no es válido." → "El correo no es válido." */
function stripFieldName(message: string): string {
  return message.replace(/^[\w.$[\]]+:\s/, '');
}
