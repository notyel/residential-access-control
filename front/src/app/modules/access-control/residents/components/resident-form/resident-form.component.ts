import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormSectionComponent } from '../../../../../shared/components/form-section/form-section.component';

/**
 * Campos del residente, compartidos por las páginas de alta y edición.
 * Muestra el correo y la contraseña solo si el formulario los incluye.
 */
@Component({
  selector: 'app-resident-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    FormSectionComponent,
  ],
  templateUrl: './resident-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResidentFormComponent {
  readonly form = input.required<FormGroup>();
  readonly errorMessage = input('');
}
