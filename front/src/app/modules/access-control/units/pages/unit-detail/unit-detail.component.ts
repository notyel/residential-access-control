import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LucideAngularModule, SquarePen } from 'lucide-angular';
import { Observable } from 'rxjs';
import {
  OCCUPANCY_STATE_LABELS,
  OCCUPANCY_STATUS_LABELS,
  Occupancy,
  OccupancyRelation,
  PersonListItem,
  RELATION_LABELS,
  UnitOccupancyState,
  UnitSummary,
  todayIso,
} from '../../../../../core/models/unit.model';
import { P } from '../../../../../core/security/permissions';
import { UnitsApiService } from '../../../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { PersonPickerComponent } from '../../../../../shared/components/person-picker/person-picker.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';

type Action = 'owner' | 'transfer' | 'occupant' | null;

/** Ficha de la unidad: lo vigente arriba, la historia abajo, y las acciones según permiso. */
@Component({
  selector: 'app-unit-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    PersonPickerComponent,
    StatusBadgeComponent,
    IfPermissionDirective,
    PageActionsDirective,
  ],
  templateUrl: './unit-detail.component.html',
  styleUrl: './unit-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitDetailComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  readonly unitId = inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  readonly summary = signal<UnitSummary | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  /** Formulario abierto en este momento; solo uno a la vez. */
  readonly action = signal<Action>(null);
  readonly person = signal<PersonListItem | null>(null);
  readonly relation = signal<OccupancyRelation>(OccupancyRelation.Tenant);
  readonly startDate = signal(todayIso());
  readonly endDate = signal('');
  /** Ocupación que se está finalizando. */
  readonly ending = signal<Occupancy | null>(null);
  readonly endReason = signal('');
  readonly includeDependents = signal(true);
  readonly busy = signal(false);

  readonly P = P;
  readonly State = UnitOccupancyState;
  readonly Relation = OccupancyRelation;
  readonly stateLabels = OCCUPANCY_STATE_LABELS;
  readonly relationLabels = RELATION_LABELS;
  readonly statusLabels = OCCUPANCY_STATUS_LABELS;
  readonly relations = [
    OccupancyRelation.Tenant,
    OccupancyRelation.OwnerResident,
    OccupancyRelation.Family,
    OccupancyRelation.Other,
  ];
  readonly EditIcon = SquarePen;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.unitSummary(this.unitId).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar la unidad.'));
      },
    });
  }

  open(action: Action): void {
    this.action.set(action);
    this.ending.set(null);
    this.person.set(null);
    this.startDate.set(todayIso());
    this.endDate.set('');
    this.errorMessage.set('');
  }

  askEnd(occupancy: Occupancy): void {
    this.action.set(null);
    this.ending.set(occupancy);
    this.endDate.set(todayIso());
    this.endReason.set('');
    this.errorMessage.set('');
  }

  value(event: Event): string {
    return (event.target as HTMLInputElement | HTMLSelectElement).value;
  }

  checked(event: Event): boolean {
    return (event.target as HTMLInputElement).checked;
  }

  save(): void {
    const person = this.person();
    if (!person) {
      this.errorMessage.set('Elija una persona.');
      return;
    }

    const requests: Record<Exclude<Action, null>, () => Observable<unknown>> = {
      owner: () => this.api.addOwner(this.unitId, person.id, this.startDate()),
      transfer: () =>
        this.api.transferOwnership(this.unitId, [person.id], this.startDate()),
      occupant: () =>
        this.api.addOccupancy({
          unitId: this.unitId,
          personId: person.id,
          relation: this.relation(),
          startDate: this.startDate(),
          endDate: this.endDate() || null,
        }),
    };
    this.run(requests[this.action()!]());
  }

  confirmEnd(): void {
    const occupancy = this.ending();
    if (!occupancy) {
      return;
    }
    this.run(
      this.api.endOccupancy(occupancy.id, {
        endDate: this.endDate(),
        reason: this.endReason() || null,
        includeDependents: this.includeDependents(),
      })
    );
  }

  decide(occupancy: Occupancy, approve: boolean): void {
    const reason = approve
      ? undefined
      : window.prompt('Motivo del rechazo') ?? undefined;
    if (!approve && !reason) {
      return;
    }
    this.run(this.api.decideOccupancy(occupancy.id, approve, reason));
  }

  toggleActive(): void {
    const unit = this.summary()?.unit;
    if (unit) {
      this.run(this.api.setUnitActive(unit.id, !unit.isActive));
    }
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
        this.ending.set(null);
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(
          apiErrorMessage(err, 'No se pudo completar la acción.')
        );
      },
    });
  }
}
