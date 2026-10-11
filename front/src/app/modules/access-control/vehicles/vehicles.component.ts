import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ChevronRight, LucideAngularModule, Plus, Search } from 'lucide-angular';
import { Observable, Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { CatalogItem, Unit } from '../../../core/models/unit.model';
import {
  VEHICLE_CLASS_LABELS,
  VEHICLE_STATUS_LABELS,
  VEHICLE_STATUS_VARIANTS,
  Vehicle,
  VehicleClass,
  VehicleStatus,
  normalizePlate,
  vehicleDescription,
} from '../../../core/models/vehicle.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { SidePanelService } from '../../../core/services/side-panel.service';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { VehiclesApiService } from '../../../core/services/vehicles-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../shared/components/paginator/paginator.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import {
  UnitPickerComponent,
  UnitPickerData,
} from '../../../shared/components/unit-picker/unit-picker.component';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';
import { NotificationService } from '../../../shared/services/notifications/notification.service';
import {
  VehicleDetailComponent,
  VehicleDetailResult,
} from './vehicle-detail.component';

/** create: vehículo nuevo con su autorización. edit: solo sus datos. reassign: solo unidad y responsable. */
type FormMode = 'create' | 'edit' | 'reassign';

interface Option {
  id: string;
  label: string;
}

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    StatusBadgeComponent,
    PageActionsDirective,
  ],
  templateUrl: './vehicles.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehiclesComponent implements OnInit {
  private readonly api = inject(VehiclesApiService);
  private readonly unitsApi = inject(UnitsApiService);
  private readonly sidePanel = inject(SidePanelService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly canWrite = inject(AuthService).hasPermission(P.vehiclesWrite);

  readonly vehicles = signal<Vehicle[]>([]);
  readonly pending = signal<Vehicle[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly type = signal('');
  readonly vehicleClass = signal('');
  readonly status = signal('');

  readonly types = signal<CatalogItem[]>([]);
  readonly formMode = signal<FormMode | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly unit = signal<Option | null>(null);
  readonly responsibles = signal<Option[]>([]);
  readonly busy = signal(false);
  readonly formError = signal('');
  readonly actionError = signal('');

  readonly form = inject(FormBuilder).nonNullable.group({
    plate: [''],
    type: ['automovil', Validators.required],
    brand: [''],
    color: [''],
    model: [''],
    notes: [''],
    responsiblePersonId: [''],
  });

  private readonly searches = new Subject<string>();

  readonly Class = VehicleClass;
  readonly Status = VehicleStatus;
  readonly classLabels = VEHICLE_CLASS_LABELS;
  readonly statusLabels = VEHICLE_STATUS_LABELS;
  readonly statusVariants = VEHICLE_STATUS_VARIANTS;
  readonly statuses = [
    VehicleStatus.Active,
    VehicleStatus.Pending,
    VehicleStatus.Suspended,
    VehicleStatus.Revoked,
  ];
  readonly describe = vehicleDescription;
  readonly PlusIcon = Plus;
  readonly SearchIcon = Search;
  readonly DetailIcon = ChevronRight;

  ngOnInit(): void {
    this.searches
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.search.set(term);
        this.pageIndex.set(0);
        this.load();
      });
    this.unitsApi.catalog('vehicle-type').subscribe((types) => this.types.set(types));
    this.load();
  }

  typeLabel(code: string): string {
    return this.types().find((type) => type.code === code)?.label ?? code;
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .search({
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        search: this.search(),
        type: this.type() || undefined,
        class: this.vehicleClass() === '' ? null : Number(this.vehicleClass()),
        status: this.status() === '' ? null : Number(this.status()),
      })
      .subscribe({
        next: (page) => {
          this.vehicles.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
    if (this.canWrite) {
      this.api.pending().subscribe((pending) => this.pending.set(pending));
    }
  }

  onSearch(event: Event): void {
    this.searches.next((event.target as HTMLInputElement).value.trim());
  }

  setFilter(filter: 'type' | 'vehicleClass' | 'status', event: Event): void {
    this[filter].set((event.target as HTMLSelectElement).value);
    this.pageIndex.set(0);
    this.load();
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.load();
  }

  openDetail(vehicle: Vehicle): void {
    this.sidePanel
      .open<VehicleDetailResult, string, VehicleDetailComponent>(VehicleDetailComponent, {
        ariaLabel: `Vehículo ${vehicle.plate}`,
        data: vehicle.id,
      })
      .closed.subscribe((result) => {
        if (!result) {
          return;
        }
        this.load();
        if (result !== 'changed') {
          this.openForm('edit' in result ? 'edit' : 'reassign', vehicle);
        }
      });
  }

  openForm(mode: FormMode, vehicle?: Vehicle): void {
    this.formMode.set(mode);
    this.editingId.set(vehicle?.id ?? null);
    this.formError.set('');
    this.unit.set(null);
    this.responsibles.set([]);
    this.form.reset({
      plate: vehicle?.hasPlate ? vehicle.plate : '',
      type: vehicle?.type ?? 'automovil',
      brand: vehicle?.brand ?? '',
      color: vehicle?.color ?? '',
      model: vehicle?.model ?? '',
      notes: vehicle?.notes ?? '',
      responsiblePersonId: '',
    });
  }

  cancelForm(): void {
    this.formMode.set(null);
  }

  /** La placa se muestra como quedará guardada. */
  onPlate(): void {
    const control = this.form.controls.plate;
    control.setValue(normalizePlate(control.value), { emitEvent: false });
  }

  pickUnit(): void {
    this.sidePanel
      .open<Unit, UnitPickerData, UnitPickerComponent>(UnitPickerComponent, {
        ariaLabel: 'Buscar unidad',
        data: { selectedId: this.unit()?.id ?? null },
      })
      .closed.subscribe((unit) => {
        if (!unit) {
          return;
        }
        this.unit.set({ id: unit.id, label: unit.label });
        this.form.controls.responsiblePersonId.setValue('');
        this.unitsApi.unitSummary(unit.id).subscribe((summary) => {
          const people = new Map<string, string>();
          for (const link of [...summary.owners, ...summary.occupants]) {
            people.set(link.personId, link.personName);
          }
          this.responsibles.set([...people].map(([id, label]) => ({ id, label })));
        });
      });
  }

  save(): void {
    const mode = this.formMode();
    if (!mode || this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const unit = this.unit();
    if (mode !== 'edit' && (!unit || !value.responsiblePersonId)) {
      this.formError.set('Indique la unidad y la persona que responde por el vehículo.');
      return;
    }
    const data = {
      plate: value.plate || null,
      type: value.type,
      brand: value.brand || null,
      color: value.color || null,
      model: value.model || null,
      notes: value.notes || null,
    };
    const id = this.editingId();
    const request: Observable<Vehicle> =
      mode === 'create'
        ? this.api.create({ ...data, unitId: unit!.id, responsiblePersonId: value.responsiblePersonId })
        : mode === 'edit'
        ? this.api.update(id!, data)
        : this.api.reassign(id!, unit!.id, value.responsiblePersonId);

    this.busy.set(true);
    this.formError.set('');
    request.subscribe({
      next: (vehicle) => {
        this.busy.set(false);
        this.formMode.set(null);
        this.notifications.success(
          mode === 'create'
            ? `Vehículo ${vehicle.plate} registrado.`
            : mode === 'edit'
            ? `Vehículo ${vehicle.plate} actualizado.`
            : `Vehículo ${vehicle.plate} reasignado a ${vehicle.unitLabel}.`
        );
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.formError.set(apiErrorMessage(err, 'No se pudo guardar el vehículo.'));
      },
    });
  }

  decide(vehicle: Vehicle, approve: boolean): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');
    this.api
      .decide(vehicle.id, approve, approve ? undefined : 'Rechazada por la administración')
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.notifications.success(
            `Solicitud de ${vehicle.plate} ${approve ? 'aprobada' : 'rechazada'}.`
          );
          this.load();
        },
        error: (err) => {
          this.busy.set(false);
          this.actionError.set(apiErrorMessage(err, 'No se pudo resolver la solicitud.'));
        },
      });
  }
}
