import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { LucideAngularModule, X } from 'lucide-angular';

/**
 * Marco del panel lateral derecho: cabecera con título y cierre, cuerpo con
 * scroll interno y pie fijo para las acciones (`sidePanelFooter`).
 * Se usa como raíz de los componentes abiertos con `SidePanelService`.
 */
@Component({
  selector: 'app-side-panel',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './side-panel.component.html',
  styleUrl: './side-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidePanelComponent {
  private readonly dialogRef = inject(DialogRef);

  readonly title = input.required<string>();

  readonly CloseIcon = X;

  close(): void {
    this.dialogRef.close();
  }
}
