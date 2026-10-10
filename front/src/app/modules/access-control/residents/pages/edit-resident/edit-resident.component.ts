import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Check, LucideAngularModule } from 'lucide-angular';
import { map } from 'rxjs';
import { ResidentsService } from '../../services/residents.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { ResidentFormComponent } from '../../components/resident-form/resident-form.component';

@Component({
  selector: 'app-edit-resident',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    PageActionsDirective,
    ResidentFormComponent,
  ],
  templateUrl: './edit-resident.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditResidentComponent implements OnInit {
  private readonly residentsService = inject(ResidentsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly residentForm = inject(FormBuilder).nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    apartmentNumber: ['', Validators.required],
  });

  readonly errorMessage = signal('');
  readonly saving = signal(false);
  private residentId = '';

  private readonly formValid = toSignal(
    this.residentForm.statusChanges.pipe(map((status) => status === 'VALID')),
    { initialValue: this.residentForm.valid }
  );
  readonly canSave = computed(() => this.formValid() && !this.saving());

  readonly SaveIcon = Check;

  ngOnInit(): void {
    if (!this.authService.hasRole('Admin')) {
      this.residentForm.disable();
    }

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.residentId = id;
      this.residentsService.getResident(id).subscribe({
        next: (resident) => this.residentForm.patchValue(resident),
        error: () =>
          this.errorMessage.set('No se pudo cargar el residente.'),
      });
    }
  }

  onSubmit(): void {
    if (!this.canSave()) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');

    this.residentsService
      .updateResident(
        this.residentId,
        this.residentForm.getRawValue()
      )
      .subscribe({
        next: () => this.router.navigate(['/access-control/residents']),
        error: (err) => {
          this.saving.set(false);
          this.errorMessage.set('Error al actualizar el residente');
          console.error(err);
        },
      });
  }
}
