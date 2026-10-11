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
  VEHICLE_STATUS_LABELS,
  VehicleDetail,
  VehicleStatus,
  vehicleDescription,
} from '../../../core/models/vehicle.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { VehiclesApiService } from '../../../core/services/vehicles-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { SidePanelComponent } from '../../../shared/components/side-panel/side-panel.component';

/** Acción que pide un texto antes de confirmarse. */
type PendingAction = 'suspend' | 'revoke' | 'note';

const ACTION_LABELS: Record<PendingAction, string> = {
  suspend: 'Motivo de la suspensión',
  revoke: 'Motivo de la revocación',
  note: 'Novedad',
};

/** Qué debe hacer la lista al cerrarse el panel. */
export type VehicleDetailResult = 'changed' | { edit: string } | { reassign: string };

/**
 * Ficha de un vehículo: sus datos, el historial de autorizaciones (nunca se sobrescribe) y las
 * novedades. Suspender y revocar exigen motivo.
 */
@Component({
  selector: 'app-vehicle-detail',
  standalone: true,
  imports: [DatePipe, MatButtonModule, SidePanelComponent],
  templateUrl: './vehicle-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDetailComponent implements OnInit {
  private readonly dialogRef = inject<DialogRef<VehicleDetailResult>>(DialogRef);
  private readonly api = inject(VehiclesApiService);
  private readonly vehicleId = inject<string>(DIALOG_DATA);

  readonly canWrite = inject(AuthService).hasPermission(P.vehiclesWrite);

  readonly detail = signal<VehicleDetail | null>(null);
  readonly pending = signal<PendingAction | null>(null);
  readonly text = signal('');
  readonly busy = signal(false);
  readonly errorMessage = signal('');
  private changed = false;

  readonly Status = VehicleStatus;
  readonly statusLabels = VEHICLE_STATUS_LABELS;
  readonly actionLabels = ACTION_LABELS;
  readonly describe = vehicleDescription;

  ngOnInit(): void {
    this.load();
  }

  ask(action: PendingAction): void {
    this.pending.set(action);
    this.text.set('');
    this.errorMessage.set('');
  }

  onText(event: Event): void {
    this.text.set((event.target as HTMLTextAreaElement).value);
  }

  confirm(): void {
    const action = this.pending();
    const text = this.text().trim();
    if (!action) {
      return;
    }
    if (text.length < 5) {
      this.errorMessage.set('Escriba el motivo.');
      return;
    }
    const request =
      action === 'suspend'
        ? this.api.suspend(this.vehicleId, text)
        : action === 'revoke'
        ? this.api.revoke(this.vehicleId, text)
        : this.api.addNote(this.vehicleId, text);
    this.run(request);
  }

  reactivate(): void {
    this.run(this.api.reactivate(this.vehicleId));
  }

  edit(): void {
    this.dialogRef.close({ edit: this.vehicleId });
  }

  reassign(): void {
    this.dialogRef.close({ reassign: this.vehicleId });
  }

  close(): void {
    this.dialogRef.close(this.changed ? 'changed' : undefined);
  }

  private load(): void {
    this.api.detail(this.vehicleId).subscribe({
      next: (detail) => this.detail.set(detail),
      error: (err) =>
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar el vehículo.')),
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
        this.pending.set(null);
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
