import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin } from 'rxjs';
import { CatalogItem, MyUnit } from '../../../core/models/unit.model';
import {
  VEHICLE_STATUS_LABELS,
  VEHICLE_STATUS_VARIANTS,
  Vehicle,
  VehicleStatus,
  normalizePlate,
  vehicleDescription,
} from '../../../core/models/vehicle.model';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { VehiclesApiService } from '../../../core/services/vehicles-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

/**
 * "Mis vehículos": los de las unidades del usuario. El registro depende de la política del
 * conjunto (no / con aprobación / directo); el servidor es quien la aplica.
 */
@Component({
  selector: 'app-my-vehicles',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    StatusBadgeComponent,
  ],
  templateUrl: './my-vehicles.component.html',
  styleUrl: '../units/pages/unit-detail/unit-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyVehiclesComponent implements OnInit {
  private readonly api = inject(VehiclesApiService);
  private readonly unitsApi = inject(UnitsApiService);

  readonly vehicles = signal<Vehicle[]>([]);
  readonly units = signal<MyUnit[]>([]);
  readonly types = signal<CatalogItem[]>([]);
  /** no, approval o direct. */
  readonly policy = signal('no');
  readonly loading = signal(true);
  readonly adding = signal(false);
  readonly busy = signal(false);
  readonly errorMessage = signal('');
  readonly notice = signal('');

  readonly form = inject(FormBuilder).nonNullable.group({
    unitId: ['', Validators.required],
    plate: [''],
    type: ['automovil', Validators.required],
    brand: [''],
    color: [''],
  });

  readonly Status = VehicleStatus;
  readonly statusLabels = VEHICLE_STATUS_LABELS;
  readonly statusVariants = VEHICLE_STATUS_VARIANTS;
  readonly describe = vehicleDescription;

  ngOnInit(): void {
    this.load();
  }

  typeLabel(code: string): string {
    return this.types().find((type) => type.code === code)?.label ?? code;
  }

  load(): void {
    forkJoin({
      mine: this.api.mine(),
      units: this.unitsApi.myUnits(),
      types: this.unitsApi.catalog('vehicle-type'),
    }).subscribe({
      next: ({ mine, units, types }) => {
        this.vehicles.set(mine.vehicles);
        this.policy.set(mine.policy);
        this.units.set(units);
        this.types.set(types);
        this.loading.set(false);
        if (units.length === 1) {
          this.form.controls.unitId.setValue(units[0].id);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar la información.'));
      },
    });
  }

  open(): void {
    this.adding.set(true);
    this.errorMessage.set('');
    this.notice.set('');
  }

  onPlate(): void {
    const control = this.form.controls.plate;
    control.setValue(normalizePlate(control.value), { emitEvent: false });
  }

  add(): void {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.errorMessage.set('');
    this.api
      .createMine({
        unitId: value.unitId,
        plate: value.plate || null,
        type: value.type,
        brand: value.brand || null,
        color: value.color || null,
      })
      .subscribe({
        next: ({ message }) => {
          this.busy.set(false);
          this.adding.set(false);
          this.notice.set(message);
          this.form.patchValue({ plate: '', brand: '', color: '' });
          this.load();
        },
        error: (err) => {
          this.busy.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar el vehículo.'));
        },
      });
  }

  remove(vehicle: Vehicle): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.errorMessage.set('');
    this.api.revokeMine(vehicle.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.notice.set(`Vehículo ${vehicle.plate} retirado.`);
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo retirar el vehículo.'));
      },
    });
  }
}
