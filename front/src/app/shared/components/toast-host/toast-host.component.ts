import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

import { NotificationService, Toast } from '../../services/notifications/notification.service';

/**
 * Los avisos del sistema: la pila de tarjetas arriba a la derecha y la banda centrada.
 *
 * Va una sola vez, en `app.component.html`, y pinta lo que tenga `NotificationService`.
 *
 * El cursor o el foco sobre un aviso detiene su cuenta atrás; la barra inferior se detiene con
 * ella. La animación de marcha va por clase (`--leave`): el servicio mantiene el aviso en la
 * lista mientras corre, y lo olvida al terminar.
 */
@Component({
  selector: 'app-toast-host',
  standalone: true,
  imports: [NgTemplateOutlet],
  templateUrl: './toast-host.component.html',
  styleUrl: './toast-host.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastHostComponent {
  private readonly notifications = inject(NotificationService);

  /** La más reciente arriba. */
  readonly cornerToasts = computed(() =>
    this.notifications
      .toasts()
      .filter((toast) => toast.placement === 'corner')
      .reverse(),
  );
  readonly topBanners = computed(() => this.bannersAt('top'));
  readonly bottomBanners = computed(() => this.bannersAt('bottom'));

  /** Un error se anuncia en cuanto aparece; el resto espera a que el lector termine. */
  roleOf(toast: Toast): 'alert' | 'status' {
    return toast.kind === 'error' ? 'alert' : 'status';
  }

  dismiss(toast: Toast): void {
    this.notifications.dismiss(toast.id);
  }

  runAction(toast: Toast): void {
    toast.action?.run();
    this.notifications.dismiss(toast.id);
  }

  pause(toast: Toast): void {
    this.notifications.pause(toast.id);
  }

  resume(toast: Toast, event?: FocusEvent): void {
    // El foco que pasa de la cruz al botón de acción sigue dentro del aviso.
    const next = event?.relatedTarget as Node | null;
    if (next && (event?.currentTarget as HTMLElement).contains(next)) return;
    this.notifications.resume(toast.id);
  }

  private bannersAt(position: Toast['position']): Toast[] {
    return this.notifications
      .toasts()
      .filter((toast) => toast.placement === 'banner' && toast.position === position);
  }
}
