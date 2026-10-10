import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Check, LucideAngularModule } from 'lucide-angular';
import { map } from 'rxjs';
import {
  COMPLEX_TYPE_LABELS,
  ComplexType,
} from '../../../../../core/models/complex.model';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { FormSectionComponent } from '../../../../../shared/components/form-section/form-section.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { ComplexesService } from '../../services/complexes.service';

const COMPLEXES_URL = '/access-control/complexes';

/** Alta y edición de un conjunto. En el alta se pide además su primer administrador. */
@Component({
  selector: 'app-complex-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideAngularModule,
    FormSectionComponent,
    PageActionsDirective,
  ],
  templateUrl: './complex-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComplexFormComponent implements OnInit {
  private readonly complexesService = inject(ComplexesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly complexId = this.route.snapshot.paramMap.get('id');
  readonly isNew = this.complexId === null;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    type: [ComplexType.Conjunto, Validators.required],
    address: ['', Validators.maxLength(250)],
    timeZoneId: ['America/Bogota', Validators.required],
    admin: this.fb.nonNullable.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    }),
  });

  readonly types = [
    ComplexType.Conjunto,
    ComplexType.Edificio,
    ComplexType.PropiedadConPorteria,
  ];
  readonly typeLabels = COMPLEX_TYPE_LABELS;
  // Zonas horarias más usadas; el servidor valida cualquier ID IANA
  readonly timeZones = [
    'America/Bogota',
    'America/Mexico_City',
    'America/Lima',
    'America/Guayaquil',
    'America/Panama',
    'America/Caracas',
    'America/Santiago',
    'America/Argentina/Buenos_Aires',
    'America/New_York',
    'Europe/Madrid',
  ];

  readonly errorMessage = signal('');
  readonly saving = signal(false);
  readonly SaveIcon = Check;
  readonly cancelUrl = COMPLEXES_URL;

  private readonly formValid = toSignal(
    this.form.statusChanges.pipe(map((status) => status === 'VALID')),
    { initialValue: this.form.valid }
  );
  readonly canSave = computed(() => this.formValid() && !this.saving());

  ngOnInit(): void {
    if (this.complexId) {
      // el administrador solo se define al crear el conjunto
      this.form.controls.admin.disable();
      this.complexesService.getById(this.complexId).subscribe({
        next: (complex) =>
          this.form.patchValue({ ...complex, address: complex.address ?? '' }),
        error: () => this.errorMessage.set('No se pudo cargar el conjunto.'),
      });
    }
  }

  onSubmit(): void {
    if (!this.canSave()) {
      this.form.markAllAsTouched();
      return;
    }

    const { admin, ...complex } = this.form.getRawValue();
    const request = this.complexId
      ? this.complexesService.update(this.complexId, complex)
      : this.complexesService.create({
          ...complex,
          adminEmail: admin.email,
          adminFirstName: admin.firstName,
          adminLastName: admin.lastName,
          adminPassword: admin.password,
        });

    this.saving.set(true);
    this.errorMessage.set('');

    request.subscribe({
      next: () => this.router.navigate([COMPLEXES_URL]),
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(
          apiErrorMessage(err, 'No se pudo guardar el conjunto.')
        );
      },
    });
  }
}
