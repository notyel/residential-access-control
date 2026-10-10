import { ChangeDetectionStrategy, Component, input } from '@angular/core';

let nextId = 0;

/** Sección de formulario: tarjeta con título y número de paso opcional. */
@Component({
  selector: 'app-form-section',
  standalone: true,
  templateUrl: './form-section.component.html',
  styleUrl: './form-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSectionComponent {
  readonly title = input.required<string>();
  readonly step = input<number | null>(null);

  readonly titleId = `form-section-${nextId++}`;
}
