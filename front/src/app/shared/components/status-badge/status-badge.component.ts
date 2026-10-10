import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type StatusVariant = 'success' | 'neutral' | 'warning' | 'danger';

/**
 * Etiqueta de estado. El indicador cambia de forma además de color (relleno
 * para estados activos, hueco para neutros) para no depender solo del color.
 */
@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="dot" aria-hidden="true"></span><ng-content></ng-content>`,
  styleUrl: './status-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': '"status-badge--" + variant()',
  },
})
export class StatusBadgeComponent {
  readonly variant = input<StatusVariant>('neutral');
}
