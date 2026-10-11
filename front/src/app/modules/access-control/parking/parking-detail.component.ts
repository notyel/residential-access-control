import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Observable } from 'rxjs';
import {
  PARKING_ASSIGNMENT_STATUS_LABELS,
  PARKING_CLASS_LABELS,
  PARKING_KIND_LABELS,
  PARKING_STATE_LABELS,
  PARKING_TYPE_LABELS,
  ParkingSpaceDetail,
  ParkingState,
  ParkingType,
} from '../../../core/models/parking.model';
import { Unit } from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { ParkingApiService } from '../../../core/services/parking-api.service';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { SidePanelComponent } from '../../../shared/components/side-panel/side-panel.component';

/** Acción que pide datos antes de confirmarse. */
type Action = 'out' | 'note' | 'holder' | 'assign' | 'reserve';

const ACTION_TITLES: Record<Action, string> = {
  out: 'Motivo por el que queda fuera de servicio',
  note: 'Novedad',
  holder: 'Unidad titular',
  assign: 'Asignación temporal a una unidad',
  reserve: 'Reserva para una unidad',
};

/**
 * Ficha de un parqueadero: quién lo ocupa y desde cuándo, su titular, sus periodos fuera de
 * servicio y todo su historial. El estado no se edita: resulta de lo que aquí se registra.
 * Se cierra devolviendo true si algo cambió.
 */
@Component({
  selector: 'app-parking-detail',
  standalone: true,
  imports: [DatePipe, MatButtonModule, SidePanelComponent],
  templateUrl: './parking-detail.component.html',
  styleUrl: './parking.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParkingDetailComponent implements OnInit {
  private readonly dialogRef = inject<DialogRef<boolean>>(DialogRef);
  private readonly api = inject(ParkingApiService);
  private readonly unitsApi = inject(UnitsApiService);
  private readonly auth = inject(AuthService);
  private readonly spaceId = inject<string>(DIALOG_DATA);

  readonly canWrite = this.auth.hasPermission(P.parkingWrite);
  readonly canAssign = this.auth.hasPermission(P.parkingAssign);
  readonly canReserve = this.auth.hasPermission(P.parkingReserve);
  readonly canRelease = this.canAssign || this.auth.hasPermission(P.accessRegister);

  readonly detail = signal<ParkingSpaceDetail | null>(null);
  readonly action = signal<Action | null>(null);
  readonly text = signal('');
  readonly unit = signal<{ id: string; label: string } | null>(null);
  readonly unitResults = signal<Unit[]>([]);
  readonly from = signal('');
  readonly to = signal('');
  readonly busy = signal(false);
  readonly errorMessage = signal('');
  private changed = false;

  readonly Type = ParkingType;
  readonly State = ParkingState;
  readonly typeLabels = PARKING_TYPE_LABELS;
  readonly kindLabels = PARKING_KIND_LABELS;
  readonly stateLabels = PARKING_STATE_LABELS;
  readonly classLabels = PARKING_CLASS_LABELS;
  readonly statusLabels = PARKING_ASSIGNMENT_STATUS_LABELS;
  readonly actionTitles = ACTION_TITLES;

  ngOnInit(): void {
    this.load();
  }

  ask(action: Action): void {
    this.action.set(action);
    this.text.set('');
    this.unit.set(null);
    this.unitResults.set([]);
    this.errorMessage.set('');
  }

  onText(event: Event): void {
    this.text.set((event.target as HTMLTextAreaElement).value);
  }

  onDate(field: 'from' | 'to', event: Event): void {
    this[field].set((event.target as HTMLInputElement).value);
  }

  searchUnits(event: Event): void {
    const term = (event.target as HTMLInputElement).value.trim();
    if (!term) {
      this.unitResults.set([]);
      return;
    }
    this.unitsApi
      .units({ pageNumber: 1, pageSize: 6, search: term, isActive: true })
      .subscribe((page) => this.unitResults.set(page.items));
  }

  chooseUnit(unit: Unit): void {
    this.unit.set({ id: unit.id, label: unit.label });
    this.unitResults.set([]);
  }

  confirm(): void {
    const action = this.action();
    const unit = this.unit();
    const text = this.text().trim();
    if (!action) {
      return;
    }
    if ((action === 'out' || action === 'note') && text.length < 3) {
      this.errorMessage.set('Escriba el texto.');
      return;
    }
    if (action !== 'out' && action !== 'note' && !unit) {
      this.errorMessage.set('Elija la unidad.');
      return;
    }
    if ((action === 'assign' || action === 'reserve') && (!this.from() || !this.to())) {
      this.errorMessage.set('Indique el inicio y el fin.');
      return;
    }
    const plan = () => ({
      spaceId: this.spaceId,
      unitId: unit!.id,
      // la hora local del navegador se envía con su zona
      startsAt: new Date(this.from()).toISOString(),
      endsAt: new Date(this.to()).toISOString(),
    });
    const requests: Record<Action, () => Observable<unknown>> = {
      out: () => this.api.outOfService(this.spaceId, text),
      note: () => this.api.addNote(this.spaceId, text),
      holder: () => this.api.setHolder(this.spaceId, unit!.id),
      assign: () => this.api.assign(plan()),
      reserve: () => this.api.reserve(plan()),
    };
    this.run(requests[action]());
  }

  endOutOfService(id: string): void {
    this.run(this.api.endOutOfService(id));
  }

  endHolder(): void {
    this.run(this.api.endHolder(this.spaceId));
  }

  release(assignmentId: string): void {
    this.run(this.api.release(assignmentId));
  }

  cancelPlan(assignmentId: string): void {
    this.run(this.api.cancelAssignment(assignmentId));
  }

  setActive(active: boolean): void {
    this.run(this.api.setActive(this.spaceId, active));
  }

  close(): void {
    this.dialogRef.close(this.changed);
  }

  private load(): void {
    this.api.detail(this.spaceId).subscribe({
      next: (detail) => this.detail.set(detail),
      error: (err) =>
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar el parqueadero.')),
    });
  }

  private run(request: Observable<unknown>): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.errorMessage.set('');
    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.action.set(null);
        this.changed = true;
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo guardar el cambio.'));
      },
    });
  }
}
