import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Inbox, LucideAngularModule, TriangleAlert } from 'lucide-angular';

/**
 * Contenedor de una tabla de datos: llena el alto disponible, con barra
 * superior (`tableToolbar`), cuerpo con scroll interno y pie (`tableFooter`).
 * Resuelve los estados de carga, vacío y error.
 *
 * ```html
 * <app-data-table [loading]="loading()" [empty]="!rows().length">
 *   <ng-container tableToolbar>…</ng-container>
 *   <table class="app-table">…</table>
 *   <app-paginator tableFooter … />
 * </app-data-table>
 * ```
 */
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [MatButtonModule, LucideAngularModule],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataTableComponent {
  readonly loading = input(false);
  readonly empty = input(false);
  readonly error = input<string | null>(null);
  readonly emptyTitle = input('No hay registros');
  readonly emptyMessage = input('');

  readonly retry = output<void>();

  readonly EmptyIcon = Inbox;
  readonly ErrorIcon = TriangleAlert;
  readonly skeletonRows = [1, 2, 3, 4, 5, 6];
}
