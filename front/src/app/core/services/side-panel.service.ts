import { Dialog, DialogRef } from '@angular/cdk/dialog';
import { ComponentType, Overlay } from '@angular/cdk/overlay';
import { Injectable, inject } from '@angular/core';

export interface SidePanelConfig<D> {
  /** Nombre accesible del panel. */
  ariaLabel: string;
  /** Se inyecta en el componente con `DIALOG_DATA`. */
  data?: D;
}

/**
 * Abre un componente en el panel lateral derecho: el patrón estándar para
 * filtros y búsquedas de apoyo. El componente usa `<app-side-panel>` como
 * marco y devuelve su resultado con `DialogRef.close(resultado)`.
 */
@Injectable({
  providedIn: 'root',
})
export class SidePanelService {
  private readonly dialog = inject(Dialog);
  private readonly overlay = inject(Overlay);

  open<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config: SidePanelConfig<D>
  ): DialogRef<R, C> {
    return this.dialog.open<R, D, C>(component, {
      ariaLabel: config.ariaLabel,
      data: config.data,
      width: 'min(var(--app-side-panel-width), 100vw)',
      height: '100dvh',
      panelClass: 'app-side-panel',
      backdropClass: 'app-scrim',
      positionStrategy: this.overlay.position().global().right('0').top('0'),
    });
  }
}
