import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin } from 'rxjs';
import {
  Authorization,
  CheckIn,
  GateContext,
  Visit,
  authorizationWindow,
} from '../../../core/models/access.model';
import { Person } from '../../../core/models/person.model';
import { CatalogItem, Unit } from '../../../core/models/unit.model';
import { PlateResult, normalizePlate } from '../../../core/models/vehicle.model';
import { VehiclesApiService } from '../../../core/services/vehicles-api.service';
import { ParkingSpace } from '../../../core/models/parking.model';
import { ParkingApiService } from '../../../core/services/parking-api.service';
import { P } from '../../../core/security/permissions';
import { AccessApiService } from '../../../core/services/access-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { SidePanelComponent } from '../../../shared/components/side-panel/side-panel.component';

export interface GateEntryData {
  context: GateContext;
  /** Autorización con la que se presenta el visitante. */
  authorization?: Authorization;
  /** Persona ya registrada (visitante anterior). */
  person?: Person;
  /** Documento buscado en portería, para no volver a escribirlo. */
  document?: string;
}

/** Lo que pasó en el panel: un ingreso o un rechazo registrado. */
export type GateEntryResult = { visit: Visit } | { rejected: string };

interface UnitPerson {
  id: string;
  name: string;
}

const CHANNELS = ['Citófono', 'Teléfono', 'Presencial'];

/**
 * Registro de un ingreso (o de su rechazo) en portería. El formulario pide lo que la política
 * del conjunto exige, pero quien valida la autorización y decide es el servidor, con el estado
 * del momento en que se guarda.
 */
@Component({
  selector: 'app-gate-entry',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    SidePanelComponent,
  ],
  templateUrl: './gate-entry.component.html',
  styleUrl: './gate.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GateEntryComponent implements OnInit {
  private readonly dialogRef = inject<DialogRef<GateEntryResult>>(DialogRef);
  private readonly api = inject(AccessApiService);
  private readonly unitsApi = inject(UnitsApiService);
  private readonly vehiclesApi = inject(VehiclesApiService);
  private readonly parkingApi = inject(ParkingApiService);
  readonly data = inject<GateEntryData>(DIALOG_DATA);

  readonly canException = inject(AuthService).hasPermission(P.accessException);
  readonly channels = CHANNELS;
  readonly window = authorizationWindow;

  readonly authorization = this.data.authorization ?? null;
  /** La autorización existe pero ahora no permite ingresar (vencida, revocada, fuera de horario…). */
  readonly blockedReason =
    this.authorization && !this.authorization.canEnterNow
      ? this.authorization.cannotEnterReason ?? 'La autorización no es válida ahora.'
      : null;

  readonly person = signal<Person | null>(this.data.person ?? null);
  readonly unit = signal<{ id: string; label: string } | null>(
    this.authorization
      ? { id: this.authorization.unitId, label: this.authorization.unitLabel }
      : null
  );
  readonly unitTerm = signal('');
  readonly unitResults = signal<Unit[]>([]);
  readonly unitPeople = signal<UnitPerson[]>([]);
  readonly reasons = signal<CatalogItem[]>([]);
  readonly rejectionReasons = signal<CatalogItem[]>([]);
  readonly documentTypes = signal<CatalogItem[]>([]);
  readonly vehicleTypes = signal<CatalogItem[]>([]);
  /** La placa escrita ya existe: aviso para el guarda antes de guardar. */
  readonly plateNotice = signal('');
  /** La placa no está registrada: se piden tipo, marca y color. */
  readonly newVehicle = signal(false);
  /** Espacios de visitantes libres para ese vehículo; la misma consulta de la pantalla de disponibilidad. */
  readonly parkingSpaces = signal<ParkingSpace[]>([]);
  readonly mode = signal<'entry' | 'reject'>('entry');
  readonly exception = signal(false);
  readonly busy = signal(false);
  readonly errorMessage = signal('');

  /** Sin autorización válida: qué exige la política para un ingreso normal. */
  readonly needsConfirmation = computed(
    () =>
      !this.usesAuthorization() &&
      !this.exception() &&
      this.data.context.withoutAuthorization === 'resident_confirmation'
  );
  readonly notAllowed = computed(
    () =>
      !this.usesAuthorization() &&
      !this.exception() &&
      (this.blockedReason !== null ||
        this.data.context.withoutAuthorization === 'not_allowed')
  );

  readonly form = inject(FormBuilder).nonNullable.group({
    firstName: [''],
    lastName: [''],
    documentType: ['CC'],
    documentNumber: [this.data.document ?? ''],
    reason: ['otro'],
    vehiclePlate: [''],
    vehicleType: ['automovil'],
    vehicleBrand: [''],
    vehicleColor: [''],
    parkingSpaceId: [''],
    notes: [''],
    confirmedByPersonId: [''],
    confirmationChannel: [CHANNELS[0]],
    exceptionReason: [''],
    rejectReason: [''],
  });

  ngOnInit(): void {
    forkJoin({
      reasons: this.unitsApi.catalog('visit-reason'),
      rejections: this.unitsApi.catalog('rejection-reason'),
      documentTypes: this.unitsApi.catalog('document-type'),
      vehicleTypes: this.unitsApi.catalog('vehicle-type'),
    }).subscribe(({ reasons, rejections, documentTypes, vehicleTypes }) => {
      this.vehicleTypes.set(vehicleTypes);
      this.reasons.set(reasons);
      this.rejectionReasons.set(rejections);
      this.documentTypes.set(documentTypes.filter((type) => type.code !== 'NA'));
      if (rejections.length) {
        this.form.controls.rejectReason.setValue(rejections[0].code);
      }
    });

    const authorization = this.authorization;
    if (authorization) {
      const [first, ...rest] = authorization.visitorName.trim().split(/\s+/);
      this.form.patchValue({
        firstName: first ?? '',
        lastName: rest.join(' '),
        documentNumber: authorization.visitorDocument ?? this.data.document ?? '',
        reason: authorization.reason,
      });
      this.loadUnitPeople(authorization.unitId);
    }
    if (this.blockedReason && !this.canException) {
      // sin permiso de excepción, lo único que queda es dejar constancia del rechazo
      this.mode.set('reject');
    }
  }

  private usesAuthorization(): boolean {
    return this.authorization !== null && this.blockedReason === null;
  }

  /** La persona puede estar ya registrada con ese documento: se reutiliza en vez de duplicarla. */
  findByDocument(): void {
    const document = this.form.controls.documentNumber.value.trim();
    if (this.person() || document.length < 3) {
      return;
    }
    this.api.lookup(document).subscribe((result) => {
      const match = result.people[0];
      if (match) {
        this.person.set(match);
      }
    });
  }

  /** Normaliza la placa y consulta si ya existe: un vehículo de residente no debería entrar como visita. */
  checkPlate(): void {
    const control = this.form.controls.vehiclePlate;
    const plate = normalizePlate(control.value);
    control.setValue(plate, { emitEvent: false });
    this.plateNotice.set('');
    this.newVehicle.set(false);
    this.parkingSpaces.set([]);
    this.form.controls.parkingSpaceId.setValue('');
    if (plate.length < 3) {
      return;
    }
    this.vehiclesApi.checkPlate(plate).subscribe({
      next: (check) => {
        this.loadParking(check.vehicle?.type);
        if (!check.vehicle) {
          this.newVehicle.set(true);
        } else if (check.result === PlateResult.Authorized || check.result === PlateResult.NotValid) {
          this.plateNotice.set(
            `Atención: ${plate} es un vehículo registrado de la unidad ${check.vehicle.unitLabel} (${check.vehicle.responsibleName}).`
          );
        } else if (check.isInside) {
          this.plateNotice.set(`Atención: ${plate} figura dentro del conjunto.`);
        }
      },
      error: () => undefined,
    });
  }

  /** Al cambiar el tipo de un vehículo nuevo cambian los espacios que lo admiten. */
  loadParking(vehicleType?: string): void {
    this.parkingApi
      .visitorSpaces(vehicleType ?? this.form.controls.vehicleType.value)
      .subscribe({
        next: (spaces) => this.parkingSpaces.set(spaces),
        // sin permiso o sin conexión: el ingreso sigue siendo posible sin parqueadero
        error: () => this.parkingSpaces.set([]),
      });
  }

  clearPerson(): void {
    this.person.set(null);
  }

  searchUnits(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this.unitTerm.set(term);
    if (term.trim().length < 1) {
      this.unitResults.set([]);
      return;
    }
    this.unitsApi
      .units({ pageNumber: 1, pageSize: 8, search: term, isActive: true })
      .subscribe((page) => this.unitResults.set(page.items));
  }

  chooseUnit(unit: Unit): void {
    this.unit.set({ id: unit.id, label: unit.label });
    this.unitResults.set([]);
    this.unitTerm.set('');
    this.form.controls.confirmedByPersonId.setValue('');
    this.loadUnitPeople(unit.id);
  }

  clearUnit(): void {
    this.unit.set(null);
    this.unitPeople.set([]);
  }

  toggleException(checked: boolean): void {
    this.exception.set(checked);
    this.errorMessage.set('');
  }

  save(): void {
    const unit = this.unit();
    const value = this.form.getRawValue();
    const person = this.person();
    this.errorMessage.set('');

    if (!unit) {
      this.errorMessage.set('Indique la unidad de destino.');
      return;
    }
    const visitorPersonId = person?.id ?? this.authorization?.visitorPersonId ?? null;
    if (!visitorPersonId && (!value.firstName.trim() || !value.lastName.trim())) {
      this.errorMessage.set('Escriba el nombre y el apellido del visitante.');
      return;
    }

    if (this.mode() === 'reject') {
      this.reject(unit.id, visitorPersonId);
      return;
    }

    const body: CheckIn = {
      unitId: unit.id,
      personId: visitorPersonId,
      newPerson: visitorPersonId
        ? null
        : {
            firstName: value.firstName,
            lastName: value.lastName,
            documentType: value.documentNumber.trim() ? value.documentType : null,
            documentNumber: value.documentNumber.trim() || null,
          },
      reason: value.reason,
      vehiclePlate: value.vehiclePlate || null,
      vehicleType: this.newVehicle() ? value.vehicleType : null,
      vehicleBrand: this.newVehicle() ? value.vehicleBrand || null : null,
      vehicleColor: this.newVehicle() ? value.vehicleColor || null : null,
      parkingSpaceId: (value.vehiclePlate && value.parkingSpaceId) || null,
      notes: value.notes || null,
    };
    if (this.exception()) {
      body.isException = true;
      body.exceptionReason = value.exceptionReason;
    } else if (this.usesAuthorization()) {
      body.authorizationId = this.authorization!.id;
    } else if (this.needsConfirmation()) {
      body.confirmedByPersonId = value.confirmedByPersonId || null;
      body.confirmationChannel = value.confirmationChannel;
    }

    this.busy.set(true);
    this.api.checkIn(body).subscribe({
      next: (visit) => this.dialogRef.close({ visit }),
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar el ingreso.'));
      },
    });
  }

  private reject(unitId: string, personId: string | null): void {
    const value = this.form.getRawValue();
    const person = this.person();
    const name = person
      ? `${person.firstName} ${person.lastName}`
      : `${value.firstName} ${value.lastName}`.trim();

    this.busy.set(true);
    this.api
      .reject({
        unitId,
        personId,
        personName: name,
        personDocument: person?.documentNumber ?? (value.documentNumber.trim() || null),
        reasonCode: value.rejectReason,
        notes: value.notes || null,
        vehiclePlate: value.vehiclePlate || null,
      })
      .subscribe({
        next: () => this.dialogRef.close({ rejected: name }),
        error: (err) => {
          this.busy.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar el rechazo.'));
        },
      });
  }

  private loadUnitPeople(unitId: string): void {
    this.unitsApi.unitSummary(unitId).subscribe({
      next: (summary) => {
        const people = new Map<string, string>();
        for (const link of [...summary.owners, ...summary.occupants]) {
          people.set(link.personId, link.personName);
        }
        this.unitPeople.set([...people].map(([id, name]) => ({ id, name })));
      },
      error: () => this.unitPeople.set([]),
    });
  }
}

