import { Injectable, signal } from '@angular/core';

import {
  NotificationOptions,
  Toast,
  ToastKind,
} from './notification.model';

export * from './notification.model';

interface Countdown {
  handle: ReturnType<typeof setTimeout>;
  startedAt: number;
  remaining: number;
}

/**
 * Avisos del sistema.
 *
 * Los avisos son una lista en una señal y `ToastHostComponent` (en `app.component.html`) la
 * pinta; nadie más necesita saber cómo se ven ni dónde se colocan.
 *
 * La firma de `success`, `error`, `warning` e `info` es la clásica de los toasts, así que las
 * pantallas solo inyectan el servicio y lo llaman. Lo nuevo va en las opciones: la banda central
 * (`placement: "banner"`), un botón de acción y la duración.
 *
 *   this.notifications.success('Residente guardado');
 *   this.notifications.error({ message: 'Sin conexión', placement: 'banner',
 *     action: { label: 'Reintentar', run: () => this.load() } });
 */
@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  /** Duración estándar de una tarjeta. */
  static readonly DEFAULT_DURATION_MS = 5000;
  /** Más tarjetas tapan la pantalla: la más antigua deja sitio a la nueva. */
  static readonly MAX_CORNER_TOASTS = 4;
  /** Lo que tarda la animación de salida; el aviso sigue en pantalla mientras tanto. */
  static readonly LEAVE_DURATION_MS = 300;

  private static readonly DEFAULT_TITLES: Record<ToastKind, string> = {
    success: 'Éxito',
    error: 'Error',
    warning: 'Advertencia',
    info: 'Información',
    primary: 'Aviso',
  };

  private readonly items = signal<Toast[]>([]);
  private readonly countdowns = new Map<number, Countdown>();
  private nextId = 1;

  /** Los avisos en pantalla, del más antiguo al más reciente. */
  readonly toasts = this.items.asReadonly();

  success(message: string, title?: string): void;
  success(options: NotificationOptions): void;
  success(messageOrOptions: string | NotificationOptions, title?: string): void {
    this.show('success', this.normalize(messageOrOptions, title));
  }

  error(message: string, title?: string): void;
  error(options: NotificationOptions): void;
  error(messageOrOptions: string | NotificationOptions, title?: string): void {
    this.show('error', this.normalize(messageOrOptions, title));
  }

  warning(message: string, title?: string): void;
  warning(options: NotificationOptions): void;
  warning(messageOrOptions: string | NotificationOptions, title?: string): void {
    this.show('warning', this.normalize(messageOrOptions, title));
  }

  info(message: string, title?: string): void;
  info(options: NotificationOptions): void;
  info(messageOrOptions: string | NotificationOptions, title?: string): void {
    this.show('info', this.normalize(messageOrOptions, title));
  }

  /** Banda central: para lo que afecta a toda la pantalla, no a una acción concreta. */
  banner(kind: ToastKind, message: string, options: Omit<NotificationOptions, 'message' | 'placement'> = {}): void {
    this.show(kind, { ...options, message, placement: 'banner' });
  }

  /**
   * Muestra un aviso de cualquier tono. Devuelve su id, o `null` si ya había uno igual en pantalla:
   * repetirlo no informa de nada (lo mismo que hacía `preventDuplicates`).
   */
  show(kind: ToastKind, options: NotificationOptions): number | null {
    const placement = options.placement ?? 'corner';
    const title = this.resolveTitle(kind, options);
    const message = options.message ?? '';

    const duplicated = this.items().some(
      (toast) =>
        !toast.leaving &&
        toast.kind === kind &&
        toast.placement === placement &&
        toast.title === title &&
        toast.message === message,
    );
    if (duplicated) return null;

    const toast: Toast = {
      id: this.nextId++,
      kind,
      title,
      message,
      placement,
      position: options.position ?? 'bottom',
      action: options.action,
      duration: Math.max(0, options.duration ?? NotificationService.DEFAULT_DURATION_MS),
      paused: false,
      leaving: false,
    };

    this.makeRoomFor(toast);
    this.items.update((list) => [...list, toast]);
    this.startCountdown(toast.id, toast.duration);
    return toast.id;
  }

  /**
   * Cierra un aviso. Marca el aviso como saliendo para que su animación de marcha se vea
   * entera; la lista lo olvida cuando la animación termina.
   */
  dismiss(id: number): void {
    const toast = this.items().find((item) => item.id === id);
    if (!toast || toast.leaving) return;

    this.stopCountdown(id);
    this.setLeaving(id, true);
    setTimeout(() => this.forget(id), NotificationService.LEAVE_DURATION_MS);
  }

  /** Quita todo de golpe, sin animación (al cerrar sesión, por ejemplo). */
  clear(): void {
    for (const id of this.countdowns.keys()) this.stopCountdown(id);
    this.items.set([]);
  }

  /** Detiene la cuenta atrás mientras se lee. */
  pause(id: number): void {
    const countdown = this.countdowns.get(id);
    if (!countdown) return;

    clearTimeout(countdown.handle);
    countdown.remaining = Math.max(0, countdown.remaining - (Date.now() - countdown.startedAt));
    this.setPaused(id, true);
  }

  /** Sigue la cuenta atrás donde se quedó. */
  resume(id: number): void {
    const countdown = this.countdowns.get(id);
    if (!countdown || !this.items().find((toast) => toast.id === id)?.paused) return;

    this.countdowns.delete(id);
    this.startCountdown(id, countdown.remaining);
    this.setPaused(id, false);
  }

  private forget(id: number): void {
    this.items.update((list) => list.filter((toast) => toast.id !== id));
  }

  private makeRoomFor(toast: Toast): void {
    const sameKind = this.items().filter(
      (item) => item.placement === toast.placement && !item.leaving,
    );
    const limit = toast.placement === 'banner' ? 1 : NotificationService.MAX_CORNER_TOASTS;
    const excess = sameKind.length - limit + 1;

    for (const old of sameKind.slice(0, Math.max(0, excess))) this.dismiss(old.id);
  }

  private startCountdown(id: number, ms: number): void {
    if (ms <= 0) return;
    this.countdowns.set(id, {
      handle: setTimeout(() => this.dismiss(id), ms),
      startedAt: Date.now(),
      remaining: ms,
    });
  }

  private stopCountdown(id: number): void {
    const countdown = this.countdowns.get(id);
    if (countdown) clearTimeout(countdown.handle);
    this.countdowns.delete(id);
  }

  private setPaused(id: number, paused: boolean): void {
    this.items.update((list) => list.map((toast) => (toast.id === id ? { ...toast, paused } : toast)));
  }

  private setLeaving(id: number, leaving: boolean): void {
    this.items.update((list) => list.map((toast) => (toast.id === id ? { ...toast, leaving } : toast)));
  }

  /**
   * La tarjeta lleva el título del tono si no se da otro; la banda solo el que se le pase, porque
   * es una frase y un «Advertencia» delante no añade nada.
   */
  private resolveTitle(kind: ToastKind, options: NotificationOptions): string | undefined {
    if (options.omitTitle) return undefined;
    if (options.title) return options.title;
    return (options.placement ?? 'corner') === 'corner'
      ? NotificationService.DEFAULT_TITLES[kind]
      : undefined;
  }

  private normalize(messageOrOptions: string | NotificationOptions, title?: string): NotificationOptions {
    if (typeof messageOrOptions === 'object' && messageOrOptions !== null) return messageOrOptions;
    // Hay llamadas que pasan `err.error?.message` sin comprobarlo: un aviso vacío antes que un fallo.
    return { message: String(messageOrOptions ?? ''), title };
  }
}
