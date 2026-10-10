/**
 * Tono del aviso. `primary` es el azul de la marca, para lo que no es ni bueno ni malo
 * (un recordatorio, un modo activo).
 */
export type ToastKind = 'success' | 'error' | 'warning' | 'info' | 'primary';

/**
 * Forma del aviso.
 *
 * - `corner`: tarjeta en la esquina superior derecha, apilada con las demás.
 * - `banner`: banda alargada centrada. Hay una sola a la vez: la nueva sustituye a la anterior.
 */
export type ToastPlacement = 'corner' | 'banner';

/** Dónde se centra la banda. */
export type BannerPosition = 'top' | 'bottom';

/** Botón del aviso. Al pulsarlo se ejecuta y el aviso se cierra. */
export interface ToastAction {
  label: string;
  run: () => void;
}

export interface NotificationOptions {
  message: string;
  title?: string;
  /** Quita el título por defecto del tono («Éxito», «Error»…). */
  omitTitle?: boolean;
  placement?: ToastPlacement;
  /** Solo para la banda. Por defecto, abajo. */
  position?: BannerPosition;
  action?: ToastAction;
  /** Milisegundos hasta cerrarse. `0` lo deja abierto hasta que se cierre a mano. */
  duration?: number;
}

/** Un aviso en pantalla, tal como lo pinta `ToastHostComponent`. */
export interface Toast {
  id: number;
  kind: ToastKind;
  title?: string;
  message: string;
  placement: ToastPlacement;
  position: BannerPosition;
  action?: ToastAction;
  duration: number;
  /** El cursor o el foco está encima: la cuenta atrás y la barra se detienen. */
  paused: boolean;
  /** Ya se cerró y está saliendo: la tarjeta anima su marcha antes de desaparecer. */
  leaving: boolean;
}
