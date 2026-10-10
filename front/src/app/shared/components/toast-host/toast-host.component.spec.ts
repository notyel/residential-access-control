import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';

import { NotificationService } from '../../services/notifications/notification.service';
import { ToastHostComponent } from './toast-host.component';

describe('ToastHostComponent', () => {
  let fixture: ComponentFixture<ToastHostComponent>;
  let service: NotificationService;
  let host: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastHostComponent],
    }).compileComponents();

    service = TestBed.inject(NotificationService);
    fixture = TestBed.createComponent(ToastHostComponent);
    host = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => service.clear());

  it('renders corner toasts newest first, with their role', () => {
    service.success('Primero');
    service.error('Segundo');
    fixture.detectChanges();

    const toasts = Array.from(host.querySelectorAll('.nk-toast'));
    expect(
      toasts.map((toast) => toast.querySelector('.nk-toast__message')?.textContent?.trim())
    ).toEqual(['Segundo', 'Primero']);
    expect(toasts[0].getAttribute('role')).toBe('alert');
    expect(toasts[1].getAttribute('role')).toBe('status');
    expect(toasts[0].classList).toContain('nk-toast--error');
  });

  it('closes a toast with its close button, after its exit animation', fakeAsync(() => {
    service.info('Sincronizando');
    fixture.detectChanges();

    (host.querySelector('.nk-toast__close') as HTMLButtonElement).click();
    fixture.detectChanges();
    // Sigue en el DOM mientras anima su marcha…
    expect(host.querySelector('.nk-toast-slot--leave')).not.toBeNull();

    tick(NotificationService.LEAVE_DURATION_MS);
    fixture.detectChanges();
    expect(host.querySelector('.nk-toast')).toBeNull();
  }));

  it('runs the action and closes the banner', fakeAsync(() => {
    const retry = jasmine.createSpy('retry');
    service.banner('error', 'Sin conexión', {
      action: { label: 'Reintentar', run: retry },
      position: 'top',
    });
    fixture.detectChanges();

    const banner = host.querySelector('.nk-banner-zone--top .nk-banner') as HTMLElement;
    expect(banner).toBeTruthy();
    expect(host.querySelector('.nk-banner-zone--bottom .nk-banner')).toBeNull();

    (banner.querySelector('.nk-banner__action') as HTMLButtonElement).click();

    expect(retry).toHaveBeenCalled();
    tick(NotificationService.LEAVE_DURATION_MS);
    expect(service.toasts().length).toBe(0);
  }));

  it('pauses while the pointer is over the toast', () => {
    service.warning('Inventario bajo');
    fixture.detectChanges();
    const toast = host.querySelector('.nk-toast') as HTMLElement;

    toast.dispatchEvent(new MouseEvent('mouseenter'));
    fixture.detectChanges();
    expect(toast.classList).toContain('nk-toast--paused');

    toast.dispatchEvent(new MouseEvent('mouseleave'));
    fixture.detectChanges();
    expect(toast.classList).not.toContain('nk-toast--paused');
  });

  it('hides the progress bar of a toast that does not expire', () => {
    service.error({ message: 'Sin conexión', duration: 0 });
    fixture.detectChanges();

    expect(host.querySelector('.nk-toast__bar')).toBeNull();
  });
});
