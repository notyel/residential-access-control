import { DatePipe } from '@angular/common';
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
import {
  Ban,
  CircleCheck,
  Clock,
  LucideAngularModule,
  LucideIconData,
  Plus,
  SquareParking,
} from 'lucide-angular';
import { forkJoin, fromEvent, interval, merge } from 'rxjs';
import { filter } from 'rxjs/operators';
import {
  Availability,
  PARKING_KIND_LABELS,
  PARKING_STATE_LABELS,
  PARKING_TYPE_LABELS,
  ParkingSpace,
  ParkingState,
  ParkingType,
  ParkingVehicleKind,
} from '../../../core/models/parking.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { ParkingApiService } from '../../../core/services/parking-api.service';
import { SidePanelService } from '../../../core/services/side-panel.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';
import { NotificationService } from '../../../shared/services/notifications/notification.service';
import { ParkingDetailComponent } from './parking-detail.component';

const REFRESH_MS = 30_000;

const STATE_ICONS: Record<ParkingState, LucideIconData> = {
  [ParkingState.Available]: CircleCheck,
  [ParkingState.Occupied]: SquareParking,
  [ParkingState.Reserved]: Clock,
  [ParkingState.OutOfService]: Ban,
};

/**
 * Parqueaderos y su disponibilidad. Los totales y el estado de cada espacio los calcula el
 * servidor de las asignaciones vigentes; aquí no se guarda ni se cuenta nada. La pantalla se
 * actualiza sola cada 30 segundos y al volver a la pestaña.
 */
@Component({
  selector: 'app-parking',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideAngularModule,
    PageActionsDirective,
  ],
  templateUrl: './parking.component.html',
  styleUrl: './parking.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParkingComponent implements OnInit {
  private readonly api = inject(ParkingApiService);
  private readonly sidePanel = inject(SidePanelService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly canWrite = inject(AuthService).hasPermission(P.parkingWrite);

  readonly availability = signal<Availability | null>(null);
  readonly spaces = signal<ParkingSpace[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly updatedAt = signal<Date | null>(null);

  readonly type = signal('');
  readonly state = signal('');
  readonly kind = signal('');
  readonly location = signal('');
  /** Periodo futuro (datetime-local); vacío = ahora. */
  readonly from = signal('');
  readonly to = signal('');

  readonly creating = signal<'one' | 'bulk' | null>(null);
  readonly busy = signal(false);
  readonly formError = signal('');

  readonly form = inject(FormBuilder).nonNullable.group({
    code: [''],
    prefix: ['V-'],
    rangeFrom: [1, [Validators.min(0)]],
    rangeTo: [10, [Validators.min(0)]],
    location: [''],
    type: [ParkingType.Visitor],
    vehicleKind: [ParkingVehicleKind.Any],
  });

  readonly types = [ParkingType.Private, ParkingType.Community, ParkingType.Visitor];
  readonly kinds = [ParkingVehicleKind.Any, ParkingVehicleKind.Car, ParkingVehicleKind.Motorcycle];
  readonly states = [
    ParkingState.Available,
    ParkingState.Occupied,
    ParkingState.Reserved,
    ParkingState.OutOfService,
  ];
  readonly typeLabels = PARKING_TYPE_LABELS;
  readonly kindLabels = PARKING_KIND_LABELS;
  readonly stateLabels = PARKING_STATE_LABELS;
  readonly stateIcons = STATE_ICONS;
  readonly PlusIcon = Plus;

  ngOnInit(): void {
    this.load();
    merge(
      interval(REFRESH_MS),
      fromEvent(document, 'visibilitychange').pipe(
        filter(() => document.visibilityState === 'visible')
      )
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        // mientras se llena un formulario o la pestaña está oculta no se refresca
        if (!this.creating() && document.visibilityState === 'visible') {
          this.load();
        }
      });
  }

  /** Total de la fila "todos los tipos" o de un tipo. */
  typeTitle(type: ParkingType | null | undefined): string {
    return type === null || type === undefined ? 'Todos' : this.typeLabels[type];
  }

  load(): void {
    const from = this.from() ? new Date(this.from()).toISOString() : undefined;
    const to = from && this.to() ? new Date(this.to()).toISOString() : undefined;
    forkJoin({
      availability: this.api.availability(from, to),
      spaces: this.api.spaces({
        type: this.type() === '' ? null : Number(this.type()),
        state: this.state() === '' ? null : Number(this.state()),
        vehicleKind: this.kind() === '' ? null : Number(this.kind()),
        location: this.location(),
        from,
        to,
      }),
    }).subscribe({
      next: ({ availability, spaces }) => {
        this.availability.set(availability);
        this.spaces.set(spaces);
        this.loading.set(false);
        this.errorMessage.set('');
        this.updatedAt.set(new Date());
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo consultar la disponibilidad.'));
      },
    });
  }

  setFilter(
    name: 'type' | 'state' | 'kind' | 'location' | 'from' | 'to',
    event: Event
  ): void {
    this[name].set((event.target as HTMLInputElement | HTMLSelectElement).value);
    this.load();
  }

  /** Lo que se ve de un espacio sin abrirlo: quién lo ocupa o para quién está apartado. */
  info(space: ParkingSpace): string {
    if (space.state === ParkingState.OutOfService) {
      return space.outOfServiceReason ?? '';
    }
    if (space.occupant) {
      return [space.occupant.plate, space.occupant.unitLabel].filter(Boolean).join(' · ');
    }
    if (space.plan) {
      return space.plan.unitLabel ?? space.plan.plate ?? '';
    }
    return space.holderUnitLabel ?? space.location ?? '';
  }

  open(space: ParkingSpace): void {
    this.sidePanel
      .open<boolean, string, ParkingDetailComponent>(ParkingDetailComponent, {
        ariaLabel: `Parqueadero ${space.code}`,
        data: space.id,
      })
      .closed.subscribe(() => this.load());
  }

  startCreate(mode: 'one' | 'bulk'): void {
    this.creating.set(mode);
    this.formError.set('');
  }

  save(): void {
    const mode = this.creating();
    if (!mode || this.form.invalid || this.busy()) {
      return;
    }
    const value = this.form.getRawValue();
    if (mode === 'one' && !value.code.trim()) {
      this.formError.set('Escriba el código del espacio.');
      return;
    }
    const common = {
      location: value.location || null,
      type: Number(value.type),
      vehicleKind: Number(value.vehicleKind),
    };

    this.busy.set(true);
    this.formError.set('');
    const done = (message: string) => {
      this.busy.set(false);
      this.creating.set(null);
      this.notifications.success(message);
      this.load();
    };
    const fail = (err: unknown) => {
      this.busy.set(false);
      this.formError.set(apiErrorMessage(err, 'No se pudo crear el parqueadero.'));
    };

    if (mode === 'one') {
      this.api.create({ ...common, code: value.code }).subscribe({
        next: (space) => done(`Parqueadero ${space.code} creado.`),
        error: fail,
      });
    } else {
      this.api
        .createBulk({ ...common, prefix: value.prefix, from: value.rangeFrom, to: value.rangeTo })
        .subscribe({
          next: (result) =>
            done(
              `${result.created.length} parqueaderos creados` +
                (result.skipped.length ? `; ${result.skipped.length} ya existían.` : '.')
            ),
          error: fail,
        });
    }
  }
}
