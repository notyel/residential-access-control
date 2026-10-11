import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Observable } from 'rxjs';
import {
  VERIFICATION_LABELS,
  Visit,
  visitorDocument,
  visitorName,
} from '../../../../../core/models/access.model';
import { P } from '../../../../../core/security/permissions';
import { AccessApiService } from '../../../../../core/services/access-api.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { SidePanelComponent } from '../../../../../shared/components/side-panel/side-panel.component';

/** Acción que exige escribir un motivo antes de confirmarse. */
type PendingAction = 'close' | 'void-entry' | 'void-exit';

const ACTION_LABELS: Record<PendingAction, string> = {
  close: 'Motivo del cierre administrativo',
  'void-entry': 'Motivo para anular el ingreso',
  'void-exit': 'Motivo para anular la salida',
};

/**
 * Trazabilidad de una visita: quién autorizó, quién registró la entrada y la salida. Desde aquí
 * se corrigen errores: los eventos no se editan, se anulan dejando el motivo.
 * Se cierra devolviendo true si algo cambió.
 */
@Component({
  selector: 'app-visit-detail',
  standalone: true,
  imports: [DatePipe, MatButtonModule, SidePanelComponent],
  templateUrl: './visit-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisitDetailComponent {
  private readonly dialogRef = inject<DialogRef<boolean>>(DialogRef);
  private readonly api = inject(AccessApiService);
  private readonly auth = inject(AuthService);

  readonly visit = signal(inject<Visit>(DIALOG_DATA));
  readonly canRegister = this.auth.hasPermission(P.accessRegister);
  readonly canVoid = this.auth.hasPermission(P.accessVoid);

  readonly pending = signal<PendingAction | null>(null);
  readonly reason = signal('');
  readonly busy = signal(false);
  readonly errorMessage = signal('');
  private changed = false;

  readonly verificationLabels = VERIFICATION_LABELS;
  readonly actionLabels = ACTION_LABELS;
  readonly visitorName = visitorName;
  readonly visitorDocument = visitorDocument;

  ask(action: PendingAction): void {
    this.pending.set(action);
    this.reason.set('');
    this.errorMessage.set('');
  }

  onReason(event: Event): void {
    this.reason.set((event.target as HTMLTextAreaElement).value);
  }

  /** withoutVehicle: el visitante sale y deja el vehículo dentro. */
  checkOut(withoutVehicle = false): void {
    this.run(
      this.api.checkOut(this.visit().id, { withoutVehicle }),
      'No se pudo registrar la salida.'
    );
  }

  confirm(): void {
    const action = this.pending();
    const reason = this.reason().trim();
    const visit = this.visit();
    if (!action) {
      return;
    }
    if (reason.length < 5) {
      this.errorMessage.set('Explique el motivo.');
      return;
    }
    if (action === 'close') {
      this.run(
        this.api.checkOut(visit.id, { administrativeReason: reason }),
        'No se pudo cerrar la visita.'
      );
    } else {
      const eventId = action === 'void-entry' ? visit.entryEventId : visit.exitEventId!;
      this.run(this.api.voidEvent(eventId, reason), 'No se pudo anular el registro.');
    }
  }

  close(): void {
    this.dialogRef.close(this.changed);
  }

  /** Tras cualquier cambio se cierra el panel: la lista se recarga con el estado real. */
  private run(request: Observable<unknown>, fallback: string): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.errorMessage.set('');
    request.subscribe({
      next: () => this.dialogRef.close(true),
      error: (err) => {
        this.busy.set(false);
        // un 409 significa que alguien más ya lo hizo: al cerrar, la lista se actualiza
        this.changed = true;
        this.errorMessage.set(apiErrorMessage(err, fallback));
      },
    });
  }
}
