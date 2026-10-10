import { Dialog, DialogRef } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { Injectable, inject } from '@angular/core';

/** Abre y cierra la búsqueda por comando (Ctrl K). */
@Injectable({
  providedIn: 'root',
})
export class CommandPaletteService {
  private readonly dialog = inject(Dialog);
  private readonly overlay = inject(Overlay);
  private dialogRef: DialogRef<unknown, unknown> | null = null;
  private opening = false;

  async open(): Promise<void> {
    if (this.dialogRef || this.opening) {
      return;
    }

    this.opening = true;
    try {
      const { CommandPaletteComponent } = await import(
        '../../shared/components/command-palette/command-palette.component'
      );

      const dialogRef = this.dialog.open<unknown>(CommandPaletteComponent, {
        ariaLabel: 'Búsqueda',
        width: 'min(640px, calc(100vw - 32px))',
        maxHeight: 'min(560px, calc(100dvh - 96px))',
        panelClass: 'app-command-palette',
        backdropClass: 'app-scrim',
        positionStrategy: this.overlay
          .position()
          .global()
          .centerHorizontally()
          .top('80px'),
      });
      this.dialogRef = dialogRef;
      dialogRef.closed.subscribe(() => (this.dialogRef = null));
    } finally {
      this.opening = false;
    }
  }

  close(): void {
    this.dialogRef?.close();
  }

  toggle(): void {
    if (this.dialogRef) {
      this.close();
    } else {
      void this.open();
    }
  }
}
