import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorMessage } from './api-error';

describe('apiErrorMessage', () => {
  const fallback = 'No se pudo guardar.';

  it('usa el mensaje de negocio que envía el API', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { success: false, message: 'Ya existe un usuario con ese correo.' },
    });
    expect(apiErrorMessage(error, fallback)).toBe(
      'Ya existe un usuario con ese correo.'
    );
  });

  it('une los errores de validación y les quita el nombre del campo', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        success: false,
        message: 'Hay datos que no son válidos.',
        errors: ['Email: El correo no tiene un formato válido.'],
      },
    });
    expect(apiErrorMessage(error, fallback)).toBe(
      'El correo no tiene un formato válido.'
    );
  });

  it('en un error del servidor usa el mensaje de respaldo', () => {
    const error = new HttpErrorResponse({
      status: 500,
      error: { success: false, message: 'Ocurrió un error inesperado.' },
    });
    expect(apiErrorMessage(error, fallback)).toBe(fallback);
  });

  it('distingue la falta de conexión', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(apiErrorMessage(error, fallback)).toContain('No hay conexión');
  });
});
