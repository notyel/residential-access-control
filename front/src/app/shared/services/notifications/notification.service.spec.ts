import { TestBed, fakeAsync, tick } from '@angular/core/testing';

import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationService);
  });

  afterEach(() => service.clear());

  /** Los avisos en pantalla, sin los que ya están saliendo. */
  const visible = () => service.toasts().filter((toast) => !toast.leaving);

  it('keeps the old signature and the default title of each kind', () => {
    service.success('Residente guardado');
    service.error('Falló', 'No se pudo guardar');

    const [success, error] = visible();
    expect(success).toEqual(
      jasmine.objectContaining({
        kind: 'success',
        title: 'Éxito',
        message: 'Residente guardado',
        placement: 'corner',
      })
    );
    expect(error.title).toBe('No se pudo guardar');
  });

  it('accepts options and can omit the title', () => {
    service.warning({ message: 'Visita vencida', omitTitle: true });

    expect(visible()[0].title).toBeUndefined();
  });

  it('closes itself after the default duration', fakeAsync(() => {
    service.info('Sincronizando');

    tick(NotificationService.DEFAULT_DURATION_MS - 1);
    expect(visible().length).toBe(1);

    // La cuenta atrás termina y el aviso anima su marcha antes de desaparecer.
    tick(1 + NotificationService.LEAVE_DURATION_MS);
    expect(service.toasts().length).toBe(0);
  }));

  it('stays open when the duration is zero', fakeAsync(() => {
    service.error({ message: 'Sin conexión', duration: 0 });

    tick(60_000);
    expect(visible().length).toBe(1);
  }));

  it('ignores a toast identical to one already on screen', () => {
    service.error('Falló');
    const id = service.show('error', { message: 'Falló', title: 'Error' });

    expect(id).toBeNull();
    expect(visible().length).toBe(1);
  });

  it('drops the oldest corner toast beyond the limit', () => {
    for (let i = 1; i <= NotificationService.MAX_CORNER_TOASTS + 1; i++) {
      service.info(`Aviso ${i}`);
    }

    const messages = visible().map((toast) => toast.message);
    expect(messages.length).toBe(NotificationService.MAX_CORNER_TOASTS);
    expect(messages).not.toContain('Aviso 1');
  });

  it('keeps a single banner: the new one replaces the previous', () => {
    service.banner('warning', 'La sesión se cierra en 10 minutos');
    service.banner('info', 'Hay una versión nueva');
    service.success('Guardado');

    const banners = visible().filter((toast) => toast.placement === 'banner');
    expect(banners.length).toBe(1);
    expect(banners[0]).toEqual(
      jasmine.objectContaining({
        kind: 'info',
        position: 'bottom',
        title: undefined,
      })
    );
    expect(visible().length).toBe(2);
  });

  it('pauses the countdown and resumes with the time that was left', fakeAsync(() => {
    const id = service.show('success', { message: 'Guardado', duration: 1000 })!;

    tick(400);
    service.pause(id);
    expect(visible()[0].paused).toBeTrue();

    tick(5000);
    expect(visible().length).toBe(1);

    service.resume(id);
    expect(visible()[0].paused).toBeFalse();
    tick(599);
    expect(visible().length).toBe(1);
    tick(1 + NotificationService.LEAVE_DURATION_MS);
    expect(service.toasts().length).toBe(0);
  }));

  it('keeps a dismissed toast on screen until its exit animation ends', fakeAsync(() => {
    const id = service.show('info', { message: 'Sincronizando' })!;

    service.dismiss(id);
    expect(service.toasts()[0].leaving).toBeTrue();

    tick(NotificationService.LEAVE_DURATION_MS);
    expect(service.toasts().length).toBe(0);
  }));
});
