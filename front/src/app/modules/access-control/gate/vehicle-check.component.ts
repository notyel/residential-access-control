import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import {
  CircleAlert,
  CircleCheck,
  CircleHelp,
  Clock,
  LucideAngularModule,
  Search,
} from 'lucide-angular';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { AccessEventType } from '../../../core/models/access.model';
import {
  PlateCheck,
  PlateResult,
  VehicleClass,
  VehicleInside,
  normalizePlate,
  vehicleDescription,
} from '../../../core/models/vehicle.model';
import { P } from '../../../core/security/permissions';
import { AccessApiService } from '../../../core/services/access-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { VehiclesApiService } from '../../../core/services/vehicles-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { NotificationService } from '../../../shared/services/notifications/notification.service';

/** Cada resultado se distingue por color, icono y texto: no solo por el color. */
const RESULT_VIEW = {
  [PlateResult.Authorized]: { tone: 'ok', icon: CircleCheck, title: 'Autorizado' },
  [PlateResult.NotValid]: { tone: 'blocked', icon: CircleAlert, title: 'Sin autorización vigente' },
  [PlateResult.VisitInProgress]: { tone: 'visit', icon: Clock, title: 'Vehículo de una visita' },
  [PlateResult.Unknown]: { tone: 'unknown', icon: CircleHelp, title: 'No registrado' },
} as const;

/**
 * Verificación de placas en portería: al escribir la placa responde si el vehículo puede entrar,
 * y desde ahí se registra su entrada o salida. Abajo, los vehículos que están dentro, calculados
 * de los eventos de acceso.
 */
@Component({
  selector: 'app-vehicle-check',
  standalone: true,
  imports: [DatePipe, MatButtonModule, LucideAngularModule],
  templateUrl: './vehicle-check.component.html',
  styleUrl: './gate.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleCheckComponent implements OnInit {
  private readonly api = inject(VehiclesApiService);
  private readonly accessApi = inject(AccessApiService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly canException = inject(AuthService).hasPermission(P.accessException);

  readonly plate = signal('');
  readonly check = signal<PlateCheck | null>(null);
  readonly checking = signal(false);
  readonly inside = signal<VehicleInside[]>([]);
  readonly driverId = signal('');
  readonly exceptionReason = signal('');
  readonly busy = signal(false);
  readonly errorMessage = signal('');

  readonly view = computed(() => {
    const check = this.check();
    return check ? RESULT_VIEW[check.result] : null;
  });
  readonly permanentInside = computed(() =>
    this.inside().filter((v) => v.class === VehicleClass.Permanent)
  );
  readonly visitorsInside = computed(() =>
    this.inside().filter((v) => v.class === VehicleClass.Visitor)
  );

  private readonly plates = new Subject<string>();

  readonly Result = PlateResult;
  readonly SearchIcon = Search;
  readonly describe = vehicleDescription;

  ngOnInit(): void {
    this.plates
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((plate) => {
          if (plate.length < 3) {
            return of(null);
          }
          this.checking.set(true);
          return this.api.checkPlate(plate).pipe(catchError(() => of(null)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((check) => this.show(check));
    this.loadInside();
  }

  /** La placa se normaliza mientras se escribe: lo que se ve es lo que se busca. */
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const plate = normalizePlate(input.value);
    input.value = plate;
    this.plate.set(plate);
    this.plates.next(plate);
  }

  onDriver(event: Event): void {
    this.driverId.set((event.target as HTMLSelectElement).value);
  }

  onReason(event: Event): void {
    this.exceptionReason.set((event.target as HTMLInputElement).value);
  }

  /** Entrada o salida del vehículo permanente con quien lo conduce. */
  move(type: AccessEventType.Entry | AccessEventType.Exit, asException = false): void {
    const check = this.check();
    const vehicle = check?.vehicle;
    if (!vehicle?.unitId || this.busy()) {
      return;
    }
    if (!this.driverId()) {
      this.errorMessage.set('Indique quién conduce el vehículo.');
      return;
    }
    this.busy.set(true);
    this.errorMessage.set('');
    this.accessApi
      .residentMovement(this.driverId(), vehicle.unitId, type, {
        vehicleId: vehicle.id,
        isException: asException,
        exceptionReason: asException ? this.exceptionReason() : undefined,
      })
      .subscribe({
        next: () => this.done(`${type === AccessEventType.Entry ? 'Entrada' : 'Salida'} de ${vehicle.plate} registrada.`),
        error: (err) => this.fail(err),
      });
  }

  /** Vehículo que quedó dentro sin su conductor. */
  exitVehicle(vehicleId: string, plate: string): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.errorMessage.set('');
    this.api.vehicleExit(vehicleId).subscribe({
      next: () => this.done(`Salida de ${plate} registrada.`),
      error: (err) => this.fail(err),
    });
  }

  private show(check: PlateCheck | null): void {
    this.check.set(check);
    this.checking.set(false);
    this.errorMessage.set('');
    this.exceptionReason.set('');
    // el responsable viene primero: es quien suele conducir
    this.driverId.set(check?.drivers[0]?.personId ?? '');
  }

  private done(message: string): void {
    this.busy.set(false);
    this.notifications.success(message);
    this.loadInside();
    const plate = this.plate();
    if (plate.length >= 3) {
      this.api.checkPlate(plate).subscribe((check) => this.show(check));
    }
  }

  private fail(err: unknown): void {
    this.busy.set(false);
    this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar el movimiento.'));
  }

  private loadInside(): void {
    this.api.inside().subscribe((inside) => this.inside.set(inside));
  }

  readonly Movement = AccessEventType;
}
